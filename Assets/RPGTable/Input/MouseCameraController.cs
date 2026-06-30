using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.Input
{
    [RequireComponent(typeof(Camera))]
    public sealed class MouseCameraController : MonoBehaviour
    {
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 30f;

        private Camera controlledCamera;
        private Vector3 lastMouseWorld;
        private bool dragging;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleDrag();
            HandleZoom();
        }

        private void HandleDrag()
        {
            if (DragStarted())
            {
                dragging = true;
                lastMouseWorld = MouseWorldPosition();
            }

            if (DragEnded())
            {
                dragging = false;
            }

            if (!dragging)
            {
                return;
            }

            var currentMouseWorld = MouseWorldPosition();
            var delta = lastMouseWorld - currentMouseWorld;
            transform.position += delta * dragSpeed;
            lastMouseWorld = MouseWorldPosition();
        }

        private void HandleZoom()
        {
            var scroll = ScrollDelta();

            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            controlledCamera.orthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize - scroll * zoomSpeed,
                minZoom,
                maxZoom);
        }

        private Vector3 MouseWorldPosition()
        {
            var mouse = MousePosition();
            mouse.z = Mathf.Abs(transform.position.z);
            var world = controlledCamera.ScreenToWorldPoint(mouse);
            world.z = 0f;
            return world;
        }

        private static bool DragStarted()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && (Mouse.current.middleButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
#else
            return UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetMouseButtonDown(2);
#endif
        }

        private static bool DragEnded()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null || Mouse.current.middleButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonUp(1) || UnityEngine.Input.GetMouseButtonUp(2);
#endif
        }

        private static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector3.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? 0f : Mouse.current.scroll.ReadValue().y / 120f;
#else
            return UnityEngine.Input.mouseScrollDelta.y;
#endif
        }
    }
}
