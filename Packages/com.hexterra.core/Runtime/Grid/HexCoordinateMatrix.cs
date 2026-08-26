namespace HexTerra
{
    /// <summary>
    /// Transformation Matrix Data for finding hex neighbours
    /// </summary>
    public static class HexCoordinateMatrix
    {
        // x-position parity-based Matrices for index transformation

        public static readonly int[] FirstX = { 0, 1, 1, 0, -1, -1 };

        public static readonly int[] FirstZEven = { 1, 0, -1, -1, -1, 0 };
        public static readonly int[] FirstZOdd = { 1, 1, 0, -1, 0, 1 };

        public static readonly int[] SecondX = { 0, 1, 2, 2, 2, 1, 0, -1, -2, -2, -2, -1 };

        public static readonly int[] SecondZEven = { 2, 1, 1, 0, -1, -2, -2, -2, -1, 0, 1, 1 };
        public static readonly int[] SecondZOdd = { 2, 2, 1, 0, -1, -1, -2, -1, -1, 0, 1, 2 };
    }
}
