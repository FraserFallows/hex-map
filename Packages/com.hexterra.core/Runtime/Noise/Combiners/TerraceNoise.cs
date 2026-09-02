using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Snaps <see cref="source"/> to <see cref="levels"/> flat heights spread evenly across
    /// [0, 1] inclusive, with a ramp between each. Output stays [0, 1].
    /// </summary>
    [Serializable]
    public class TerraceNoise : Noise2D
    {
        [Tooltip("Noise snapped into flat bands.")]
        [SerializeReference] public Noise2D source = new FractalNoise();

        [Tooltip("Number of flat heights, evenly spaced over 0 to 1. 2 is just a floor and a ceiling.")]
        [Min(2)] public int levels = 5;

        [Tooltip("Sharpness of the step between bands. 0 is a rounded ramp, 1 a vertical cliff.")]
        [Range(0f, 1f)] public float flatness = 0.6f;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            int risers = Mathf.Max(levels - 1, 1);
            float scaled = Mathf.Clamp01(source.Sample(x, y)) * risers;
            float band = Mathf.Min(Mathf.Floor(scaled), risers - 1);
            float t = scaled - band;

            // flatness pulls the ramp into the middle of the band, leaving a flat on either side.
            float ramp = Mathf.Max(1f - flatness, 1e-4f);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.5f) / ramp + 0.5f));

            return Mathf.Clamp01((band + rise) / risers);
        }
    }
}
