namespace HexTerra
{
    public class FlatSource : IHeightmapSource
    {
        private readonly int _height;

        public FlatSource(int height = 0)
        {
            _height = height;
        }

        public int[,] SampleHeightmap(int width, int height)
        {
            var heights = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    heights[x, y] = _height;

            return heights;
        }
    }
}
