using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Immutable snapshot of a generated hex map for pathfinding: one node per cell at a dense
    /// index, each with its axial coord, step height, world position, and its six neighbour
    /// indices. Built once per generation.
    /// </summary>
    public sealed class PathGraph
    {
        public int NodeCount => _coords.Length;

        private readonly Vector2Int[] _coords;
        private readonly int[] _stepHeights;
        private readonly int[] _neighbours;
        private readonly Vector3[] _worldPositions;
        private readonly Dictionary<Vector2Int, int> _indexByCoord;

        private const int Sides = 6;

        // Arrays are stored as given, not copied: the caller must not mutate them afterwards.
        // neighbours holds Sides entries per node at [node * Sides .. node * Sides + Sides).
        public PathGraph(Vector2Int[] coords, int[] stepHeights, int[] neighbours, Vector3[] worldPositions)
        {
            _coords = coords;
            _stepHeights = stepHeights;
            _neighbours = neighbours;
            _worldPositions = worldPositions;

            _indexByCoord = new Dictionary<Vector2Int, int>(coords.Length);
            for (int i = 0; i < coords.Length; i++)
                _indexByCoord[coords[i]] = i;
        }

        public Vector2Int CoordOf(int node) => _coords[node];

        public int StepHeightOf(int node) => _stepHeights[node];

        public Vector3 WorldPositionOf(int node) => _worldPositions[node];

        /// <summary>
        /// The six neighbour node indices for a node, clockwise from the top edge in
        /// HexMath.Directions order. An entry is -1 where the map has no cell on that side.
        /// </summary>
        public ReadOnlySpan<int> NeighboursOf(int node) => new(_neighbours, node * Sides, Sides);

        /// <summary>
        /// The node index at an axial coord, or -1 if no cell sits there.
        /// </summary>
        public int IndexOf(Vector2Int coord) => _indexByCoord.GetValueOrDefault(coord, -1);
    }
}
