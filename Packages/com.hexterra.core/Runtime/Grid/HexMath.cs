using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Axial hex maths for flat-top hexes. Coordinates are stored as axial (q, r). The cube
    /// form (x, y, z) = (q, -q - r, r), which always sums to zero, is used internally where the
    /// three-axis symmetry is cleaner.
    /// </summary>
    public static class HexMath
    {
        private const float ColumnSpacing = 1.5f;
        private const float RowHalf = 0.866025404f;

        /// <summary>
        /// Neighbour offsets, clockwise from the top edge. The index doubles as a wall
        /// orientation (index * 60 degrees), so this order is load-bearing.
        /// </summary>
        public static readonly Vector2Int[] Directions =
        {
            new(0, 1), new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1)
        };

        /// <summary>
        /// Converts an axial coordinate to its cube form, filling in the derived y = -q - r axis.
        /// </summary>
        public static Vector3Int ToCube(Vector2Int axial) => new(axial.x, -axial.x - axial.y, axial.y);

        /// <summary>
        /// Converts a cube coordinate back to axial, dropping the redundant y-axis.
        /// </summary>
        public static Vector2Int ToAxial(Vector3Int cube) => new(cube.x, cube.z);

        /// <summary>
        /// Grid-step distance between two hexes: the number of single-hex moves that separate them.
        /// </summary>
        public static int Distance(Vector2Int from, Vector2Int to)
        {
            Vector3Int cubeDelta = ToCube(from) - ToCube(to);
            return (Mathf.Abs(cubeDelta.x) + Mathf.Abs(cubeDelta.y) + Mathf.Abs(cubeDelta.z)) / 2;
        }

        /// <summary>
        /// World-space centre of the hex at a (possibly fractional) axial coordinate. Lies on the
        /// XZ plane, so Y is always zero.
        /// </summary>
        public static Vector3 ToWorld(float q, float r) => new(ColumnSpacing * q, 0f, RowHalf * (q + 2f * r));

        public static Vector3 ToWorld(Vector2Int axial) => ToWorld(axial.x, axial.y);

        /// <summary>
        /// Axial coordinate of the hex covering a world position, rounded to the nearest cell.
        /// Reads the XZ plane only, so Y is ignored.
        /// </summary>
        public static Vector2Int FromWorld(Vector3 world)
        {
            float q = world.x / ColumnSpacing;
            float r = (world.z / RowHalf - q) / 2f;
            return Round(new Vector2(q, r));
        }

        /// <summary>
        /// Rounds a fractional axial coordinate to the nearest cell. Rounds in cube space so
        /// the three axes stay consistent (x + y + z = 0).
        /// </summary>
        public static Vector2Int Round(Vector2 axial)
        {
            float fractionalX = axial.x;
            float fractionalZ = axial.y;
            float fractionalY = -fractionalX - fractionalZ;

            int roundedX = Mathf.RoundToInt(fractionalX);
            int roundedY = Mathf.RoundToInt(fractionalY);
            int roundedZ = Mathf.RoundToInt(fractionalZ);

            float xRoundingError = Mathf.Abs(roundedX - fractionalX);
            float yRoundingError = Mathf.Abs(roundedY - fractionalY);
            float zRoundingError = Mathf.Abs(roundedZ - fractionalZ);

            // Re-derive whichever axis rounded furthest from the other two, restoring
            // x + y + z = 0. Only x and z form the axial result, so when y rounded furthest
            // both are already correct and nothing needs fixing.
            if (xRoundingError > yRoundingError && xRoundingError > zRoundingError)
                roundedX = -roundedY - roundedZ;
            else if (zRoundingError > yRoundingError)
                roundedZ = -roundedX - roundedY;

            return new Vector2Int(roundedX, roundedZ);
        }
    }
}
