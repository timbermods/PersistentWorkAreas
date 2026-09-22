using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PersistentWorkAreas
{
    // Reference identity prevents a replacement building inheriting a deleted building's pin.
    internal sealed class PinSet<T> where T : class
    {
        private sealed class IdentityComparer : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
        internal static readonly IEqualityComparer<T> Identity = new IdentityComparer();
        private readonly HashSet<T> _items = new HashSet<T>(Identity);
        public int Count => _items.Count;
        public IEnumerable<T> Items => _items;
        // Set by every pin, unpin or clear that changes the set, so the pin file is written only after a change.
        public bool Changed { get; set; }
        public bool Contains(T item) => item != null && _items.Contains(item);
        public bool Set(T item, bool pinned)
        {
            if (item == null) return false;
            bool changed = pinned ? _items.Add(item) : _items.Remove(item);
            Changed |= changed;
            return changed;
        }
        public bool Clear()
        {
            if (_items.Count == 0) return false;
            _items.Clear();
            Changed = true;
            return true;
        }
    }
}
