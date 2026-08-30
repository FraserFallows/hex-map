using System;
using UnityEngine;

namespace HexTerra
{
    // Cellular (Worley) noise: distance to the nearest feature point, one per unit cell.
    [Serializable]
    public class WorleyNoise : Noise2D
    {
        // Feature size: above 1 spreads the pattern out, below 1 tightens it.
        [Min(0.0001f)] public float scale = 1f;

        // 0 pins feature points to cell centres (a regular grid), 1 scatters them fully. Past 1 a
        // point can leave its cell and the 3x3 search below would miss the true nearest.
        [Range(0f, 1f)] public float jitter = 1f;

        public override float Sample(float x, float y)
        {
            float s = Mathf.Max(scale, 0.0001f);
            x /= s;
            y /= s;

            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float nearestSq = float.MaxValue;

            for (int gx = -1; gx <= 1; gx++)
            for (int gy = -1; gy <= 1; gy++)
            {
                int cx = xi + gx;
                int cy = yi + gy;

                float px = cx + 0.5f + (Hash(cx, cy, 0) - 0.5f) * jitter;
                float py = cy + 0.5f + (Hash(cx, cy, 1) - 0.5f) * jitter;

                float dx = px - x;
                float dy = py - y;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq < nearestSq)
                    nearestSq = distanceSq;
            }

            return Mathf.Clamp01(Mathf.Sqrt(nearestSq));
        }

        private static float Hash(int x, int y, int channel)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(channel * 1013904223);
                h = (h ^ (h >> 15)) * 2246822519u;
                h = (h ^ (h >> 13)) * 3266489917u;
                h ^= h >> 16;
                return h / (float)uint.MaxValue;
            }
        }
    }
}
