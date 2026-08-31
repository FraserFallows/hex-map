using System;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Binary min-heap of node indices keyed by a primary then a secondary int, with O(log n)
    /// decrease-key. Node indices must be dense in [0, capacity): an array of that size maps each
    /// node to its heap slot so its key can be lowered in place. Holds at most one entry per node.
    /// </summary>
    internal sealed class MinHeap
    {
        public int Count => _count;

        private readonly int[] _nodes;
        private readonly int[] _primary;
        private readonly int[] _secondary;
        private readonly int[] _slotOfNode;
        private int _count;

        public MinHeap(int capacity)
        {
            _nodes = new int[capacity];
            _primary = new int[capacity];
            _secondary = new int[capacity];
            _slotOfNode = new int[capacity];
            Array.Fill(_slotOfNode, -1);
        }

        public bool Contains(int node) => _slotOfNode[node] >= 0;

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _slotOfNode[_nodes[i]] = -1;
            _count = 0;
        }

        /// <summary>
        /// Inserts a node, or lowers its key in place when it is already present and the new key
        /// sorts earlier. A key that sorts later than the current one is ignored.
        /// </summary>
        public void PushOrDecrease(int node, int primary, int secondary)
        {
            int slot = _slotOfNode[node];
            if (slot >= 0)
            {
                if (!Sorts(primary, secondary, _primary[slot], _secondary[slot]))
                    return;

                _primary[slot] = primary;
                _secondary[slot] = secondary;
                SiftUp(slot);
                return;
            }

            slot = _count++;
            _nodes[slot] = node;
            _primary[slot] = primary;
            _secondary[slot] = secondary;
            _slotOfNode[node] = slot;
            SiftUp(slot);
        }

        /// <summary>
        /// Removes and returns the earliest-sorting node. Call only when Count is positive.
        /// </summary>
        public int Pop()
        {
            int root = _nodes[0];
            _slotOfNode[root] = -1;

            int last = --_count;
            if (last > 0)
            {
                _nodes[0] = _nodes[last];
                _primary[0] = _primary[last];
                _secondary[0] = _secondary[last];
                _slotOfNode[_nodes[0]] = 0;
                SiftDown(0);
            }

            return root;
        }

        // Primary ascending; secondary breaks ties. Strict, so equal keys keep their place.
        private static bool Sorts(int primary, int secondary, int thanPrimary, int thanSecondary) =>
            primary < thanPrimary || (primary == thanPrimary && secondary < thanSecondary);

        private void SiftUp(int slot)
        {
            while (slot > 0)
            {
                int parent = (slot - 1) >> 1;
                if (!Sorts(_primary[slot], _secondary[slot], _primary[parent], _secondary[parent]))
                    break;

                Swap(slot, parent);
                slot = parent;
            }
        }

        private void SiftDown(int slot)
        {
            while (true)
            {
                int left = (slot << 1) + 1;
                if (left >= _count)
                    break;

                int right = left + 1;
                int child = right < _count && Sorts(_primary[right], _secondary[right], _primary[left], _secondary[left])
                    ? right
                    : left;

                if (!Sorts(_primary[child], _secondary[child], _primary[slot], _secondary[slot]))
                    break;

                Swap(slot, child);
                slot = child;
            }
        }

        private void Swap(int a, int b)
        {
            (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
            (_primary[a], _primary[b]) = (_primary[b], _primary[a]);
            (_secondary[a], _secondary[b]) = (_secondary[b], _secondary[a]);
            _slotOfNode[_nodes[a]] = a;
            _slotOfNode[_nodes[b]] = b;
        }
    }
}
