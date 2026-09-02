using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Remaps <see cref="source"/> through <see cref="curve"/>. Both curve axes run 0 to 1.
    /// </summary>
    [Serializable]
    public class CurveNoise : Noise2D
    {
        [Tooltip("Noise remapped through the curve.")]
        [SerializeReference] public Noise2D source = new PerlinNoise();
        [Tooltip("Maps the source value (X) to the output (Y). Both axes run 0 to 1.")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public override float Sample(float x, float y)
        {
            if (source == null)
                return 0f;

            var value = source.Sample(x, y);
            if (curve == null || curve.length == 0)
                return value;

            return Mathf.Clamp01(curve.Evaluate(value));
        }
    }
}
