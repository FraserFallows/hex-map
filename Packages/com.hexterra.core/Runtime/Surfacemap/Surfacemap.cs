using System;
using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A named, reusable surface recipe: the classifier noise, weights, references, thresholds
    /// and clean-up passes that tag each cell Grass / Dirt / Rock, plus the vertex-tint noise
    /// that drifts colour within a kind.
    /// </summary>
    [CreateAssetMenu(fileName = "New Surfacemap", menuName = "HexTerra/Surfacemap")]
    public class Surfacemap : ScriptableObject
    { 
        [Tooltip("Weight of steepness in the score. 0: slope ignored. 1: slope dominant.")]
        [Range(0f, 1f)] public float slopeWeight = 0.4f;
        [Tooltip("Weight of raw height in the score. 0: height ignored. 1: height dominant.")]
        [Range(0f, 1f)] public float altitudeWeight = 0.2f;
        [Tooltip("Weight of convexity in the score. Bumps push toward Rock, hollows toward Grass, level ground stays neutral. 0: ignored. 1: dominant.")]
        [Range(0f, 1f)] public float convexityWeight = 0.15f;
        [Tooltip("Weight of the noise field in the score. 0: noise ignored. 1: noise dominant.")]
        [Range(0f, 1f)] public float noiseWeight = 0.25f;

        [Tooltip("Step change to a neighbour that counts as fully steep. Small: any slope maxes it. Large: only sheer drops.")]
        [Min(1)] public int slopeReference = 3;
        [Tooltip("Step height that counts as fully high. Small: most of the map reads high. Large: only peaks.")]
        [Min(1)] public int altitudeReference = 8;
        [Tooltip("Height difference from the neighbour mean that counts as fully convex or hollow. Small: slight relief maxes the term. Large: only steep relief does, so most cells read neutral.")]
        [Min(1)] public int convexityReference = 3;

        [Tooltip("Score at or above this is Rock. 0: all Rock. 1: no Rock.")]
        [Range(0f, 1f)] public float rockThreshold = 0.6f;
        [Tooltip("Score at or above this is Dirt, below it Grass. 0: no Grass. Rock Threshold or above: no Dirt.")]
        [Range(0f, 1f)] public float dirtThreshold = 0.35f;
        [Tooltip("Majority-filter smoothing passes. 0: every speckle kept. High: small pockets dissolve into neighbours.")]
        [Min(0)] public int cleanupPasses = 2;

        [Tooltip("Noise field blended into the score. None: score is purely terrain-driven.")]
        [SerializeReference] public Noise2D noise = new WorleyNoise();
        [Tooltip("Feature size of the classifier noise, in hex units. Small: fine specks. Large: broad regions.")]
        public float noiseScale = 12f;

        public TintSettings tint = new();

        /// <summary>
        /// Low-frequency colour drift within a SurfaceKind. <see cref="perCell"/> samples the noise
        /// once per cell (flat colour, hard edges) instead of per vertex (smooth drift).
        /// </summary>
        [Serializable]
        public class TintSettings
        {
            [Tooltip("Sample the tint once per cell (flat colour, hard edges) instead of per vertex (smooth drift).")]
            public bool perCell = true;
            [Tooltip("Low-frequency noise that drifts colour within a SurfaceKind band.")]
            [SerializeReference] public Noise2D noise = new FractalNoise();
            [Tooltip("Feature size of the tint noise, in hex units.")]
            public float noiseScale = 8f;
        }

        /// <summary>
        /// These fields as a <see cref="SurfaceClassifier.Settings"/>, folding in the seed and the
        /// drop-to-Rock step count (the caller reads that from the palette).
        /// </summary>
        public SurfaceClassifier.Settings ToClassifierSettings(int seed, int rockPromoteSteps) => new()
        {
            noise = noise,
            noiseScale = noiseScale,
            slopeWeight = slopeWeight,
            altitudeWeight = altitudeWeight,
            convexityWeight = convexityWeight,
            noiseWeight = noiseWeight,
            slopeReference = slopeReference,
            altitudeReference = altitudeReference,
            convexityReference = convexityReference,
            rockThreshold = rockThreshold,
            dirtThreshold = dirtThreshold,
            rockPromoteSteps = rockPromoteSteps,
            cleanupPasses = cleanupPasses,
            seed = seed
        };

        public HexMeshBuilder.TintConfig ToTintConfig() => new()
        {
            noise = tint.noise,
            noiseScale = tint.noiseScale,
            perCell = tint.perCell
        };
    }
}
