using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A named, reusable heightmap recipe: the noise field plus the height ceiling it quantises
    /// onto and how large its features are.
    /// </summary>
    [CreateAssetMenu(fileName = "New Heightmap", menuName = "HexTerra/Heightmap")]
    public class Heightmap : ScriptableObject
    {
        [Tooltip("Noise field sampled for terrain shape. Build it from primitive and combiner nodes.")]
        [SerializeReference] public Noise2D noise = new FractalNoise();

        [Tooltip("Top step height the [0, 1] noise maps onto. Higher gives finer vertical relief.")]
        [Min(1)] public int maxHeight = 20;

        [Tooltip("Feature size in hex units. Bigger means broader hills and valleys.")]
        public float noiseScale = 20f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // noiseScale is used as a divisor when sampling, so it has to stay clear of zero.
            noiseScale = Mathf.Max(noiseScale, 0.01f);
        }
#endif
    }
}
