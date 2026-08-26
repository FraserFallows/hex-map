using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Trivial IElevationSource that returns a constant step-height everywhere — a flat baseline,
    /// useful for tests or as a comparison against procedural sources.
    /// </summary>
    public class FlatElevationSource : IElevationSource
    {
        private readonly int _height;

        public FlatElevationSource(int height = 0)
        {
            _height = height;
        }

        public int[,] SampleElevation(int gridSize, Vector2 offset)
        {
            var heights = new int[gridSize, gridSize];

            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    heights[x, y] = _height;

            return heights;
        }
    }
}
