using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// The pathfinding entry point: holds the traversal rules, rebuilds the PathGraph and solver
    /// from a map on each generation, and answers coordinate-to-coordinate path queries. Wire
    /// RebuildGraph to HexMap.mapGenerated in the inspector.
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

        /// <summary>
        /// Rebuilds the graph and solver for a freshly generated map. Wire this to
        /// HexMap.mapGenerated.
        /// </summary>
        public void RebuildGraph(HexMap map)
        {
            Graph = PathGraphBuilder.Build(map.Cells);
            _solver = new HexAStar(Graph);
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

        private TraversalModel BuildRules() => new(baseCost, ascentCost, descentCost);

#if UNITY_EDITOR
        // A negative band would let a move cost less than baseCost and break the A* heuristic.
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
