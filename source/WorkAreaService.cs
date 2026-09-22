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
        private readonly PinSet<EntityComponent> _pins = new PinSet<EntityComponent>();
        private readonly HashSet<Vector3Int> _cells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _nextCells = new HashSet<Vector3Int>();
        private readonly List<EntityComponent> _removed = new List<EntityComponent>();
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
        private EntityComponent _selected;
        private bool _active;
        private bool _dirty;
        private bool _geometryDirty;
        private float _nextRefresh;
        private bool _rendererFailed;
        public event Action Changed;
        public int Count => _pins.Count;
        public bool Available => _active && !_rendererFailed;

        public WorkAreaService(INavigationRangeService navigation, ConstructionModeService construction,
            IBlockService blocks, PreviewBlockService previews, ILevelVisibilityService visibility,
            MapSize mapSize, ISpecService specs, EventBus events, LoadingScreen loading,
            UILayout layout, InputService input, ILoc loc)
        {
            _navigation = navigation; _construction = construction; _blocks = blocks;
            _previews = previews; _visibility = visibility; _mapSize = mapSize; _specs = specs;
            _events = events; _loading = loading; _layout = layout; _input = input; _loc = loc;
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
            if (_pins.Set(entity.GetComponent<EntityComponent>(), value))
            {
                Invalidate();
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
                if (_dirty && Time.unscaledTime >= _nextRefresh)
                {
                    RefreshCells();
                    _nextRefresh = Time.unscaledTime + .2f;
                }
                if (_cells.Count > 0) _outline?.Draw();
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
            _dirty = false;
            _nextCells.Clear();
            _removed.Clear();
            foreach (var entity in _pins.Items)
            {
                if (!Supports(entity)) { _removed.Add(entity); continue; }
                // The selected building already has its normal outline.
                if (ReferenceEquals(entity, _selected)) continue;
                var block = entity.GetComponent<BlockObject>();
                var access = entity.GetComponent<BuildingAccessible>();
                bool unfinished = !block.IsFinished;
                Vector3? center = unfinished ? access.CalculateAccess() : access.Accessible.UnblockedSingleAccessInstant;
                if (!center.HasValue) continue;
                bool terrain = entity.GetComponent<BuildingWithTerrainRange>();
                bool preview = unfinished || _construction.InConstructionMode;
                IEnumerable<Vector3Int> range = terrain
                    ? (preview ? _navigation.GetTerrainPreviewNodesInRange(center.Value) : _navigation.GetTerrainNodesInRange(center.Value))
                    : (preview ? _navigation.GetRoadSpillPreviewNodesInRange(center.Value) : _navigation.GetRoadSpillNodesInRange(center.Value));
                _nextCells.UnionWith(range);
            }
            foreach (var entity in _removed) _pins.Set(entity, false);
            if (_removed.Count > 0) Notify();
            if (_geometryDirty || !_cells.SetEquals(_nextCells))
            {
                _geometryDirty = false;
                _cells.Clear();
                _cells.UnionWith(_nextCells);
                if (_cells.Count > 0)
                {
                    if (_outline == null) _outline = new NativeOutline(_blocks, _previews, _visibility, _mapSize, _specs);
                    _outline.Update(_cells);
                }
            }
            if (Count == 0) ReleaseOutline();
        }

        private void Invalidate() { _dirty = true; _geometryDirty = true; }
        public void OnInstantNavMeshUpdated(NavMeshUpdate update) => Invalidate();
        public void OnPreviewNavMeshUpdated(NavMeshUpdate update) => Invalidate();
        [OnEvent] public void OnVisibleLevel(MaxVisibleLevelChangedEvent e) { Invalidate(); _nextRefresh = 0; }
        [OnEvent] public void OnConstruction(ConstructionModeChangedEvent e) { Invalidate(); _nextRefresh = 0; }
        [OnEvent] public void OnSelected(SelectableObjectSelectedEvent e)
        {
            _selected = e.SelectableObject.GetComponent<EntityComponent>();
            Invalidate(); _nextRefresh = 0;
        }
        [OnEvent] public void OnUnselected(SelectableObjectUnselectedEvent e)
        {
            _selected = null;
            Invalidate(); _nextRefresh = 0;
        }
        [OnEvent] public void OnDeleted(EntityDeletedEvent e)
        {
            if (_pins.Set(e.Entity.GetComponent<EntityComponent>(), false))
            {
                Invalidate(); _nextRefresh = 0;
                if (Count == 0) ReleaseOutline();
                Notify();
            }
        }

        private void Notify()
        {
            if (_clearButton != null)
            {
                _clearButton.text = _loc.T(LocKeys.ClearAll, Count);
                _clearButton.style.display = Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            Changed?.Invoke();
        }
        private void ReleaseOutline()
        {
            var outline = _outline;
            _outline = null;
            try { outline?.Dispose(); }
            catch (Exception error) { Debug.LogError("[PersistentWorkAreas] Renderer cleanup failed: " + error); }
            _cells.Clear(); _nextCells.Clear();
            _dirty = false; _geometryDirty = false;
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
            _selected = null;
            _clearButton?.RemoveFromHierarchy();
            _clearButton = null;
            Changed = null;
        }
    }
}
