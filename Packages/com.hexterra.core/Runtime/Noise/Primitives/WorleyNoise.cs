using System;
using UnityEngine;

namespace HexTerra
{
    // Cellular (Worley) noise: distance to the nearest feature point, one per unit cell.
    [Serializable]
    public class WorleyNoise : Noise2D
    {
        // 0 pins feature points to cell centres (a regular grid), 1 scatters them fully.
        public float jitter = 1f;

        public override float Sample(float x, float y)
        {
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
