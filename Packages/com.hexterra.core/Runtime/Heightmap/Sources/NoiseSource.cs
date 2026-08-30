using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Quantises a <see cref="Noise2D"/> field into integer step-heights. Deterministic for a seed.
    /// </summary>
    public class NoiseSource : IHeightmapSource
    {
        private readonly Noise2D _noise;
        private readonly float _scale;
        private readonly int _maxHeight;
        private readonly int _seed;

        public NoiseSource(Noise2D noise, int maxHeight, float noiseScale, int seed)
        {
            _noise = noise;
            _maxHeight = Mathf.Max(1, maxHeight);
            _scale = Mathf.Max(noiseScale, 0.0001f);
            _seed = seed;
        }

        public int[,] SampleHeightmap(int width, int height)
        {
            var heights = new int[width, height];

            if (_noise == null)
            {
                Debug.LogError("NoiseSource: no noise assigned. Returning a flat heightmap.");
                return heights;
            }

            // Local RNG, so the offset is reproducible for the seed without touching global state.
            var rng = new System.Random(_seed);
            var offsetX = (float)(rng.NextDouble() * 1000.0);
            var offsetY = (float)(rng.NextDouble() * 1000.0);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var value = _noise.Sample(x / _scale + offsetX, y / _scale + offsetY);
                    heights[x, y] = Mathf.RoundToInt(Mathf.Clamp01(value) * _maxHeight);
                }
            }

            return heights;
        }
    }
}
