using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Reads step-heights from the red channel of an image, stretched to fill the grid.
    /// </summary>
    public class TextureSource : IHeightmapSource
    {
        private readonly Texture2D _image;
        private readonly int _bands;
        private readonly bool _bilinear;

        public TextureSource(Texture2D image, int bands, bool bilinear)
        {
            _image = image;
            _bands = Mathf.Max(1, bands);
            _bilinear = bilinear;
        }

        public int[,] SampleHeightmap(int width, int height)
        {
            if (!_image) return new int[width, height];

            if (!_image.isReadable)
            {
                Debug.LogError($"TextureSource: '{_image.name}' is not readable. Enable Read/Write in its import settings.");
                return new int[width, height];
            }

            return HeightmapConverter.Read(_image, width, height, _bands, _bilinear);
        }
    }
}
