using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A named, reusable noise setup: the noise field plus the height ceiling it quantises
    /// onto and how large its features are.
    /// </summary>
    [CreateAssetMenu(fileName = "New NoisePreset", menuName = "HexTerra/Noise Preset")]
    public class NoisePreset : ScriptableObject
    {
        [SerializeReference] public Noise2D noise = new FractalNoise();

        // Top integer step-height the [0, 1] field maps onto. More is finer relief, so only the lower bound matters.
        [Min(1)] public int maxHeight = 20;

        // Feature size in hex units. Bigger means broader hills and valleys.
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
