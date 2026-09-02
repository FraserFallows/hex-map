using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Stretches <see cref="source"/>'s [<see cref="inputMin"/>, <see cref="inputMax"/>] window to
    /// fill [0, 1], so an averaged field that never reaches the extremes still spans the full range.
    /// </summary>
    [Serializable]
    public class RemapNoise : Noise2D
    {
        [Tooltip("Noise whose value window is stretched to fill 0 to 1.")]
        [SerializeReference] public Noise2D source = new FractalNoise();

        [Tooltip("Source value that maps to 0. Anything lower clamps to 0.")]
        public float inputMin = 0.35f;
        [Tooltip("Source value that maps to 1. Anything higher clamps to 1.")]
        public float inputMax = 0.65f;

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            // InverseLerp clamps to [0, 1] and returns 0 when the window is degenerate.
            return Mathf.InverseLerp(inputMin, inputMax, source.Sample(x, y));
        }
    }
}
