using UnityEngine;

namespace HexTerra
{
    public class HexCell : MonoBehaviour
    {
        public GameObject[] neighbours = new GameObject[6];

        public int q;
        public int r;

        /// <summary>
        /// Vertical displacement in 0.25 metre increments.
        /// </summary>
        public int stepHeight;
    }
}
