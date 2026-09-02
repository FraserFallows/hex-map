using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HexTerra.Tests
{
    public sealed class SurfacePaletteSetTests
    {
        private readonly List<Object> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _spawned)
                if (o) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private SurfacePaletteSet NewPalette()
        {
            var palette = ScriptableObject.CreateInstance<SurfacePaletteSet>();
            _spawned.Add(palette);
            return palette;
        }

        private Texture2D Track(Texture2D texture)
        {
            _spawned.Add(texture);
            return texture;
        }

        [Test]
        public void BakeLutHasExpectedShapeAndFilters()
        {
            var lut = Track(NewPalette().BakeLut());

            Assert.AreEqual(SurfacePaletteSet.LutWidth, lut.width);
            Assert.AreEqual(3, lut.height);
            Assert.AreEqual(FilterMode.Point, lut.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, lut.wrapMode);
        }

        [Test]
        public void BakeLutRunsEachRowAcrossItsGradient()
        {
            var palette = NewPalette();
            palette.grass = RampFrom(Color.black, Color.white);

            var lut = Track(palette.BakeLut());
            int last = SurfacePaletteSet.LutWidth - 1;
            int mid = last / 2;

            // Row 0 runs black -> white; 0 and 1 are gamma fixed points, so the bounds hold
            // whatever colour space GetPixel reports in.
            Assert.Less(lut.GetPixel(0, 0).grayscale, 0.1f, "dark end");
            Assert.Greater(lut.GetPixel(last, 0).grayscale, 0.9f, "light end");
            Assert.That(lut.GetPixel(mid, 0).grayscale,
                Is.GreaterThan(lut.GetPixel(0, 0).grayscale).And.LessThan(lut.GetPixel(last, 0).grayscale),
                "monotone across the row");
        }

        private static Gradient RampFrom(Color dark, Color light) => new()
        {
            colorKeys = new[] { new GradientColorKey(dark, 0f), new GradientColorKey(light, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };
    }
}
