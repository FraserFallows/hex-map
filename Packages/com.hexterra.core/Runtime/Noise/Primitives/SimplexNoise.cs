using System;
using UnityEngine;

namespace HexTerra
{
    // Standard 2D simplex noise (Gustavson), remapped from [-1, 1] to [0, 1].
    [Serializable]
    public class SimplexNoise : Noise2D
    {
        // Feature size: above 1 spreads the pattern out, below 1 tightens it.
        [Min(0.0001f)] public float scale = 1f;

        private const float F2 = 0.3660254f;   // (sqrt(3) - 1) / 2
        private const float G2 = 0.21132487f;  // (3 - sqrt(3)) / 6

        private static readonly float[,] Gradients =
        {
            { 1f, 1f }, { -1f, 1f }, { 1f, -1f }, { -1f, -1f },
            { 1f, 0f }, { -1f, 0f }, { 0f, 1f }, { 0f, -1f }
        };

        private static readonly int[] Perm = BuildPerm();

        public override float Sample(float x, float y)
        {
            float s = Mathf.Max(scale, 0.0001f);
            x /= s;
            y /= s;

            float skew = (x + y) * F2;
            int i = Mathf.FloorToInt(x + skew);
            int j = Mathf.FloorToInt(y + skew);

            float unskew = (i + j) * G2;
            float x0 = x - (i - unskew);
            float y0 = y - (j - unskew);

            int i1 = x0 > y0 ? 1 : 0;
            int j1 = x0 > y0 ? 0 : 1;

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;

            float n0 = Corner(x0, y0, Perm[ii + Perm[jj]] & 7);
            float n1 = Corner(x1, y1, Perm[ii + i1 + Perm[jj + j1]] & 7);
            float n2 = Corner(x2, y2, Perm[ii + 1 + Perm[jj + 1]] & 7);

            return Mathf.Clamp01(0.5f + 0.5f * 70f * (n0 + n1 + n2));
        }

        private static float Corner(float x, float y, int gradient)
        {
            float t = 0.5f - x * x - y * y;
            if (t < 0f)
                return 0f;

            t *= t;
            return t * t * (Gradients[gradient, 0] * x + Gradients[gradient, 1] * y);
        }

        private static int[] BuildPerm()
        {
            var values = new int[256];
            for (int k = 0; k < 256; k++)
                values[k] = k;

            var rng = new System.Random(1337);
            for (int k = 255; k > 0; k--)
            {
                int swap = rng.Next(k + 1);
                (values[k], values[swap]) = (values[swap], values[k]);
            }

            var perm = new int[512];
            for (int k = 0; k < 512; k++)
                perm[k] = values[k & 255];
            return perm;
        }
    }
}
