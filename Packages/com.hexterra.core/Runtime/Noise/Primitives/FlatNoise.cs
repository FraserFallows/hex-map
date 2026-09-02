using System;
using UnityEngine;

namespace HexTerra
{
    // A constant field: Sample returns the same value everywhere.
    [Serializable]
    public class FlatNoise : Noise2D
    {
        [Tooltip("The value the field holds everywhere. Useful as a floor or bias term inside LayeredNoise.")]
        [Range(0f, 1f)] public float value = 0.5f;

        public override float Sample(float x, float y) => value;
    }
}
