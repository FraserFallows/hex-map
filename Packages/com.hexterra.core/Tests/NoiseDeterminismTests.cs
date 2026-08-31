using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HexTerra.Tests
{
    public sealed class NoiseDeterminismTests
    {
        private static readonly (float x, float y)[] Probes =
        {
            (0f, 0f), (0.5f, 0.5f), (1.25f, -3.75f), (-8f, 2f), (13.3f, 41.9f), (-100.5f, 0.25f)
        };

        private static Noise2D[] Primitives() => new Noise2D[]
        {
            new PerlinNoise { scale = 1f },
            new SimplexNoise { scale = 1f },
            new WorleyNoise { scale = 1f, jitter = 1f }
        };

        [Test]
        public void SameSeedGivesIdenticalHeightmaps()
        {
            var a = new NoiseSource(new PerlinNoise { scale = 1f }, 32, 1.5f, 4242);
            var b = new NoiseSource(new PerlinNoise { scale = 1f }, 32, 1.5f, 4242);

            Assert.AreEqual(a.SampleHeightmap(24, 24), b.SampleHeightmap(24, 24));
        }

        [Test]
        public void RepeatedCallsGiveTheSameHeightmap()
        {
            var source = new NoiseSource(new SimplexNoise { scale = 1f }, 32, 1f, 7);

            Assert.AreEqual(source.SampleHeightmap(20, 20), source.SampleHeightmap(20, 20));
        }

        [Test]
        public void DifferentSeedsGiveDifferentHeightmaps()
        {
            var noise = new PerlinNoise { scale = 1f };
            var a = new NoiseSource(noise, 32, 1f, 1).SampleHeightmap(16, 16);
            var b = new NoiseSource(noise, 32, 1f, 2).SampleHeightmap(16, 16);

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void HeightsStayWithinTheBand()
        {
            var heights = new NoiseSource(new PerlinNoise { scale = 1f }, 20, 1f, 99).SampleHeightmap(32, 32);

            foreach (int h in heights)
                Assert.That(h, Is.InRange(0, 20));
        }

        [Test]
        public void MaxHeightIsClampedToAtLeastOne()
        {
            // maxHeight 0 unclamped would flatten every cell to 0; clamping to 1 lets peaks reach 1.
            var heights = new NoiseSource(new PerlinNoise { scale = 1f }, 0, 1f, 5).SampleHeightmap(48, 48);

            Assert.IsTrue(heights.Cast<int>().All(h => h is 0 or 1), "band should be [0, 1]");
            Assert.IsTrue(heights.Cast<int>().Any(h => h == 1), "peaks should reach 1");
        }

        [Test]
        public void NullNoiseLogsAndReturnsFlat()
        {
            LogAssert.Expect(LogType.Error, "NoiseSource: no noise assigned. Returning a flat heightmap.");
            var heights = new NoiseSource(null, 10, 1f, 0).SampleHeightmap(4, 4);

            Assert.IsTrue(heights.Cast<int>().All(h => h == 0));
        }

        [Test]
        public void EachPrimitiveIsDeterministic()
        {
            foreach (var noise in Primitives())
                foreach (var (x, y) in Probes)
                    Assert.AreEqual(noise.Sample(x, y), noise.Sample(x, y), $"{noise.GetType().Name} ({x}, {y})");
        }

        [Test]
        public void TwoInstancesOfAPrimitiveAgree()
        {
            var first = Primitives();
            var second = Primitives();

            for (int i = 0; i < first.Length; i++)
                foreach (var (x, y) in Probes)
                    Assert.AreEqual(first[i].Sample(x, y), second[i].Sample(x, y),
                        $"{first[i].GetType().Name} ({x}, {y})");
        }

        [Test]
        public void EachPrimitiveStaysInUnitRange()
        {
            foreach (var noise in Primitives())
                for (float x = -20f; x <= 20f; x += 1.3f)
                    for (float y = -20f; y <= 20f; y += 1.7f)
                        Assert.That(noise.Sample(x, y), Is.InRange(0f, 1f), $"{noise.GetType().Name} ({x}, {y})");
        }

        [Test]
        public void ScaleKeepsPrimitivesDeterministicAndInRange()
        {
            var scaled = new Noise2D[]
            {
                new PerlinNoise { scale = 0.3f },
                new SimplexNoise { scale = 5f },
                new WorleyNoise { scale = 2f, jitter = 0.5f }
            };

            foreach (var noise in scaled)
                foreach (var (x, y) in Probes)
                {
                    float value = noise.Sample(x, y);
                    Assert.AreEqual(value, noise.Sample(x, y), $"{noise.GetType().Name}");
                    Assert.That(value, Is.InRange(0f, 1f), $"{noise.GetType().Name} ({x}, {y})");
                }
        }

        [Test]
        public void WorleyWithoutJitterPutsAFeaturePointAtEveryCellCentre()
        {
            var worley = new WorleyNoise { scale = 1f, jitter = 0f };

            for (int cx = -2; cx <= 2; cx++)
                for (int cy = -2; cy <= 2; cy++)
                    Assert.That(worley.Sample(cx + 0.5f, cy + 0.5f), Is.EqualTo(0f).Within(1e-4f), $"cell ({cx}, {cy})");
        }
    }
}
