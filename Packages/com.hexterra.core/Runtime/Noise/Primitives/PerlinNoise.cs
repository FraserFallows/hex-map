using System;
using UnityEngine;

namespace HexTerra
{
    [Serializable]
    public class PerlinNoise : Noise2D
    {
        // Feature size: above 1 spreads the pattern out, below 1 tightens it.
        [Min(0.0001f)] public float scale = 1f;

        public override float Sample(float x, float y)
        {
            float s = Mathf.Max(scale, 0.0001f);
            // Mathf.PerlinNoise can drift slightly outside [0, 1]; the contract says it must not.
            return Mathf.Clamp01(Mathf.PerlinNoise(x / s, y / s));
        }
    }
}
