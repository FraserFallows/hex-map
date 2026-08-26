using System.Collections;
using HexTerra;
using UnityEngine;

namespace _Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private HexMap hexMap;
        public SunController sunController;

        private void Start()
        {
            SetupCamera();
            if (hexMap.AnimateOnPlay)
            {
                StartCoroutine(AnimateOnPlay());
                return;
            }
                
            sunController.PositionSun(sunController.RandomTimeOfYear());
            hexMap.BeginGeneration();
                
        }

        private void SetupCamera()
        {
            var midpoint = hexMap.GetMidpointWorldPosition();
            if (midpoint.HasValue)
                cam.transform.position = new Vector3(0.0f, 50.0f, midpoint.Value.z);
        }

        private IEnumerator AnimateOnPlay()
        {
            while (true)
            {
                sunController.PositionSun(sunController.RandomTimeOfYear());
                hexMap.BeginGeneration();
                yield return new WaitForSeconds(3);
            }
        }
    }
}
