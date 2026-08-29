using HexTerra;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Core
{
    /// <summary>
    /// RTS-style rig: pan the ground plane, scroll to zoom, Q/E and R/F (or right-drag) to
    /// orbit yaw and pitch. Bindings are editable in the inspector.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputAction pan;
        [SerializeField] private InputAction orbit;
        [SerializeField] private InputAction orbitDrag;
        [SerializeField] private InputAction pointerDelta;
        [SerializeField] private InputAction zoom;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 45f;
        [SerializeField] private bool edgePan;
        [SerializeField] private float edgePanBorder = 10f;

        [Header("Zoom")]
        [SerializeField] private float zoomStep = 8f;
        [SerializeField] private float minHeight = 20f;
        [SerializeField] private float maxHeight = 160f;

        [Header("Orbit")]
        [SerializeField] private float rotateSpeed = 120f;
        [SerializeField] private float dragRotateSpeed = 0.15f;
        [SerializeField] private float minPitch = 15f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool invertPitch;

        [Header("Framing")]
        [SerializeField] private float framePitch = 55f;
        [SerializeField] private float frameDistance = 90f;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(transform.eulerAngles.x, minPitch, maxPitch);
            if (pan == null || pan.bindings.Count == 0)
                ConfigureDefaultBindings();
        }

        private void OnEnable()
        {
            pan.Enable();
            orbit.Enable();
            orbitDrag.Enable();
            pointerDelta.Enable();
            zoom.Enable();
        }

        private void OnDisable()
        {
            pan.Disable();
            orbit.Disable();
            orbitDrag.Disable();
            pointerDelta.Disable();
            zoom.Disable();
        }

        private void Update()
        {
            Pan();
            Orbit();
            Zoom();
        }

#if UNITY_EDITOR
        private void Reset() => ConfigureDefaultBindings();
#endif

        /// <summary>
        /// Recentres and re-angles the rig on the map's midpoint.
        /// </summary>
        public void FrameMap(HexMap map)
        {
            var centre = map.GetMidpointWorldPosition();
            if (!centre.HasValue)
                return;

            _pitch = Mathf.Clamp(framePitch, minPitch, maxPitch);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.SetPositionAndRotation(centre.Value - rotation * Vector3.forward * frameDistance, rotation);
        }

        private void Pan()
        {
            var input = pan.ReadValue<Vector2>();

            // Screen-edge push: cursor position, not a rebindable control.
            if (edgePan && Application.isFocused && Mouse.current != null)
            {
                var cursor = Mouse.current.position.ReadValue();
                if (cursor.x <= edgePanBorder) input.x = -1f;
                else if (cursor.x >= Screen.width - edgePanBorder) input.x = 1f;
                if (cursor.y <= edgePanBorder) input.y = -1f;
                else if (cursor.y >= Screen.height - edgePanBorder) input.y = 1f;
            }

            if (input == Vector2.zero)
                return;

            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            transform.position += (right * input.x + forward * input.y) * (panSpeed * Time.deltaTime);
        }

        private void Orbit()
        {
            var rotation = orbit.ReadValue<Vector2>() * (rotateSpeed * Time.deltaTime);
            if (orbitDrag.IsPressed())
                rotation += pointerDelta.ReadValue<Vector2>() * dragRotateSpeed;

            if (rotation == Vector2.zero)
                return;

            _yaw += rotation.x;
            _pitch = Mathf.Clamp(_pitch + (invertPitch ? -rotation.y : rotation.y), minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void Zoom()
        {
            var scroll = zoom.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f))
                return;

            var position = transform.position + transform.forward * (Mathf.Sign(scroll) * zoomStep);
            position.y = Mathf.Clamp(position.y, minHeight, maxHeight);
            transform.position = position;
        }

        private void ConfigureDefaultBindings()
        {
            pan = new InputAction("Pan", expectedControlType: "Vector2");
            pan.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            pan.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            // x = yaw (Q/E), y = pitch (F/R)
            orbit = new InputAction("Orbit", expectedControlType: "Vector2");
            orbit.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/r").With("Down", "<Keyboard>/f")
                .With("Left", "<Keyboard>/q").With("Right", "<Keyboard>/e");

            orbitDrag = new InputAction("OrbitDrag", InputActionType.Button, "<Mouse>/rightButton");
            pointerDelta = new InputAction("PointerDelta", InputActionType.Value, "<Mouse>/delta");
            zoom = new InputAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");
        }
    }
}
