using UnityEngine;

namespace _Scripts.Core
{
    public class CameraController : MonoBehaviour
    {
        public float movementSpeed = 5f;
        public float rotationSpeed = 100f;

        private void Update()
        {
            var horizontalInput = Input.GetAxis("Horizontal");
            var verticalInput = Input.GetAxis("Vertical");

            var translation = new Vector3(horizontalInput, 0f, verticalInput) * (movementSpeed * Time.deltaTime);
            transform.Translate(translation, Space.World);

            var mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);

            var mouseY = Input.GetAxis("Mouse Y");
            transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
