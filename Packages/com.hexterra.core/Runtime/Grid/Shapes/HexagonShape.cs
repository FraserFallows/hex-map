using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Hexagonal footprint, sized by a single radius (hexes from the centre out to an edge)
    /// rather than the width and height the other shapes take.
    /// </summary>
    public class HexagonShape : IMapShape
    {
        private readonly int _radius;

        public HexagonShape(int radius) => _radius = Mathf.Max(0, radius);

        public RectInt AxialBounds => new(-_radius, -_radius, 2 * _radius + 1, 2 * _radius + 1);

        public IEnumerable<Vector2Int> Cells()
        {
            for (int q = -_radius; q <= _radius; q++)
            {
                int rMin = Mathf.Max(-_radius, -q - _radius);
                int rMax = Mathf.Min(_radius, -q + _radius);
                for (int r = rMin; r <= rMax; r++)
                    yield return new Vector2Int(q, r);
            }
        }
    }
}
