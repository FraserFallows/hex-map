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
        [SerializeReference] public Noise2D source = new PerlinNoise();

        public FractalMode mode = FractalMode.Fbm;

        // Fewer than 1 octave leaves the field empty, and past ~8 the extra octaves are sub-hex detail.
        [Range(1, 8)] public int octaves = 5;

        // Frequency multiplier per octave. Below 1 the octaves would coarsen instead of refining.
        [Range(1f, 4f)] public float lacunarity = 2f;

        // Amplitude falloff per octave. Above 1 the fine octaves would swamp the base shape.
        [Range(0f, 1f)] public float persistence = 0.5f;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float totalAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                var octave = source.Sample(x * frequency, y * frequency);
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
            }

            return totalAmplitude > 0f ? total / totalAmplitude : 0f;
        }
    }
}
