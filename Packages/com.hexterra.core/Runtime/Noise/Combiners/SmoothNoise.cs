using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Box-blurs <see cref="source"/> by averaging a grid of samples around each point. Softens
    /// the field's shape, not its value distribution (a curve or remap does that). Output stays [0, 1].
    /// </summary>
    [Serializable]
    public class SmoothNoise : Noise2D
    {
        [SerializeReference] public Noise2D source = new FractalNoise();

        // Blur reach in sample units. 0 passes the source through unchanged.
        [Min(0f)] public float radius = 1f;

        // Samples each way from centre per axis: (2 * taps + 1) squared per output. More is smoother and slower.
        [Range(1, 4)] public int taps = 2;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;
            if (radius <= 0f || taps < 1)
                return source.Sample(x, y);

            int side = 2 * taps + 1;
            float step = radius / taps;
            float sum = 0f;

            for (int i = -taps; i <= taps; i++)
            for (int j = -taps; j <= taps; j++)
                sum += source.Sample(x + i * step, y + j * step);

            return sum / (side * side);
        }
    }
}
