using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// The pathfinding entry point: holds the traversal rules, rebuilds the PathGraph and solvers
    /// from a map on each generation, and answers path and reachability queries between axial
    /// coords. Wire RebuildGraph to HexMap.mapGenerated in the inspector.
    /// </summary>
    public sealed class Pathfinder : MonoBehaviour
    {
        [SerializeField, Min(1)] private int baseCost = 2;

        // [d] is the extra cost for a move that changes height by d steps (index 0 = flat).
        // Each list's length is its reach: a change past the last index is impassable.
        [SerializeField] private int[] ascentCost = { 0, 0, 1, 3, 5, 7, 9 };
        [SerializeField] private int[] descentCost = { 0, 0, 1, 1, 2, 3, 5 };

        // Default budget for cost-bounded queries, in MoveCost points.
        [SerializeField, Min(0)] private int movePoints = 16;

        /// <summary>
        /// The graph for the current map, or null before the first generation.
        /// </summary>
        public PathGraph Graph { get; private set; }

        public int MovePoints => movePoints;

        private HexAStar _solver;
        private HexDijkstra _reachability;

        /// <summary>
        /// Rebuilds the graph and solvers for a freshly generated map. Wire this to
        /// HexMap.mapGenerated.
        /// </summary>
        public void RebuildGraph(HexMap map)
        {
            Graph = PathGraphBuilder.Build(map.Cells);
            _solver = new HexAStar(Graph);
            _reachability = new HexDijkstra(Graph);
        }

        /// <summary>
        /// Fills pathOut with the node indices from one axial coord to another and returns true,
        /// or clears pathOut and returns false when there is no graph yet, either coord is off the
        /// map, or no route exists under the current rules. An optional costOut receives the
        /// cumulative cost at each path node.
        /// </summary>
        public bool TryFindPath(Vector2Int from, Vector2Int to, List<int> pathOut, List<int> costOut = null)
        {
            pathOut.Clear();
            costOut?.Clear();
            if (Graph == null) return false;

            int start = Graph.IndexOf(from);
            int goal = Graph.IndexOf(to);
            if (start < 0 || goal < 0) return false;

            return _solver.TryFindPath(BuildRules(), start, goal, pathOut, costOut);
        }

        /// <summary>
        /// Fills nodesOut with the node indices reachable from an axial coord for a cost of at most
        /// budget and returns true, or clears nodesOut and returns false when there is no graph yet
        /// or the coord is off the map. An optional costsOut receives the cost to reach each node.
        /// </summary>
        public bool TryFindReachable(Vector2Int from, int budget, List<int> nodesOut, List<int> costsOut = null)
        {
            nodesOut.Clear();
            costsOut?.Clear();
            if (Graph == null) return false;

            int start = Graph.IndexOf(from);
            if (start < 0) return false;

            return _reachability.TryFindReachable(BuildRules(), start, budget, nodesOut, costsOut);
        }

        public bool TryFindReachable(Vector2Int from, List<int> nodesOut, List<int> costsOut = null) =>
            TryFindReachable(from, movePoints, nodesOut, costsOut);

        private TraversalModel BuildRules() => new(baseCost, ascentCost, descentCost);

#if UNITY_EDITOR
        // A negative band makes a move cheaper than baseCost, breaking the A* heuristic and Dijkstra.
        private void OnValidate()
        {
            ClampToZero(ascentCost);
            ClampToZero(descentCost);
        }

        private static void ClampToZero(int[] bands)
        {
            for (int i = 0; i < bands.Length; i++)
                if (bands[i] < 0) bands[i] = 0;
        }
#endif
    }
}
