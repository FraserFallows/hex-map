using UnityEngine;

namespace HexTerra
{
    [CreateAssetMenu(fileName = "New NoiseData", menuName = "HexTerra/FBM Noise Data")]
    public class FBMNoiseData : ScriptableObject
    {
        /// <summary>
        /// Number of steps between 0.0f and 1.0f.
        /// </summary>
        public int step = 20;
        public int octaves = 5;
        public float lacunarity = 2.0f;
        public float persistence = 0.5f;
    }
}
