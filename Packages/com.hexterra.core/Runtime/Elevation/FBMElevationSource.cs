using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Elevation source driven by fractal Brownian motion (FBM) noise, configured via FBMNoiseData.
    /// </summary>
    public class FBMElevationSource : IElevationSource
    {
        private readonly FBMNoiseData _noiseData;
        private Texture2D _generatedTexture;

        public FBMElevationSource(FBMNoiseData noiseData) => _noiseData = noiseData;

        public int[,] SampleElevation(int gridSize, Vector2 offset)
        {
            var noiseTexture = GenerateFbmNoiseTexture(offset, gridSize);
            return ReadStepHeights(noiseTexture, gridSize);
        }

        /// <summary>
        /// Generates an FBM noise texture — also called by the editor preview window.
        /// </summary>
        public Texture2D GenerateFbmNoiseTexture(Vector2 _translation, int _textureScale)
        {
            _generatedTexture = new Texture2D(_textureScale, _textureScale);

            for (int y = 0; y < _textureScale; y++)
            {
                for (int x = 0; x < _textureScale; x++)
                {
                    var xCoord = (x / (float)_textureScale * _noiseData.lacunarity) + _translation.x;
                    var yCoord = (y / (float)_textureScale * _noiseData.lacunarity) + _translation.y;

                    var fbmValue = Fbm(xCoord, yCoord, _noiseData.octaves, _noiseData.lacunarity, _noiseData.persistence);
                    fbmValue = Mathf.Round(fbmValue * _noiseData.step) / _noiseData.step;
                    var colour = new Color(fbmValue, fbmValue, fbmValue);

                    _generatedTexture.SetPixel(x, y, colour);
                }
            }
            _generatedTexture.Apply();

            return _generatedTexture;
        }

        public float Fbm(float _x, float _y, int _octaves, float _lacunarity, float _persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;

            for (int i = 0; i < _octaves; i++)
            {
                total += Mathf.PerlinNoise(_x * frequency, _y * frequency) * amplitude;
                frequency *= _lacunarity;
                amplitude *= _persistence;
            }

            return total;
        }

        // Method to convert a noise texture's greyscale values into integer step-heights
        private int[,] ReadStepHeights(Texture2D _noiseTexture, int _textureScale)
        {
            if (!_noiseTexture)
            {
                Debug.LogError("FBM noise texture was not generated.");
                return new int[_textureScale, _textureScale];
            }

            var stepHeights = new int[_textureScale, _textureScale];

            for (int x = 0; x < _textureScale; x++)
            {
                for (int y = 0; y < _textureScale; y++)
                {
                    var pixelColour = _noiseTexture.GetPixel(x, y);
                    var greyscale = (pixelColour.r + pixelColour.g + pixelColour.b) / 3.0f;
                    stepHeights[x, y] = Mathf.RoundToInt(greyscale * _noiseData.step);
                }
            }

            return stepHeights;
        }
    }
}
