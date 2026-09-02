using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Tags each cell Grass, Dirt or Rock from a weighted score over slope, height, convexity
    /// and a noise field, then runs majority-filter passes to clean up the clusters. A deep
    /// enough drop to a neighbour forces Rock.
    /// </summary>
    public class SurfaceClassifier
    {
        private readonly Settings _settings;
        private readonly float _offsetX;
        private readonly float _offsetY;

        private const float Epsilon = 1e-4f;

        public SurfaceClassifier(Settings settings)
        {
            _settings = settings;

            // Local RNG so the patch pattern is reproducible for the seed without touching global state.
            var rng = new System.Random(settings.seed);
            _offsetX = (float)(rng.NextDouble() * 1000.0);
            _offsetY = (float)(rng.NextDouble() * 1000.0);
        }

        /// <summary>
        /// Classifies the grid's cells and writes each result to <see cref="HexCell.surfaceKind"/>.
        /// </summary>
        public void Apply(HexGrid grid)
        {
            var cells = grid.Cells;
            int count = cells.Count;

            var index = new Dictionary<HexCell, int>(count);
            for (int i = 0; i < count; i++)
                index[cells[i]] = i;

            var steps = new int[count];
            var worldXZ = new Vector2[count];
            var neighbours = new int[count * 6];

            for (int i = 0; i < count; i++)
            {
                var cell = cells[i];
                steps[i] = cell.stepHeight;
                var position = cell.transform.position;
                worldXZ[i] = new Vector2(position.x, position.z);

                for (int d = 0; d < 6; d++)
                {
                    var neighbour = cell.neighbours[d];
                    neighbours[i * 6 + d] =
                        neighbour && index.TryGetValue(neighbour, out int ni) ? ni : -1;
                }
            }

            var kinds = Classify(steps, neighbours, worldXZ);
            for (int i = 0; i < count; i++)
                cells[i].surfaceKind = kinds[i];
        }

        /// <summary>
        /// Classifies each cell from its step height, its 6-stride neighbour indices into the
        /// same arrays (-1 for a missing neighbour) and its world XZ. Returns one kind per
        /// cell in input order.
        /// </summary>
        public SurfaceKind[] Classify(int[] steps, int[] neighbours, Vector2[] worldXZ)
        {
            int count = steps.Length;
            var kinds = new SurfaceKind[count];
            var cliffDrop = new int[count];

            float slopeRef = Mathf.Max(_settings.slopeReference, Epsilon);
            float altitudeRef = Mathf.Max(_settings.altitudeReference, Epsilon);
            float convexityRef = Mathf.Max(_settings.convexityReference, Epsilon);
            float noiseScale = Mathf.Max(_settings.noiseScale, Epsilon);

            // A missing noise field drops its weight from the blend rather than diluting the
            // other terms with a constant zero.
            float noiseWeight = _settings.noise != null ? _settings.noiseWeight : 0f;
            float weightSum = _settings.slopeWeight + _settings.altitudeWeight
                              + _settings.convexityWeight + noiseWeight;

            for (int i = 0; i < count; i++)
            {
                int step = steps[i];
                int maxDelta = 0;
                int drop = 0;
                int neighbourSum = 0;
                int neighbourCount = 0;

                for (int d = 0; d < 6; d++)
                {
                    int nj = neighbours[i * 6 + d];
                    if (nj < 0) continue;

                    int delta = step - steps[nj];
                    int abs = delta < 0 ? -delta : delta;
                    if (abs > maxDelta) maxDelta = abs;
                    if (delta > drop) drop = delta;
                    neighbourSum += steps[nj];
                    neighbourCount++;
                }

                cliffDrop[i] = drop;

                // slope: steepest drop or rise to a neighbour. altitude: raw height.
                float slope01 = Mathf.Clamp01(maxDelta / slopeRef);
                float altitude01 = Mathf.Clamp01(step / altitudeRef);

                // convexity: height against the neighbour mean, 0.5 = level, above = a
                // bump (ridge, knoll), below = a hollow. Bumps erode rockier than hollows.
                float neighbourMean = neighbourCount > 0 ? (float)neighbourSum / neighbourCount : step;
                float convexity01 = Mathf.Clamp01(0.5f + (step - neighbourMean) / (2f * convexityRef));

                float noise01 = 0f;
                if (_settings.noise != null)
                    noise01 = Mathf.Clamp01(_settings.noise.Sample(
                        worldXZ[i].x / noiseScale + _offsetX,
                        worldXZ[i].y / noiseScale + _offsetY));

                float rockiness = weightSum > Epsilon
                    ? (_settings.slopeWeight * slope01
                       + _settings.altitudeWeight * altitude01
                       + _settings.convexityWeight * convexity01
                       + noiseWeight * noise01) / weightSum
                    : 0f;

                kinds[i] = rockiness >= _settings.rockThreshold ? SurfaceKind.Rock
                         : rockiness >= _settings.dirtThreshold ? SurfaceKind.Dirt
                         : SurfaceKind.Grass;
            }

            CleanUp(kinds, neighbours);

            // A cell that drops this many steps or more to a neighbour is a cliff, whatever
            // the score said. Applied after clean-up so it is never smoothed away.
            if (_settings.rockPromoteSteps > 0)
                for (int i = 0; i < count; i++)
                    if (cliffDrop[i] >= _settings.rockPromoteSteps)
                        kinds[i] = SurfaceKind.Rock;

            return kinds;
        }

        // Majority-filter passes over the discrete grid: a cell whose neighbours hold a
        // strict majority of another kind flips to it. Double-buffered so every read in a
        // pass sees the previous pass. Border cells are left alone so edges don't smear.
        private void CleanUp(SurfaceKind[] kinds, int[] neighbours)
        {
            if (_settings.cleanupPasses <= 0) return;

            int count = kinds.Length;
            var next = new SurfaceKind[count];
            var tally = new int[3];

            for (int pass = 0; pass < _settings.cleanupPasses; pass++)
            {
                for (int i = 0; i < count; i++)
                {
                    tally[0] = tally[1] = tally[2] = 0;
                    int neighbourCount = 0;

                    for (int d = 0; d < 6; d++)
                    {
                        int nj = neighbours[i * 6 + d];
                        if (nj < 0) continue;
                        tally[(int)kinds[nj]]++;
                        neighbourCount++;
                    }

                    next[i] = kinds[i];
                    if (neighbourCount < 3) continue;

                    int topKind = 0;
                    for (int k = 1; k < 3; k++)
                        if (tally[k] > tally[topKind]) topKind = k;

                    if (tally[topKind] * 2 > neighbourCount && topKind != (int)kinds[i])
                        next[i] = (SurfaceKind)topKind;
                }

                Array.Copy(next, kinds, count);
            }
        }

        public struct Settings
        {
            public Noise2D noise;
            public float noiseScale;
            public float slopeWeight;
            public float altitudeWeight;
            public float convexityWeight;
            public float noiseWeight;
            public float slopeReference;
            public float altitudeReference;
            public float convexityReference;
            public float rockThreshold;
            public float dirtThreshold;
            public int rockPromoteSteps;
            public int cleanupPasses;
            public int seed;
        }
    }
}
