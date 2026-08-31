using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// A* over a PathGraph. One instance is bound to one graph and reuses its working buffers
    /// across queries, so a single solver serves many searches but is not thread-safe. The
    /// heuristic is grid distance scaled by the model's base move cost, which stays admissible
    /// and consistent as long as every band is non-negative.
    /// </summary>
    public sealed class HexAStar
    {
        private readonly PathGraph _graph;
        private readonly MinHeap _open;
        private readonly int[] _gScore;
        private readonly int[] _cameFrom;
        private readonly bool[] _closed;

        public HexAStar(PathGraph graph)
        {
            _graph = graph;
            int count = graph.NodeCount;
            _open = new MinHeap(count);
            _gScore = new int[count];
            _cameFrom = new int[count];
            _closed = new bool[count];
        }

        /// <summary>
        /// Fills pathOut with node indices from start to goal inclusive and returns true, or
        /// clears pathOut and returns false when either index is out of range or no route exists
        /// under the given rules. start == goal returns a single-node path. When costOut is given
        /// it receives the cumulative cost at each path node, so its last entry is the total.
        /// </summary>
        public bool TryFindPath(TraversalModel rules, int start, int goal, List<int> pathOut, List<int> costOut = null)
        {
            pathOut.Clear();
            costOut?.Clear();

            int count = _graph.NodeCount;
            if (start < 0 || start >= count || goal < 0 || goal >= count)
                return false;

            Array.Fill(_gScore, int.MaxValue);
            Array.Fill(_cameFrom, -1);
            Array.Clear(_closed, 0, count);
            _open.Clear();

            Vector2Int goalCoord = _graph.CoordOf(goal);

            _gScore[start] = 0;
            int startH = Heuristic(start, goalCoord, rules);
            _open.PushOrDecrease(start, startH, startH);

            while (_open.Count > 0)
            {
                int current = _open.Pop();
                if (current == goal)
                    return Reconstruct(start, goal, pathOut, costOut);

                _closed[current] = true;
                int currentHeight = _graph.StepHeightOf(current);
                int currentG = _gScore[current];

                ReadOnlySpan<int> neighbours = _graph.NeighboursOf(current);
                foreach (var neighbour in neighbours)
                {
                    if (neighbour < 0 || _closed[neighbour])
                        continue;

                    int nextHeight = _graph.StepHeightOf(neighbour);
                    if (!rules.CanEnter(currentHeight, nextHeight))
                        continue;

                    int tentativeG = currentG + rules.MoveCost(currentHeight, nextHeight);
                    if (tentativeG >= _gScore[neighbour])
                        continue;

                    _gScore[neighbour] = tentativeG;
                    _cameFrom[neighbour] = current;

                    int h = Heuristic(neighbour, goalCoord, rules);
                    _open.PushOrDecrease(neighbour, tentativeG + h, h);
                }
            }

            return false;
        }

        private int Heuristic(int node, Vector2Int goalCoord, TraversalModel rules) =>
            HexMath.Distance(_graph.CoordOf(node), goalCoord) * rules.baseCost;

        private bool Reconstruct(int start, int goal, List<int> pathOut, List<int> costOut)
        {
            for (int node = goal; node != start; node = _cameFrom[node])
                pathOut.Add(node);

            pathOut.Add(start);
            pathOut.Reverse();

            if (costOut != null)
                foreach (int node in pathOut)
                    costOut.Add(_gScore[node]);

            return true;
        }
    }
}
