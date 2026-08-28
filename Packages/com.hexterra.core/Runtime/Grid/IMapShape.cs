using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A map outline: the axial (q, r) cells it contains and their bounding box, used to
    /// size and index the backing array.
    /// </summary>
    public interface IMapShape
    {
        IEnumerable<Vector2Int> Cells();
        RectInt AxialBounds { get; }
    }
}
