using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Inverts <see cref="source"/>'s value, mapping 0 to 1 and 1 to 0.
    /// </summary>
    [Serializable]
    public class OneMinusNoise : Noise2D
    {
        [Tooltip("Noise whose value is inverted.")]
        [SerializeReference] public Noise2D source = new FractalNoise();

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            return 1f - source.Sample(x, y);
        }
    }
}