using System;
using System.Collections.Generic;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Cost-bounded Dijkstra over a PathGraph: from a start node, every node reachable for a cost
    /// of at most a given budget, each with that cost. One instance is bound to one graph and
    /// reuses its working buffers across queries, so a single instance serves many queries but is
    /// not thread-safe.
    /// </summary>
    public sealed class HexDijkstra
    {
        private readonly PathGraph _graph;
        private readonly MinHeap _open;
        private readonly int[] _cost;
        private readonly bool[] _closed;

        public HexDijkstra(PathGraph graph)
        {
            _graph = graph;
            int count = graph.NodeCount;
            _open = new MinHeap(count);
            _cost = new int[count];
            _closed = new bool[count];
        }

        /// <summary>
        /// Fills nodesOut with every node reachable from start for a cost of at most budget and
        /// returns true, or clears nodesOut and returns false when start is out of range. Nodes
        /// come out in ascending cost order, so start is first at cost 0. When costsOut is given it
        /// receives the cost to reach the node at the same index.
        /// </summary>
        public bool TryFindReachable(TraversalModel rules, int start, int budget, List<int> nodesOut, List<int> costsOut = null)
        {
            nodesOut.Clear();
            costsOut?.Clear();

            int count = _graph.NodeCount;
            if (start < 0 || start >= count)
                return false;

            Array.Fill(_cost, int.MaxValue);
            Array.Clear(_closed, 0, count);
            _open.Clear();

            // No heuristic: cost is the entire sort key, the heap's secondary slot is unused.
            _cost[start] = 0;
            _open.PushOrDecrease(start, 0, 0);

            while (_open.Count > 0)
            {
                int current = _open.Pop();
                _closed[current] = true;

                int currentCost = _cost[current];
                nodesOut.Add(current);
                costsOut?.Add(currentCost);

                int currentHeight = _graph.StepHeightOf(current);

                ReadOnlySpan<int> neighbours = _graph.NeighboursOf(current);
                foreach (var neighbour in neighbours)
                {
                    if (neighbour < 0 || _closed[neighbour])
                        continue;

                    int nextHeight = _graph.StepHeightOf(neighbour);
                    if (!rules.CanEnter(currentHeight, nextHeight))
                        continue;

                    int tentativeCost = currentCost + rules.MoveCost(currentHeight, nextHeight);
                    if (tentativeCost > budget || tentativeCost >= _cost[neighbour])
                        continue;

                    _cost[neighbour] = tentativeCost;
                    _open.PushOrDecrease(neighbour, tentativeCost, 0);
                }
            }

            return true;
        }
    }
}
