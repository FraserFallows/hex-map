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
        [SerializeReference] public Noise2D source = new FractalNoise();

        public float inputMin = 0.35f;
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
