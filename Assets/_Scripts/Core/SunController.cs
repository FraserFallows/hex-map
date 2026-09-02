using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Aims a directional light from a packed time-of-year vector: x is the day angle,
    /// y the seasonal tilt.
    /// </summary>
    public class SunController : MonoBehaviour
    {
        public void PositionSun(Vector2 timeOfYear)
        {
            var day = timeOfYear.x;
            var season = timeOfYear.y;

            transform.rotation = Quaternion.Euler(0, 0, day) * Quaternion.Euler(season, 0, 90);
        }

        public Vector2 RandomTimeOfYear()
        {
            // Day: 9 angles 16 degrees apart over 128 degrees. Season: a 20, 40 or 60 degree tilt.
            float day = 16 * Random.Range(-4, 5);
            float season = 20 * Random.Range(1, 4);
            return new Vector2(day, season);
        }

        public void RandomisePosition() => PositionSun(RandomTimeOfYear());
    }
}
