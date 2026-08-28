using System;

namespace HexTerra
{
    /// <summary>
    /// A 2D noise field. <see cref="Sample"/> returns [0, 1].
    /// </summary>
    [Serializable]
    public abstract class Noise2D
    {
        public abstract float Sample(float x, float y);

        /// <summary>
        /// Remaps a [0, 1] sample to a signed [-1, 1] value.
        /// </summary>
        protected static float Signed(float value01) => value01 * 2f - 1f;
    }
}