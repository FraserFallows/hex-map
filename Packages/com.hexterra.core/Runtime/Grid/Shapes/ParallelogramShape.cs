using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Parallelogram footprint: a <c>width</c> by <c>height</c> block in axial space with no
    /// per-row shift, so the hex basis shears it into a parallelogram with straight edges.
    /// <see cref="RectangleShape"/> is the jagged-edged variant.
    /// </summary>
    public class ParallelogramShape : IMapShape
    {
        private readonly int _width;
        private readonly int _height;

        public ParallelogramShape(int width, int height)
        {
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);
        }

        public RectInt AxialBounds => new(0, 0, _width, _height);

        public IEnumerable<Vector2Int> Cells()
        {
            for (int q = 0; q < _width; q++)
                for (int r = 0; r < _height; r++)
                    yield return new Vector2Int(q, r);
        }
    }
}
