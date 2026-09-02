using System;
using UnityEngine;

namespace HexTerra
{
    public enum FractalMode { Fbm, Ridged, Billow }

    /// <summary>
    /// Stacks octaves of <see cref="source"/> and normalises to [0, 1]. Ridged and Billow
    /// reshape each octave before it is summed.
    /// </summary>
    [Serializable]
    public class FractalNoise : Noise2D
    {
        [Tooltip("Noise stacked octave on octave.")]
        [SerializeReference] public Noise2D source = new PerlinNoise();

        [Tooltip("How each octave is shaped before summing: Fbm as-is, Ridged inverting peaks into sharp ridges, Billow folding it into rounded lumps.")]
        public FractalMode mode = FractalMode.Fbm;

        [Tooltip("How many octaves to stack. Past about 8 the added detail is finer than a hex.")]
        [Range(1, 8)] public int octaves = 5;

        [Tooltip("Frequency multiplier per octave: how much finer each one is than the last.")]
        [Range(1f, 4f)] public float lacunarity = 2f;

        [Tooltip("Amplitude falloff per octave: how much less each one adds than the last.")]
        [Range(0f, 1f)] public float persistence = 0.5f;

        [Tooltip("Rotates each octave this many degrees past the last so axis-aligned artefacts cancel instead of streaking. 0 keeps them aligned.")]
        [Range(0f, 90f)] public float octaveRotation = 30f;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float totalAmplitude = 0f;

            float radians = octaveRotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            float sx = x;
            float sy = y;

            for (int i = 0; i < octaves; i++)
            {
                var octave = source.Sample(sx * frequency, sy * frequency);
                octave = mode switch
                {
                    FractalMode.Ridged => 1f - Mathf.Abs(Signed(octave)),
                    FractalMode.Billow => Mathf.Abs(Signed(octave)),
                    _ => octave
                };

                total += octave * amplitude;
                totalAmplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
                (sx, sy) = (sx * cos - sy * sin, sx * sin + sy * cos);
            }

            return totalAmplitude > 0f ? total / totalAmplitude : 0f;
        }
    }
}
