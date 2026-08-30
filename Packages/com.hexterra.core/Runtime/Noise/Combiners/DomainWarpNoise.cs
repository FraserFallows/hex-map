using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Samples <see cref="source"/> at a position pushed around by <see cref="warp"/>, for
    /// flowing, less grid-aligned terrain.
    /// </summary>
    [Serializable]
    public class DomainWarpNoise : Noise2D
    {
        // Reused warp field sampled far from the origin so the Y displacement is uncorrelated with X.
        private static readonly Vector2 WarpDecorrelation = new(137.2f, 91.7f);

        [SerializeReference] public Noise2D source = new PerlinNoise();
        [SerializeReference] public Noise2D warp = new PerlinNoise();

        // Warp offset in feature-size units. 0 disables warping; past 1 the source is pushed by
        // more than a whole feature and the terrain smears into noise.
        [Range(0f, 1f)] public float strength = 0.5f;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;
            if (warp == null)
                return source.Sample(x, y);

            float offsetX = Signed(warp.Sample(x, y)) * strength;
            float offsetY = Signed(warp.Sample(x + WarpDecorrelation.x, y + WarpDecorrelation.y)) * strength;

            return source.Sample(x + offsetX, y + offsetY);
        }
    }
}
