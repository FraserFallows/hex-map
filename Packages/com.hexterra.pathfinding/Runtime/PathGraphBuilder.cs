using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Builds a PathGraph from a generated map's cells: one node per cell, adjacency resolved by
    /// axial coordinate against HexMath.Directions. Call once per generation.
    /// </summary>
    public static class PathGraphBuilder
    {
        public static PathGraph Build(IReadOnlyList<HexCell> cells)
        {
            int count = cells.Count;
            int sides = HexMath.Directions.Length;

            var coords = new Vector2Int[count];
            var stepHeights = new int[count];
            var worldPositions = new Vector3[count];
            var neighbours = new int[count * sides];
            var indexByCoord = new Dictionary<Vector2Int, int>(count);

            for (int i = 0; i < count; i++)
            {
                HexCell cell = cells[i];
                var coord = new Vector2Int(cell.q, cell.r);

                coords[i] = coord;
                stepHeights[i] = cell.stepHeight;
                worldPositions[i] = cell.transform.position;
                indexByCoord[coord] = i;
            }

            for (int i = 0; i < count; i++)
            {
                for (int side = 0; side < sides; side++)
                {
                    Vector2Int adjacent = coords[i] + HexMath.Directions[side];
                    neighbours[i * sides + side] = indexByCoord.GetValueOrDefault(adjacent, -1);
                }
            }

            return new PathGraph(coords, stepHeights, neighbours, worldPositions);
        }
    }
}
