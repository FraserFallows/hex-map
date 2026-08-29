using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Rectangular footprint: every row is <c>width</c> hexes, with a jagged left/right edge
    /// from the per-row axial shift. <see cref="ParallelogramShape"/> is the un-jagged rhombus.
    /// </summary>
    public class RectangleShape : IMapShape
    {
        private readonly int _width;
        private readonly int _height;

        public RectangleShape(int width, int height)
        {
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);
        }

        public RectInt AxialBounds
        {
            get
            {
                int qMin = -((_height - 1) >> 1);
                return new RectInt(qMin, 0, _width - qMin, _height);
            }
        }

        public IEnumerable<Vector2Int> Cells()
        {
            for (int r = 0; r < _height; r++)
            {
                int shift = r >> 1;
                for (int q = 0; q < _width; q++)
                    yield return new Vector2Int(q - shift, r);
            }
        }
    }
}
