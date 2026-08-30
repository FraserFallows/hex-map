using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// <see cref="Render"/> writes a <see cref="Noise2D"/> field to a texture.
    /// <see cref="Read"/> samples an image into step-heights.
    /// </summary>
    public static class HeightmapConverter
    {
        /// <summary>
        /// Fills the target texture with a greyscale render of the noise field, sampled over the
        /// window [offset, offset + span) on each axis. A non-zero steps quantises the values
        /// into steps + 1 levels.
        /// </summary>
        public static void Render(Texture2D target, Noise2D noise, float span, Vector2 offset, int steps = 0)
        {
            int width = target.width;
            int height = target.height;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var value = Mathf.Clamp01(noise.Sample(
                        x / (float)width * span + offset.x,
                        y / (float)height * span + offset.y));

                    if (steps > 0)
                        value = Mathf.Round(value * steps) / steps;

                    pixels[y * width + x] = new Color(value, value, value);
                }
            }

            target.SetPixels(pixels);
            target.Apply();
        }

        /// <summary>
        /// Samples the source's red channel into a width x height grid of step-heights in
        /// [0, bands], bilinear unless nearest is requested. The source must be readable.
        /// </summary>
        public static int[,] Read(Texture2D source, int width, int height, int bands, bool bilinear = true)
        {
            var heights = new int[width, height];
            var n = Mathf.Max(1, bands);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var u = (x + 0.5f) / width;
                    var v = (y + 0.5f) / height;

                    var pixel = bilinear
                        ? source.GetPixelBilinear(u, v)
                        : source.GetPixel(
                            Mathf.Clamp(Mathf.FloorToInt(u * source.width), 0, source.width - 1),
                            Mathf.Clamp(Mathf.FloorToInt(v * source.height), 0, source.height - 1));

                    heights[x, y] = Mathf.RoundToInt(Mathf.Clamp01(pixel.r) * n);
                }
            }

            return heights;
        }
    }
}
