using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding.Tests
{
    /// <summary>
    /// Builds hand-specified PathGraphs for the solver tests, plus a linear-scan cost oracle the
    /// heap-based solvers are checked against.
    /// </summary>
    internal static class PathGraphFactory
    {
        /// <summary>
        /// A PathGraph over the given cells, in the given order (cell i becomes node i). Neighbours
        /// are wired by axial adjacency; world positions come from HexMath.ToWorld.
        /// </summary>
        public static PathGraph FromCells(params (int q, int r, int stepHeight)[] cells)
        {
            int count = cells.Length;
            var coords = new Vector2Int[count];
            var stepHeights = new int[count];
            var worldPositions = new Vector3[count];
            var indexByCoord = new Dictionary<Vector2Int, int>(count);

            for (int i = 0; i < count; i++)
            {
                coords[i] = new Vector2Int(cells[i].q, cells[i].r);
                stepHeights[i] = cells[i].stepHeight;
                worldPositions[i] = HexMath.ToWorld(coords[i]);
                indexByCoord[coords[i]] = i;
            }

            var neighbours = new int[count * 6];
            for (int i = 0; i < count; i++)
                for (int d = 0; d < 6; d++)
                    neighbours[i * 6 + d] =
                        indexByCoord.GetValueOrDefault(coords[i] + HexMath.Directions[d], -1);

            return new PathGraph(coords, stepHeights, neighbours, worldPositions);
        }

        /// <summary>
        /// A width x height block of hexes (columns q in [0, width), rows r in [0, height)), each
        /// cell's step height from stepHeight(q, r). Node index is column-major.
        /// </summary>
        public static PathGraph Block(int width, int height, Func<int, int, int> stepHeight)
        {
            var cells = new (int, int, int)[width * height];
            int i = 0;
            for (int q = 0; q < width; q++)
                for (int r = 0; r < height; r++)
                    cells[i++] = (q, r, stepHeight(q, r));
            return FromCells(cells);
        }

        public static PathGraph FlatBlock(int width, int height) => Block(width, height, (_, _) => 0);

        /// <summary>
        /// Shortest cost from start to every node under the rules, by an independent linear-scan
        /// Dijkstra. int.MaxValue where a node cannot be reached.
        /// </summary>
        public static int[] ShortestCosts(PathGraph graph, TraversalModel rules, int start)
        {
            int count = graph.NodeCount;
            var dist = new int[count];
            Array.Fill(dist, int.MaxValue);
            dist[start] = 0;
            var settled = new bool[count];

            while (true)
            {
                int u = -1;
                int best = int.MaxValue;
                for (int i = 0; i < count; i++)
                    if (!settled[i] && dist[i] < best)
                    {
                        best = dist[i];
                        u = i;
                    }
                if (u < 0)
                    break;

                settled[u] = true;
                int from = graph.StepHeightOf(u);
                var neighbours = graph.NeighboursOf(u);
                for (int d = 0; d < 6; d++)
                {
                    int v = neighbours[d];
                    if (v < 0)
                        continue;

                    int to = graph.StepHeightOf(v);
                    if (!rules.CanEnter(from, to))
                        continue;

                    int step = dist[u] + rules.MoveCost(from, to);
                    if (step < dist[v])
                        dist[v] = step;
                }
            }

            return dist;
        }
    }
}
