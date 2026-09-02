using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HexTerra.Tests
{
    public sealed class SurfaceClassifierTests
    {
        private static SurfaceClassifier.Settings BaseSettings() => new()
        {
            noise = null,
            noiseScale = 12f,
            slopeWeight = 0.4f,
            altitudeWeight = 0.2f,
            convexityWeight = 0.15f,
            noiseWeight = 0.25f,
            slopeReference = 3f,
            altitudeReference = 8f,
            convexityReference = 3f,
            rockThreshold = 0.6f,
            dirtThreshold = 0.35f,
            rockPromoteSteps = 0,
            cleanupPasses = 0,
            seed = 12345
        };

        private static SurfaceKind[] Run(SurfaceClassifier.Settings settings, Grid grid) =>
            new SurfaceClassifier(settings).Classify(grid.steps, grid.neighbours, grid.worldXZ);

        [Test]
        public void FlatGridWithNoNoiseIsAllGrass()
        {
            var grid = new Grid(8, 8, (q, r) => 0);

            var kinds = Run(BaseSettings(), grid);

            Assert.IsTrue(kinds.All(k => k == SurfaceKind.Grass));
        }

        [Test]
        public void SteepHighPeakClassifiesAsRock()
        {
            var grid = new Grid(5, 5, (q, r) => q == 2 && r == 2 ? 12 : 0);

            var kinds = Run(BaseSettings(), grid);

            Assert.AreEqual(SurfaceKind.Rock, kinds[grid.IndexOf(2, 2)], "the peak");
            Assert.AreEqual(SurfaceKind.Grass, kinds[grid.IndexOf(0, 0)], "a flat corner");
        }

        [Test]
        public void AltitudeTermRaisesRockiness()
        {
            var settings = BaseSettings();
            settings.slopeWeight = 0f;
            settings.convexityWeight = 0f;
            settings.noiseWeight = 0f;
            settings.altitudeWeight = 1f;

            var low = Run(settings, new Grid(6, 6, (q, r) => 2));
            var high = Run(settings, new Grid(6, 6, (q, r) => 12));

            Assert.IsTrue(low.All(k => k == SurfaceKind.Grass));
            Assert.IsTrue(high.All(k => k == SurfaceKind.Rock));
        }

        [Test]
        public void ACliffDropPromotesToRock()
        {
            // One raised cell dropping 6 steps to its neighbours, with every score weight
            // zeroed so the blend says Grass and only the cliff rule can make it Rock.
            var grid = new Grid(5, 5, (q, r) => q == 2 && r == 2 ? 6 : 0);
            int centre = grid.IndexOf(2, 2);

            var settings = BaseSettings();
            settings.slopeWeight = settings.altitudeWeight = settings.convexityWeight = settings.noiseWeight = 0f;

            settings.rockPromoteSteps = 0;
            Assert.AreEqual(SurfaceKind.Grass, Run(settings, grid)[centre], "disabled");

            settings.rockPromoteSteps = 5;
            Assert.AreEqual(SurfaceKind.Rock, Run(settings, grid)[centre], "6-step drop clears a 5-step bar");
            Assert.AreEqual(SurfaceKind.Grass, Run(settings, grid)[grid.IndexOf(0, 0)], "a flat corner is untouched");

            settings.rockPromoteSteps = 7;
            Assert.AreEqual(SurfaceKind.Grass, Run(settings, grid)[centre], "6-step drop under a 7-step bar");
        }

        [Test]
        public void RaisingRockThresholdNeverAddsRock()
        {
            var settings = BaseSettings();
            settings.noise = new WorleyNoise { scale = 1f, jitter = 1f };
            settings.noiseWeight = 0.5f;
            settings.slopeWeight = 0.3f;
            settings.altitudeWeight = 0.1f;
            settings.convexityWeight = 0.1f;

            var grid = new Grid(12, 12, (q, r) => (q * 2 + r) % 7);

            int previous = int.MaxValue;
            foreach (float threshold in new[] { 0.3f, 0.45f, 0.6f, 0.75f, 0.9f })
            {
                var stepped = settings;
                stepped.rockThreshold = threshold;
                stepped.dirtThreshold = Mathf.Min(0.2f, threshold);

                int rock = Run(stepped, grid).Count(k => k == SurfaceKind.Rock);
                Assert.LessOrEqual(rock, previous, $"threshold {threshold}");
                previous = rock;
            }
        }

        [Test]
        public void RepeatedAndParallelClassifyRunsAgree()
        {
            var settings = BaseSettings();
            settings.noise = new WorleyNoise { scale = 1f, jitter = 1f };
            settings.noiseWeight = 0.4f;
            settings.seed = 999;

            var grid = new Grid(10, 10, (q, r) => (q + r) % 4);
            var classifier = new SurfaceClassifier(settings);

            var first = classifier.Classify(grid.steps, grid.neighbours, grid.worldXZ);
            var second = classifier.Classify(grid.steps, grid.neighbours, grid.worldXZ);
            var fresh = Run(settings, grid);

            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreEqual(first, fresh);
        }

        [Test]
        public void DifferentSeedsGiveDifferentPatterns()
        {
            var settings = BaseSettings();
            settings.noise = new WorleyNoise { scale = 1f, jitter = 1f };
            settings.slopeWeight = 0f;
            settings.altitudeWeight = 0f;
            settings.convexityWeight = 0f;
            settings.noiseWeight = 1f;

            var grid = new Grid(10, 10, (q, r) => 0);

            settings.seed = 1;
            var a = Run(settings, grid);
            settings.seed = 2;
            var b = Run(settings, grid);

            CollectionAssert.AreNotEqual(a, b);
        }

        [Test]
        public void NoiseWeightIsInertWhenNoiseIsNull()
        {
            var grid = new Grid(9, 9, (q, r) => (q + 2 * r) % 5);

            var withWeight = BaseSettings();
            var withoutWeight = BaseSettings();
            withoutWeight.noiseWeight = 0f;

            CollectionAssert.AreEqual(Run(withoutWeight, grid), Run(withWeight, grid));
        }

        [Test]
        public void BorderCellsClassifyAndSurviveCleanup()
        {
            var line = new Grid(3, 1, (q, r) => q * 4);

            var settings = BaseSettings();
            settings.cleanupPasses = 5;

            var kinds = Run(settings, line);

            Assert.AreEqual(3, kinds.Length);
            Assert.IsTrue(kinds.All(k => k is SurfaceKind.Grass or SurfaceKind.Dirt or SurfaceKind.Rock));
        }

        // A rectangular axial block, q in [0, width), r in [0, height), with a step-height
        // function and the six-neighbour adjacency the classifier consumes.
        private sealed class Grid
        {
            public readonly int[] steps;
            public readonly int[] neighbours;
            public readonly Vector2[] worldXZ;
            public readonly int width;

            public Grid(int width, int height, Func<int, int, int> step)
            {
                this.width = width;
                int count = width * height;
                steps = new int[count];
                worldXZ = new Vector2[count];
                neighbours = new int[count * 6];

                for (int r = 0; r < height; r++)
                for (int q = 0; q < width; q++)
                {
                    int i = IndexOf(q, r);
                    steps[i] = step(q, r);
                    var world = HexMath.ToWorld(q, r);
                    worldXZ[i] = new Vector2(world.x, world.z);

                    for (int d = 0; d < 6; d++)
                    {
                        var dir = HexMath.Directions[d];
                        int nq = q + dir.x;
                        int nr = r + dir.y;
                        neighbours[i * 6 + d] =
                            nq >= 0 && nq < width && nr >= 0 && nr < height ? IndexOf(nq, nr) : -1;
                    }
                }
            }

            public int IndexOf(int q, int r) => r * width + q;
        }
    }
}
