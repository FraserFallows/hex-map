using System;
using UnityEngine;

namespace HexTerra
{
    [Serializable]
    public class PerlinNoise : Noise2D
    {
        public override float Sample(float x, float y) => Mathf.PerlinNoise(x, y);
    }
}
