using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A map outline: the axial (q, r) cells it contains, plus their bounding box.
    /// </summary>
    public interface IMapShape
    {
        IEnumerable<Vector2Int> Cells();
        RectInt AxialBounds { get; }
    }
}
