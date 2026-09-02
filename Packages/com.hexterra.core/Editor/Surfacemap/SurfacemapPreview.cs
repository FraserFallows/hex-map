using UnityEngine;

namespace HexTerra.Editor
{
    /// <summary>
    /// The surface recipe applied straight to the heightmap noise rather than to a built map.
    /// <see cref="Classify"/> tags every texel Grass / Dirt / Rock once. <see cref="Paint"/> colours
    /// a classification, flat or through the palette gradient with tint drift and a height shade.
    /// </summary>
    internal static class SurfacemapPreview
    {
        private const int Size = 256;
        private const float WorldSpan = 64f;

        internal struct RecipeContext
        {
            public Heightmap heightmap;
            public SurfacePaletteSet palette;
            public int seed;

            // Set when a classify input the editor cannot observe itself (seed, heightmap,
            // palette) changed last pass.
            public bool dirty;
            public bool valid;
        }

        // The owner data a standalone Surfacemap inspector lacks. HexMapEditor sets this around the
        // embedded panel's draw and clears it after; left default (valid false) the editor shows
        // the noise previews alone.
        internal static RecipeContext Context;

        /// <summary>
        /// One <see cref="Classify"/> result: the per-texel kind and step grids, each texel's world
        /// position for tint sampling, and the tallest step for the height shade. Reused across
        /// passes, so pass the instance back to refill it.
        /// </summary>
        internal sealed class Classification
        {
            public readonly int[] steps = new int[Size * Size];
            public readonly Vector2[] worldXZ = new Vector2[Size * Size];
            public SurfaceKind[] kinds;
            public int maxStep;
        }

        private static readonly Color Grass = new(0.36f, 0.50f, 0.24f);
        private static readonly Color Dirt = new(0.50f, 0.38f, 0.24f);
        private static readonly Color Rock = new(0.50f, 0.50f, 0.53f);

        // Pure function of Size and HexMath.Directions, so it is built once and shared.
        private static int[] _neighbours;

        // Scratch pixel buffer; both Paint calls in a pass fill and upload it in turn.
        private static Color[] _pixels;

        /// <summary>
        /// Refills <paramref name="into"/> (or a fresh instance) from the heightmap noise and the
        /// surfacemap's classifier settings, and returns it. Null when the heightmap or the
        /// surfacemap is missing.
        /// </summary>
        public static Classification Classify(Classification into, Heightmap heightmap,
            Surfacemap surfacemap, SurfacePaletteSet palette, int seed)
        {
            if (!heightmap || heightmap.noise == null || !surfacemap)
                return null;

            var result = into ?? new Classification();

            int maxHeight = Mathf.Max(1, heightmap.maxHeight);
            float heightSpan = WorldSpan / Mathf.Max(heightmap.noiseScale, 0.0001f);

            var steps = result.steps;
            var worldXZ = result.worldXZ;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float v = Mathf.Clamp01(heightmap.noise.Sample(
                        x / (float)Size * heightSpan, y / (float)Size * heightSpan));
                    steps[i] = Mathf.RoundToInt(v * maxHeight);
                    worldXZ[i] = new Vector2(x, y) / Size * WorldSpan;
                }
            }

            result.kinds = new SurfaceClassifier(
                    surfacemap.ToClassifierSettings(seed, palette ? palette.rockWallSteps : 0))
                .Classify(steps, Neighbours(), worldXZ);

            int maxStep = 1;
            foreach (int step in steps)
                if (step > maxStep) maxStep = step;
            result.maxStep = maxStep;

            return result;
        }

        /// <summary>
        /// Colours <paramref name="reuse"/> (or a fresh texture) from <paramref name="classification"/>
        /// and returns it: flat kind colours, or the palette gradient with tint drift and a height
        /// shade when <paramref name="shaded"/> and a palette are given. Null when the classification
        /// is empty.
        /// </summary>
        public static Texture2D Paint(Classification classification, Texture2D reuse,
            Surfacemap surfacemap, SurfacePaletteSet palette, int seed, bool shaded)
        {
            if (classification?.kinds == null)
                return null;

            var texture = reuse ? reuse : NewTexture();

            var kinds = classification.kinds;
            var steps = classification.steps;
            var worldXZ = classification.worldXZ;
            float maxStep = classification.maxStep;

            bool tinted = shaded && palette;
            var tint = surfacemap.tint.noise;
            float tintScale = Mathf.Max(surfacemap.tint.noiseScale, 0.0001f);
            bool perCell = surfacemap.tint.perCell;
            var rng = new System.Random(seed);
            float offsetX = (float)(rng.NextDouble() * 1000.0);
            float offsetY = (float)(rng.NextDouble() * 1000.0);

            var pixels = _pixels ??= new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                if (!tinted)
                {
                    pixels[i] = KindColour(kinds[i]);
                    continue;
                }

                var pos = perCell ? SnapToHex(worldXZ[i]) : worldXZ[i];
                float t = tint != null
                    ? Mathf.Clamp01(tint.Sample(pos.x / tintScale + offsetX, pos.y / tintScale + offsetY))
                    : 0.5f;
                var colour = KindGradient(palette, kinds[i]).Evaluate(t);
                float shade = Mathf.Lerp(0.55f, 1f, steps[i] / maxStep);
                pixels[i] = new Color(colour.r * shade, colour.g * shade, colour.b * shade);
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false);
            return texture;
        }

        private static Texture2D NewTexture() => new(Size, Size, TextureFormat.RGBA32, mipChain: false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // The texel grid read as an axial lattice: HexMath.Directions gives the six neighbours, so
        // the classifier's clean-up passes behave as they do on a real map. Off-grid is -1.
        private static int[] Neighbours()
        {
            if (_neighbours != null)
                return _neighbours;

            _neighbours = new int[Size * Size * 6];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    for (int d = 0; d < 6; d++)
                    {
                        int nx = x + HexMath.Directions[d].x;
                        int ny = y + HexMath.Directions[d].y;
                        _neighbours[i * 6 + d] =
                            nx >= 0 && nx < Size && ny >= 0 && ny < Size ? ny * Size + nx : -1;
                    }
                }
            }
            return _neighbours;
        }

        // The centre of the hex covering a sample point, for the flat per-cell tint.
        private static Vector2 SnapToHex(Vector2 worldXZ)
        {
            var centre = HexMath.ToWorld(HexMath.FromWorld(new Vector3(worldXZ.x, 0f, worldXZ.y)));
            return new Vector2(centre.x, centre.z);
        }

        private static Color KindColour(SurfaceKind kind) => kind switch
        {
            SurfaceKind.Dirt => Dirt,
            SurfaceKind.Rock => Rock,
            _ => Grass
        };

        private static Gradient KindGradient(SurfacePaletteSet palette, SurfaceKind kind) => kind switch
        {
            SurfaceKind.Dirt => palette.dirt,
            SurfaceKind.Rock => palette.rock,
            _ => palette.grass
        };
    }
}
