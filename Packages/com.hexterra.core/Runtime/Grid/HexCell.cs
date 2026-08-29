using UnityEngine;

namespace HexTerra
{
    public class HexCell : MonoBehaviour
    {
        public GameObject[] neighbours = new GameObject[6];

        public int q;
        public int r;

        // Height in discrete steps. WorldHeight converts it to metres.
        public int stepHeight;

        public float WorldHeight => stepHeight * StepMetres;

        // World metres per step.
        public const float StepMetres = 0.25f;
    }
}
