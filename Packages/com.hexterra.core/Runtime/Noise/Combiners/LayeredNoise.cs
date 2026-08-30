using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    public enum LayerBlend { Add, Subtract, Multiply, Min, Max, Lerp }

    [Serializable]
    public struct NoiseLayer
    {
        [SerializeReference] public Noise2D source;
        public LayerBlend blend;
        [Range(0f, 1f)] public float weight;

        // Optional. Null applies the layer everywhere; otherwise its [0, 1] value scales the weight.
        [SerializeReference] public Noise2D mask;
    }

    /// <summary>
    /// Folds <see cref="layers"/> left into a single field: each layer blends onto the result so
    /// far, scaled by its weight and its mask. Output is clamped to [0, 1]; layer order matters.
    /// </summary>
    [Serializable]
    public class LayeredNoise : Noise2D
    {
        // Offsets layer i so two identically configured layers don't sample the same pattern.
        private static readonly Vector2 LayerDecorrelation = new(137.2f, 91.7f);

        public List<NoiseLayer> layers = new();

        public override float Sample(float x, float y)
        {
            if (layers == null)
                return 0f;

            float acc = 0f;

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer.source == null || layer.weight <= 0f)
                    continue;

                float ox = x + LayerDecorrelation.x * i;
                float oy = y + LayerDecorrelation.y * i;

                float value = layer.source.Sample(ox, oy);
                float weight = layer.weight * (layer.mask?.Sample(ox, oy) ?? 1f);

                acc = layer.blend switch
                {
                    LayerBlend.Add => acc + value * weight,
                    LayerBlend.Subtract => acc - value * weight,
                    LayerBlend.Multiply => Mathf.Lerp(acc, acc * value, weight),
                    LayerBlend.Min => Mathf.Lerp(acc, Mathf.Min(acc, value), weight),
                    LayerBlend.Max => Mathf.Lerp(acc, Mathf.Max(acc, value), weight),
                    LayerBlend.Lerp => Mathf.Lerp(acc, value, weight),
                    _ => acc
                };
            }

            return Mathf.Clamp01(acc);
        }
    }
}
