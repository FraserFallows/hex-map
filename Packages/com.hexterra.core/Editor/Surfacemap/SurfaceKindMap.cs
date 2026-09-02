using System.Collections.Generic;
using UnityEngine;

namespace HexTerra.Editor
{
    /// <summary>
    /// Renders the classified grid to an image, one texel per cell by axial coordinate: flat
    /// SurfaceKind colours, or the palette gradient shaded by cell height when a palette is
    /// given, matching the built surface's tint.
    /// </summary>
    internal static class SurfaceKindMap
    {
        private static readonly Color Grass = new(0.36f, 0.50f, 0.24f);
        private static readonly Color Dirt = new(0.50f, 0.38f, 0.24f);
        private static readonly Color Rock = new(0.50f, 0.50f, 0.53f);
        private static readonly Color Outside = new(0.14f, 0.14f, 0.15f);

        /// <summary>
        /// Repaints and returns <paramref name="reuse"/> when its size still matches the grid's
        /// axial bounding box, otherwise a fresh point-filtered texture. Null when there are no
        /// cells yet. A non-null <paramref name="palette"/> switches cells from flat kinds to the
        /// tint pipeline, sampling <paramref name="tintNoise"/> with the build's seed offset.
        /// </summary>
        public static Texture2D Render(IReadOnlyList<HexCell> cells, Texture2D reuse,
            SurfacePaletteSet palette = null, Noise2D tintNoise = null, float tintScale = 1f, int seed = 0)
        {
            if (cells == null || cells.Count == 0)
                return reuse;

            int minQ = int.MaxValue, minR = int.MaxValue, maxQ = int.MinValue, maxR = int.MinValue;
            int maxStep = 1;
            foreach (var cell in cells)
            {
                if (cell.q < minQ) minQ = cell.q;
                if (cell.q > maxQ) maxQ = cell.q;
                if (cell.r < minR) minR = cell.r;
                if (cell.r > maxR) maxR = cell.r;
                if (cell.stepHeight > maxStep) maxStep = cell.stepHeight;
            }

            int width = maxQ - minQ + 1;
            int height = maxR - minR + 1;

            var texture = reuse && reuse.width == width && reuse.height == height
                ? reuse
                : new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Outside;

            float scale = Mathf.Max(tintScale, 0.0001f);
            var rng = new System.Random(seed);
            float offsetX = (float)(rng.NextDouble() * 1000.0);
            float offsetY = (float)(rng.NextDouble() * 1000.0);

            foreach (var cell in cells)
                pixels[(cell.r - minR) * width + (cell.q - minQ)] = palette
                    ? Tinted(cell, palette, tintNoise, scale, offsetX, offsetY, maxStep)
                    : Colour(cell.surfaceKind);

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false);
            return texture;
        }

        private static Color Colour(SurfaceKind kind) => kind switch
        {
            SurfaceKind.Dirt => Dirt,
            SurfaceKind.Rock => Rock,
            _ => Grass
        };

        private static Color Tinted(HexCell cell, SurfacePaletteSet palette, Noise2D tintNoise,
            float scale, float offsetX, float offsetY, int maxStep)
        {
            var gradient = cell.surfaceKind switch
            {
                SurfaceKind.Dirt => palette.dirt,
                SurfaceKind.Rock => palette.rock,
                _ => palette.grass
            };

            var position = cell.transform.position;
            float t = tintNoise != null
                ? Mathf.Clamp01(tintNoise.Sample(position.x / scale + offsetX, position.z / scale + offsetY))
                : 0.5f;

            var colour = gradient.Evaluate(t);
            float shade = Mathf.Lerp(0.55f, 1f, cell.stepHeight / (float)maxStep);
            return new Color(colour.r * shade, colour.g * shade, colour.b * shade);
        }
    }
}
