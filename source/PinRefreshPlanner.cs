using System;
using System.Collections.Generic;

namespace PersistentWorkAreas
{
    // Inclusive grid bounds (x, y, z with z up), the same shape as the game's BoundingBox.
    internal readonly struct CellBox
    {
        public static readonly CellBox Empty = new CellBox(0, 0, 0, -1, -1, -1);
        public static readonly CellBox Unbounded = new CellBox(int.MinValue, int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, int.MaxValue);
        public readonly int MinX, MinY, MinZ, MaxX, MaxY, MaxZ;

        public CellBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            MinX = minX; MinY = minY; MinZ = minZ; MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        public static CellBox Point(int x, int y, int z) => new CellBox(x, y, z, x, y, z);
        // BuildingTerrainRange.UpdateBoundingBox builds its box the same way: floor(access - reach) to ceil(access + reach).
        public static CellBox Around(float x, float y, float z, float reach) => new CellBox(
            (int)Math.Floor(x - reach), (int)Math.Floor(y - reach), (int)Math.Floor(z - reach),
            (int)Math.Ceiling(x + reach), (int)Math.Ceiling(y + reach), (int)Math.Ceiling(z + reach));
        public bool IsEmpty => MinX > MaxX || MinY > MaxY || MinZ > MaxZ;
        public bool IsUnbounded => MinX == int.MinValue && MinY == int.MinValue && MinZ == int.MinValue &&
            MaxX == int.MaxValue && MaxY == int.MaxValue && MaxZ == int.MaxValue;
        public bool Intersects(CellBox other) => !IsEmpty && !other.IsEmpty &&
            MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY && MinZ <= other.MaxZ && MaxZ >= other.MinZ;
        public CellBox Union(CellBox other) => IsEmpty ? other : other.IsEmpty ? this : new CellBox(
            Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY), Math.Min(MinZ, other.MinZ),
            Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY), Math.Max(MaxZ, other.MaxZ));
    }

    // Decides which pinned ranges need a fresh navigation query and when the combined outline must be rebuilt.
    // Each pin caches its cells, so a navigation change re-queries only the pins it can reach, and a
    // selection change rebuilds the outline from the cache. Unity-free so the checks can count the work.
    internal sealed class PinRefreshPlanner<TPin, TKey, TCell> where TPin : class
    {
        // Returns false when the pin can no longer be shown. The key holds everything the query depends on
        // besides the navigation mesh; reach bounds the navigation changes that can alter the range.
        public delegate bool DescribePin(TPin pin, out TKey key, out CellBox reach);
        public delegate void QueryRange(TPin pin, TKey key, HashSet<TCell> cells);

        private sealed class Entry
        {
            public bool Described;
            public TKey Key;
            public bool Stale = true;
            public CellBox Reach = CellBox.Empty;
            // Bounds of the cached cells. Blocking objects and previews placed there change the outline.
            public CellBox Extent = CellBox.Empty;
            public HashSet<TCell> Cells = new HashSet<TCell>();
        }

        private readonly Dictionary<TPin, Entry> _entries = new Dictionary<TPin, Entry>(PinSet<TPin>.Identity);
        private readonly EqualityComparer<TKey> _keys = EqualityComparer<TKey>.Default;
        private readonly DescribePin _describe;
        private readonly QueryRange _query;
        private readonly Func<TCell, CellBox> _cellBox;
        private HashSet<TCell> _cells = new HashSet<TCell>();
        private HashSet<TCell> _next = new HashSet<TCell>();
        private HashSet<TCell> _queried = new HashSet<TCell>();
        private TPin _selected;
        private bool _pending;
        private bool _union;
        private bool _redraw;

        public PinRefreshPlanner(DescribePin describe, QueryRange query, Func<TCell, CellBox> cellBox)
        {
            _describe = describe; _query = query; _cellBox = cellBox;
        }

        public int Count => _entries.Count;
        public bool Pending => _pending;
        // The combined range of every pin except the selected one, which the game outlines itself.
        public IReadOnlyCollection<TCell> Cells => _cells;

        public bool Add(TPin pin)
        {
            if (pin == null || _entries.ContainsKey(pin)) return false;
            _entries.Add(pin, new Entry());
            _pending = true;
            return true;
        }

        public bool Remove(TPin pin)
        {
            if (pin == null || !_entries.Remove(pin)) return false;
            _pending = _union = true;
            return true;
        }

        public void Clear()
        {
            _entries.Clear();
            _cells.Clear(); _next.Clear(); _queried.Clear();
            _pending = _union = _redraw = false;
        }

        public void Select(TPin pin)
        {
            bool pinned = Has(_selected) || Has(pin);
            _selected = pin;
            if (pinned) _pending = _union = true;
        }

        // Construction mode switches finished buildings between the live and preview navigation graphs.
        public void InvalidateAll()
        {
            if (_entries.Count == 0) return;
            foreach (var entry in _entries.Values) entry.Stale = true;
            _pending = _redraw = true;
        }

        // The visible level changes what is drawn, never which cells are in range.
        public void Redraw()
        {
            if (_entries.Count == 0) return;
            _pending = _redraw = true;
        }

        // A navigation change with the given bounds. Only pins it can reach are re-queried.
        public void Touch<TBounds>(TBounds bounds, Func<CellBox, TBounds, bool> intersects)
        {
            if (_entries.Count == 0) return;
            // Also re-describe every pin on the next pass, which catches access and graph changes cheaply.
            _pending = true;
            foreach (var pair in _entries)
            {
                var entry = pair.Value;
                if (!entry.Stale && Hits(entry.Reach, bounds, intersects)) entry.Stale = true;
                if (!_redraw && !ReferenceEquals(pair.Key, _selected) && Hits(entry.Extent, bounds, intersects)) _redraw = true;
            }
        }

        // Adds pins that can no longer be shown to dropped and forgets them.
        // Returns true when the outline must be rebuilt from Cells.
        public bool Refresh(List<TPin> dropped)
        {
            _pending = false;
            bool union = _union;
            _union = false;
            int first = dropped.Count;
            foreach (var pair in _entries)
            {
                var pin = pair.Key;
                var entry = pair.Value;
                if (!_describe(pin, out var key, out var reach)) { dropped.Add(pin); continue; }
                if (!entry.Described || !_keys.Equals(entry.Key, key))
                {
                    entry.Described = true;
                    entry.Key = key;
                    entry.Stale = true;
                }
                // The selected pin is not drawn, so its query waits until it is deselected.
                if (entry.Stale && !ReferenceEquals(pin, _selected))
                {
                    entry.Stale = false;
                    _queried.Clear();
                    _query(pin, key, _queried);
                    if (!entry.Cells.SetEquals(_queried))
                    {
                        var cached = entry.Cells; entry.Cells = _queried; _queried = cached;
                        entry.Extent = CellBox.Empty;
                        foreach (var cell in entry.Cells) entry.Extent = entry.Extent.Union(_cellBox(cell));
                        union = true;
                    }
                }
                entry.Reach = reach.Union(entry.Extent);
            }
            for (int i = first; i < dropped.Count; i++) union |= _entries.Remove(dropped[i]);
            bool redraw = _redraw;
            _redraw = false;
            if (!union) return redraw;
            _next.Clear();
            foreach (var pair in _entries)
                if (!ReferenceEquals(pair.Key, _selected)) _next.UnionWith(pair.Value.Cells);
            if (_next.SetEquals(_cells)) return redraw;
            var drawn = _cells; _cells = _next; _next = drawn;
            return true;
        }

        private bool Has(TPin pin) => pin != null && _entries.ContainsKey(pin);

        private static bool Hits<TBounds>(CellBox box, TBounds bounds, Func<CellBox, TBounds, bool> intersects) =>
            !box.IsEmpty && (box.IsUnbounded || intersects(box, bounds));
    }
}
