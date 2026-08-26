using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Supplies per-cell step-height values for a hex grid of a given size.
    /// </summary>
    public interface IElevationSource
    {
        /// <summary>
        /// Samples step-heights across a gridSize x gridSize area, offset by the given translation.
        /// </summary>
        int[,] SampleElevation(int gridSize, Vector2 offset);
    }
}
