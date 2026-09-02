using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Per-SurfaceKind tint gradients, baked into a lookup texture the surface shader samples
    /// by a vertex's tint value.
    /// </summary>
    [CreateAssetMenu(fileName = "New SurfacePaletteSet", menuName = "HexTerra/Surface Palette Set")]
    public class SurfacePaletteSet : ScriptableObject
    {
        [Tooltip("Colour ramp for Grass faces. A face's vertex tint value picks a point along it, 0 at the left, 1 at the right.")]
        public Gradient grass = Ramp(new Color(0.24f, 0.34f, 0.16f), new Color(0.44f, 0.58f, 0.30f));
        [Tooltip("Colour ramp for Dirt faces, read by the vertex tint value like Grass.")]
        public Gradient dirt = Ramp(new Color(0.32f, 0.24f, 0.16f), new Color(0.52f, 0.41f, 0.28f));
        [Tooltip("Colour ramp for Rock faces, read by the vertex tint value like Grass.")]
        public Gradient rock = Ramp(new Color(0.30f, 0.30f, 0.32f), new Color(0.56f, 0.56f, 0.59f));

        [Tooltip("A wall face at least this many steps tall tints as Dirt, or as its cell's own kind if that is higher.")]
        [Min(1)] public int dirtWallSteps = 3;
        [Tooltip("A wall face at least this tall tints as Rock. Separately, any cell that drops this many steps to a neighbour is forced to Rock.")]
        [Min(1)] public int rockWallSteps = 9;

        public const int LutWidth = 64;
        private const int Rows = 3;

        /// <summary>
        /// Bakes the three gradients into a LutWidth-by-3 point-sampled, clamped texture, one
        /// row per kind in SurfaceKind order. Repaints and returns <paramref name="reuse"/>
        /// when it is already the right size, else allocates a fresh one.
        /// </summary>
        public Texture2D BakeLut(Texture2D reuse = null)
        {
            var lut = reuse && reuse.width == LutWidth && reuse.height == Rows
                ? reuse
                : new Texture2D(LutWidth, Rows, TextureFormat.RGBA32, mipChain: false, linear: false);
            lut.name = $"{name} LUT";
            lut.wrapMode = TextureWrapMode.Clamp;
            lut.filterMode = FilterMode.Point;

            var rows = new[] { grass, dirt, rock };
            for (int row = 0; row < Rows; row++)
                for (int x = 0; x < LutWidth; x++)
                    lut.SetPixel(x, row, rows[row].Evaluate(x / (LutWidth - 1f)));

            lut.Apply(updateMipmaps: false);
            return lut;
        }

        private static Gradient Ramp(Color dark, Color light) => new()
        {
            colorKeys = new[] { new GradientColorKey(dark, 0f), new GradientColorKey(light, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };

#if UNITY_EDITOR
        private void OnValidate() => rockWallSteps = Mathf.Max(rockWallSteps, dirtWallSteps);
#endif
    }
}
