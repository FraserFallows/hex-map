using UnityEngine;

namespace HexTerra
{
    public class HexCell : MonoBehaviour
    {
        /// <summary>
        /// Hex top [0] and walls [1].
        /// </summary>
        public GameObject[] hex = new GameObject[2];

        public GameObject[] firstNeighbours = new GameObject[6];
        public GameObject[] secondNeighbours = new GameObject[12];

        public int xGridPos;
        public int zGridPos;

        /// <summary>
        /// Vertical displacement in 0.25 metre increments.
        /// </summary>
        public int stepHeight;

        public bool xParity;
    }
}
