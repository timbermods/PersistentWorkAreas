using System;
using System.Collections.Generic;
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
using Timberborn.SceneLoading;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.UILayoutSystem;
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
        private readonly float _reach;
        private readonly INavigationRangeService _navigation;
        private readonly ConstructionModeService _construction;
        private readonly IBlockService _blocks;
        private readonly PreviewBlockService _previews;
        private readonly ILevelVisibilityService _visibility;
        private readonly MapSize _mapSize;
        private readonly ISpecService _specs;
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
        public event Action Changed;
        public int Count => _pins.Count;
        public bool Available => _active && !_rendererFailed;

        public WorkAreaService(INavigationRangeService navigation, NavigationDistance distance, ConstructionModeService construction,
            IBlockService blocks, PreviewBlockService previews, ILevelVisibilityService visibility,
            MapSize mapSize, ISpecService specs, EventBus events, LoadingScreen loading,
            UILayout layout, InputService input, ILoc loc)
        {
            _navigation = navigation; _construction = construction; _blocks = blocks;
            _previews = previews; _visibility = visibility; _mapSize = mapSize; _specs = specs;
            _events = events; _loading = loading; _layout = layout; _input = input; _loc = loc;
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
                if (_pins.Count == 0) ReleaseOutline();
                Notify();
            }
        }

        public void ClearAll()
        {
            _pins.Clear();
            ReleaseOutline();
            Notify();
        }

        public bool ProcessInput()
        {
            if (!Available || Count == 0 || !_input.IsKeyDown(ClearKey)) return false;
            ClearAll();
            return true;
        }

        public void UpdateSingleton()
        {
            if (!Available || Count == 0) return;
            try
            {
                if (_planner.Pending && Time.unscaledTime >= _nextRefresh)
                {
                    RefreshCells();
                    _nextRefresh = Time.unscaledTime + .2f;
                }
                if (_planner.Cells.Count > 0) _outline?.Draw();
            }
            catch (Exception error)
            {
                // Fail only this cosmetic feature if a later game update changes an internal API.
                _rendererFailed = true;
                Debug.LogError("[PersistentWorkAreas] Outline disabled for this map: " + error);
                ClearAll();
            }
        }

        private void RefreshCells()
        {
            _removed.Clear();
            bool redraw = _planner.Refresh(_removed);
            foreach (var entity in _removed) _pins.Set(entity, false);
            if (_removed.Count > 0) Notify();
            var cells = _planner.Cells;
            if ((redraw || _outline == null) && cells.Count > 0)
            {
                if (_outline == null) _outline = new NativeOutline(_blocks, _previews, _visibility, _mapSize, _specs);
                _outline.Update(cells);
            }
            if (Count == 0) ReleaseOutline();
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
            var pin = e.Entity.GetComponent<EntityComponent>();
            if (_pins.Set(pin, false))
            {
                _planner.Remove(pin);
                _nextRefresh = 0;
                if (Count == 0) ReleaseOutline();
                Notify();
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
        private void OnLoading(object sender, EventArgs e) => Dispose();
        public void Dispose()
        {
            if (!_active) return;
            _active = false;
            _loading.LoadingScreenEnabled -= OnLoading;
            _input.RemoveInputProcessor(this);
            _events.Unregister(this);
            ClearAll();
            _planner.Select(null);
            _clearButton?.RemoveFromHierarchy();
            _clearButton = null;
            Changed = null;
        }
    }
}
