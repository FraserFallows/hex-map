using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// A named, reusable noise setup — the field itself plus how it quantises into
    /// height bands and how large its features are. A HexMap points at one, and
    /// several maps can share it.
    /// </summary>
    [CreateAssetMenu(fileName = "New NoisePreset", menuName = "HexTerra/Noise Preset")]
    public class NoisePreset : ScriptableObject
    {
        [SerializeReference] public Noise2D noise = new FractalNoise();

        // Discrete height bands the noise is quantised into.
        public int bands = 20;

        // Feature size in hex units. Bigger means broader hills and valleys.
        public float noiseScale = 20f;
    }
}
