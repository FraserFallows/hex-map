using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// The generated grid as data: a 2D array of hex GameObjects addressed by axial coordinate,
    /// plus the axial bounding box that maps a coordinate to its array slot. Holds no rendering
    /// or generation state.
    /// </summary>
    public class HexGrid
    {
        public GameObject[,] HexArray { get; }

        /// <summary>
        /// Axial bounding box of the grid. The array index for an axial coord is
        /// (q - AxialBounds.xMin, r - AxialBounds.yMin).
        /// </summary>
        public RectInt AxialBounds { get; }

        /// <summary>
        /// The cell nearest the map's centre, or null if that cell falls outside the shape.
        /// </summary>
        public GameObject MidpointHex { get; }

        public HexGrid(GameObject[,] hexArray, RectInt axialBounds)
        {
            HexArray = hexArray;
            AxialBounds = axialBounds;
            MidpointHex = ComputeMidpointHex();
        }

        /// <summary>
        /// The hex at the given axial coordinates, or null if they fall outside the grid.
        /// </summary>
        public GameObject GetHexAt(int q, int r)
        {
            if (HexArray == null) return null;

            int x = q - AxialBounds.xMin;
            int y = r - AxialBounds.yMin;
            if (x < 0 || x >= AxialBounds.width || y < 0 || y >= AxialBounds.height)
                return null;

            return HexArray[x, y];
        }

        private GameObject ComputeMidpointHex()
        {
            if (HexArray == null) return null;

            var centre = new Vector2(
                AxialBounds.xMin + (AxialBounds.width - 1) / 2f,
                AxialBounds.yMin + (AxialBounds.height - 1) / 2f);
            var axial = HexMath.Round(centre);
            return GetHexAt(axial.x, axial.y);
        }
    }
}
