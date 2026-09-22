using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.BuilderHubSystem;
using Timberborn.BuildingRange;
using Timberborn.Buildings;
using Timberborn.Common;
using Timberborn.ConstructionMode;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.InputSystem;
using Timberborn.LevelVisibilitySystem;
using Timberborn.Localization;
using Timberborn.MapStateSystem;
using Timberborn.Navigation;
using Timberborn.Planting;
using Timberborn.PlantingUI;
using Timberborn.PlatformUtilities;
using Timberborn.SceneLoading;
using Timberborn.SelectionSystem;
using Timberborn.SettlementNameSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using Timberborn.UILayoutSystem;
using Timberborn.WorkSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace PersistentWorkAreas
{
    public sealed class WorkAreaService : IPostLoadableSingleton, IUpdatableSingleton,
        IInputProcessor, ISingletonInstantNavMeshListener, ISingletonPreviewNavMeshListener, IDisposable
    {
        public const string ClearKey = "PersistentWorkAreas.Clear";
        private static readonly Func<CellBox, BoundingBox, bool> Touches = (box, bounds) => ToBoundingBox(box).Intersects(in bounds);
        private readonly PinSet<EntityComponent> _pins = new PinSet<EntityComponent>();
        private readonly PinRefreshPlanner<EntityComponent, (Vector3? Center, bool Preview, bool Terrain), Vector3Int> _planner;
        private readonly List<EntityComponent> _removed = new List<EntityComponent>();
        private readonly List<EntityComponent> _planters = new List<EntityComponent>();
        private readonly float _reach;
        private readonly INavigationRangeService _navigation;
        private readonly ConstructionModeService _construction;
        private readonly IBlockService _blocks;
        private readonly PreviewBlockService _previews;
        private readonly ILevelVisibilityService _visibility;
        private readonly MapSize _mapSize;
        private readonly ISpecService _specs;
        private readonly EntityComponentRegistry _registry;
        private readonly EntityRegistry _entities;
        private readonly SettlementReferenceService _settlements;
        private readonly EventBus _events;
        private readonly LoadingScreen _loading;
        private readonly UILayout _layout;
        private readonly InputService _input;
        private readonly ILoc _loc;
        private NativeOutline _outline;
        private Button _clearButton;
        private bool _active;
        private float _nextRefresh;
        private bool _rendererFailed;
        // The resource group of the plant whose planting tool is open, or null.
        private string _planting;
        private bool _plantersChanged;
        private PinFile _pinFile;
        // Set at load, so the first update marks this settlement as recently used in the pin file.
        private bool _touchPinFile;
        // After a failed write the next attempt waits, and only the first failure is logged.
        private float _nextSave;
        private bool _saveWarned;
        public event Action Changed;
        public int Count => _pins.Count;
        public bool Available => _active && !_rendererFailed;

        public WorkAreaService(INavigationRangeService navigation, NavigationDistance distance, ConstructionModeService construction,
            IBlockService blocks, PreviewBlockService previews, ILevelVisibilityService visibility,
            MapSize mapSize, ISpecService specs, EntityComponentRegistry registry, EventBus events, LoadingScreen loading,
            UILayout layout, InputService input, ILoc loc, EntityRegistry entities, SettlementReferenceService settlements)
        {
            _navigation = navigation; _construction = construction; _blocks = blocks;
            _previews = previews; _visibility = visibility; _mapSize = mapSize; _specs = specs;
            _registry = registry; _events = events; _loading = loading; _layout = layout; _input = input; _loc = loc;
            _entities = entities; _settlements = settlements;
            // BuildingTerrainRange's own margin for deciding whether a navigation change can alter a range.
            _reach = distance.ResourceBuildings + 2f;
            _planner = new PinRefreshPlanner<EntityComponent, (Vector3? Center, bool Preview, bool Terrain), Vector3Int>(
                Describe, Query, cell => CellBox.Point(cell.x, cell.y, cell.z));
        }

        public void PostLoad()
        {
            _active = true;
            _events.Register(this);
            _loading.LoadingScreenEnabled += OnLoading;
            _input.AddInputProcessor(this);
            // Notify() below sets the label, which shows the pin count.
            _clearButton = WorkAreaFragment.MakeButton(string.Empty, ClearAll);
            _clearButton.name = "PersistentWorkAreasClearAll";
            _clearButton.tooltip = _loc.T(LocKeys.ClearAllTooltip);
            _clearButton.style.marginTop = 6;
            _layout.AddTopRight(_clearButton, 1000);
            _pinFile = new PinFile(Path.Combine(UserDataFolder.Folder, "PersistentWorkAreas", "Pins.txt"));
            RestorePins(Supports);
            Notify();
        }

        public static bool Supports(BaseComponent component)
        {
            if (!component) return false;
            var block = component.GetComponent<BlockObject>();
            // The game gives every Builder's Hut a road-spill range (shown only while it is selected), but it is not a working area worth pinning.
            if (component.GetComponent<BuilderHubWorkplaceBehavior>()) return false;
            return block && !block.IsPreview && component.GetComponent<BuildingAccessible>() &&
                (component.GetComponent<BuildingWithTerrainRange>() || component.GetComponent<BuildingWithRoadSpillRange>());
        }

        public bool IsPinned(BaseComponent entity) => entity && _pins.Contains(entity.GetComponent<EntityComponent>());

        public void SetPinned(BaseComponent entity, bool value)
        {
            if (!Available || !entity || (value && !Supports(entity))) return;
            var pin = entity.GetComponent<EntityComponent>();
            if (_pins.Set(pin, value))
            {
                if (value) _planner.Add(pin); else _planner.Remove(pin);
                _nextRefresh = 0;
                if (_planner.Count == 0) ReleaseOutline();
                Notify();
            }
        }

        // Clears the player's pins. An open planting tool keeps showing its buildings.
        public void ClearAll()
        {
            foreach (var pin in _pins.Items) _planner.Remove(pin);
            _pins.Clear();
            _nextRefresh = 0;
            if (_planner.Count == 0) ReleaseOutline();
            Notify();
        }

        public bool ProcessInput()
        {
            if (!Available || Count == 0 || !_input.IsKeyDown(ClearKey)) return false;
            ClearAll();
            return true;
        }

        public void UpdateSingleton() => UpdateAt(Time.unscaledTime);

        // The frame update, given the clock, so the checks can run it outside the game.
        private void UpdateAt(float now)
        {
            if (!Available) return;
            try
            {
                if (now >= _nextSave && !SavePins()) _nextSave = now + 10f;
                if (_plantersChanged) ShowPlanters();
                if (_planner.Count == 0) return;
                if (_planner.Pending && now >= _nextRefresh)
                {
                    RefreshCells();
                    _nextRefresh = now + .2f;
                }
                if (_planner.Cells.Count > 0) _outline?.Draw();
            }
            catch (Exception error)
            {
                // Fail only this cosmetic feature if a later game update changes an internal API.
                _rendererFailed = true;
                Debug.LogError("[PersistentWorkAreas] Outline disabled for this map: " + error);
                Reset();
            }
        }

        // The planting tool shows the working areas of the buildings that plant its crop or tree, finished or not.
        private void ShowPlanters()
        {
            _plantersChanged = false;
            _planters.Clear();
            if (_planting != null)
                foreach (var workplace in _registry.GetAll<Workplace>())
                {
                    var planter = workplace.GetComponent<PlanterBuildingSpec>();
                    if (planter != null && PlantingRanges.Shows(_planting, planter.PlantableResourceGroup) && Supports(workplace))
                        _planters.Add(workplace.GetComponent<EntityComponent>());
                }
            _planner.Show(_planters);
            _planters.Clear();
            _nextRefresh = 0;
            if (_planner.Count == 0) ReleaseOutline();
        }

        private void RefreshCells()
        {
            _removed.Clear();
            bool redraw = _planner.Refresh(_removed);
            bool unpinned = false;
            foreach (var entity in _removed) unpinned |= _pins.Set(entity, false);
            if (unpinned) Notify();
            var cells = _planner.Cells;
            if ((redraw || _outline == null) && cells.Count > 0)
            {
                if (_outline == null) _outline = new NativeOutline(_blocks, _previews, _visibility, _mapSize, _specs);
                _outline.Update(cells);
            }
            if (_planner.Count == 0) ReleaseOutline();
        }

        private bool Describe(EntityComponent entity, out (Vector3? Center, bool Preview, bool Terrain) key, out CellBox reach)
        {
            key = default; reach = CellBox.Empty;
            if (!Supports(entity)) return false;
            var block = entity.GetComponent<BlockObject>();
            var access = entity.GetComponent<BuildingAccessible>();
            bool unfinished = !block.IsFinished;
            Vector3? center = unfinished ? access.CalculateAccess() : access.Accessible.UnblockedSingleAccessInstant;
            bool terrain = entity.GetComponent<BuildingWithTerrainRange>();
            key = (center, unfinished || _construction.InConstructionMode, terrain);
            if (!center.HasValue) return true;
            // A road-spill range follows its district's whole road network, so any navigation change can alter it.
            if (!terrain) { reach = CellBox.Unbounded; return true; }
            var grid = CoordinateSystem.WorldToGrid(center.Value);
            reach = CellBox.Around(grid.x, grid.y, grid.z, _reach);
            return true;
        }

        private void Query(EntityComponent entity, (Vector3? Center, bool Preview, bool Terrain) key, HashSet<Vector3Int> cells)
        {
            if (!key.Center.HasValue) return;
            var center = key.Center.Value;
            // The game refills one shared flow field per query, so each result is copied before the next query.
            cells.UnionWith(key.Terrain
                ? (key.Preview ? _navigation.GetTerrainPreviewNodesInRange(center) : _navigation.GetTerrainNodesInRange(center))
                : (key.Preview ? _navigation.GetRoadSpillPreviewNodesInRange(center) : _navigation.GetRoadSpillNodesInRange(center)));
        }

        private static BoundingBox ToBoundingBox(CellBox box)
        {
            var builder = new BoundingBox.Builder();
            builder.Expand(new Vector3Int(box.MinX, box.MinY, box.MinZ));
            builder.Expand(new Vector3Int(box.MaxX, box.MaxY, box.MaxZ));
            return builder.Build();
        }

        public void OnInstantNavMeshUpdated(NavMeshUpdate update) => _planner.Touch(update.Bounds, Touches);
        public void OnPreviewNavMeshUpdated(NavMeshUpdate update) => _planner.Touch(update.Bounds, Touches);
        [OnEvent] public void OnVisibleLevel(MaxVisibleLevelChangedEvent e) { _planner.Redraw(); _nextRefresh = 0; }
        [OnEvent] public void OnConstruction(ConstructionModeChangedEvent e) { _planner.InvalidateAll(); _nextRefresh = 0; }
        // The selected building already has its normal outline, so the pinned outline leaves it out.
        [OnEvent] public void OnSelected(SelectableObjectSelectedEvent e)
        {
            _planner.Select(e.SelectableObject.GetComponent<EntityComponent>());
            _nextRefresh = 0;
        }
        [OnEvent] public void OnUnselected(SelectableObjectUnselectedEvent e)
        {
            _planner.Select(null);
            _nextRefresh = 0;
        }
        [OnEvent] public void OnDeleted(EntityDeletedEvent e)
        {
            // The game has already taken a deleted building out of the entity registry, so the next scan drops it.
            if (_planting != null && e.Entity.GetComponent<PlanterBuildingSpec>() != null) _plantersChanged = true;
            var pin = e.Entity.GetComponent<EntityComponent>();
            if (_pins.Set(pin, false))
            {
                _planner.Remove(pin);
                _nextRefresh = 0;
                if (_planner.Count == 0) ReleaseOutline();
                Notify();
            }
        }
        // A planter loaded, or placed by another co-op player, while the planting tool is open.
        [OnEvent] public void OnInitialized(EntityInitializedEvent e)
        {
            if (_planting != null && e.Entity.GetComponent<PlanterBuildingSpec>() != null) _plantersChanged = true;
        }
        // ToolService.SwitchTool exits the old tool and enters the new one in the same call, so the buildings are found
        // on the next update: switching between crops of the same planters keeps their cached ranges.
        [OnEvent] public void OnToolEntered(ToolEnteredEvent e)
        {
            if (e.Tool is PlantingTool tool) Plant(tool.PlantableSpec?.ResourceGroup);
        }
        [OnEvent] public void OnToolExited(ToolExitedEvent e)
        {
            if (e.Tool is PlantingTool) Plant(null);
        }
        private void Plant(string group)
        {
            _planting = group;
            _plantersChanged = true;
        }

        // Pins come back from the pin file, never from the save, so each co-op player gets back only their own.
        // PostLoad passes Supports; the checks pass their own test, because Supports needs live game objects.
        private void RestorePins(Func<EntityComponent, bool> supports)
        {
            var settlement = _settlements.SettlementReference?.SettlementName;
            if (settlement == null) return;
            try
            {
                foreach (var pin in PinStore.Restore(_pinFile.Load(settlement), _entities.GetEntity, supports))
                    if (_pins.Set(pin, true)) _planner.Add(pin);
                _touchPinFile = true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[PersistentWorkAreas] Pinned areas not restored from " + _pinFile.FilePath + ": " + error.Message);
            }
            // Restoring is not a change: the file keeps remembered buildings this save lacks, for when a newer save loads.
            _pins.Changed = false;
        }

        // Writes the pins after they change, or marks the loaded settlement as recently used. A new game's settlement
        // has no name until the player gives one, so its pins wait until then. Leaving the map, loading or a renderer
        // failure forgets pins in memory only. False when the write failed; the change stays pending for a retry.
        private bool SavePins()
        {
            if ((!_pins.Changed && !_touchPinFile) || _pinFile == null) return true;
            var settlement = _settlements.SettlementReference?.SettlementName;
            if (settlement == null) return true;
            try
            {
                if (_pins.Changed) _pinFile.Save(settlement, _pins.Items.Select(pin => pin.EntityId));
                else _pinFile.Touch(settlement);
                _pins.Changed = false;
                _touchPinFile = false;
                return true;
            }
            catch (Exception error)
            {
                // Moving the settlement's entry first is only a courtesy, so it is not retried.
                _touchPinFile = false;
                if (!_saveWarned) Debug.LogWarning("[PersistentWorkAreas] Pinned areas not saved to " + _pinFile.FilePath + ": " + error.Message);
                _saveWarned = true;
                return false;
            }
        }

        private void Notify()
        {
            if (_clearButton != null)
            {
                _clearButton.text = ClearLabel();
                _clearButton.style.display = Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            Changed?.Invoke();
        }
        // Notify also runs inside the game's EntityDeletedEvent, so a translation with a broken {0} must not throw.
        private string ClearLabel()
        {
            try { return _loc.T(LocKeys.ClearAll, Count); }
            catch (FormatException error)
            {
                Debug.LogError("[PersistentWorkAreas] Bad " + LocKeys.ClearAll + " text: " + error.Message);
                return _loc.T(LocKeys.ClearAll);
            }
        }
        private void ReleaseOutline()
        {
            var outline = _outline;
            _outline = null;
            try { outline?.Dispose(); }
            catch (Exception error) { Debug.LogError("[PersistentWorkAreas] Renderer cleanup failed: " + error); }
            _planner.Clear();
        }
        // Saves a pending pin change, then forgets the pins and the planting tool's buildings, and releases the renderer.
        private void Reset()
        {
            SavePins();
            _planting = null;
            _plantersChanged = false;
            _pins.Clear();
            _pins.Changed = false;
            ReleaseOutline();
            Notify();
        }
        private void OnLoading(object sender, EventArgs e) => Dispose();
        public void Dispose()
        {
            if (!_active) return;
            _active = false;
            _loading.LoadingScreenEnabled -= OnLoading;
            _input.RemoveInputProcessor(this);
            _events.Unregister(this);
            Reset();
            _planner.Select(null);
            _clearButton?.RemoveFromHierarchy();
            _clearButton = null;
            Changed = null;
        }
    }
}
