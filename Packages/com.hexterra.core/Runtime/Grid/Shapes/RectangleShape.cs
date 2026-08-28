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
            for (int row = 0; row < _height; row++)
            {
                int shift = row >> 1;
                for (int col = 0; col < _width; col++)
                    yield return new Vector2Int(col - shift, row);
            }
        }
    }
}
