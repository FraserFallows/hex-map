namespace HexTerra
{
    /// <summary>
    /// Supplies a per-cell heightmap: step-heights over a width x height area.
    /// </summary>
    public interface IHeightmapSource
    {
        int[,] SampleHeightmap(int width, int height);
    }
}
