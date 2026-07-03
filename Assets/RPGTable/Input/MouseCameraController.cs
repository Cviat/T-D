using UnityEngine;
using System.Runtime.InteropServices;
using RPGTable.Runtime;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

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
            if (CampaignGameLoader.PlayerViewCameraControlActive)
            {
                dragging = false;
                return;
            }

            if (!MouseOnControlledDisplay())
            {
                dragging = false;
                return;
            }

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
            if (CampaignGameLoader.PlayerViewCameraControlActive)
            {
                return;
            }

            if (!MouseOnControlledDisplay())
            {
                return;
            }

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
            var mouse = MousePositionOnControlledDisplay();
            mouse.z = Mathf.Abs(transform.position.z);
            var world = controlledCamera.ScreenToWorldPoint(mouse);
            world.z = 0f;
            return world;
        }

        private bool MouseOnControlledDisplay()
        {
            return MouseDisplayIndex() == controlledCamera.targetDisplay;
        }

        private Vector3 MousePositionOnControlledDisplay()
        {
            return MousePositionForDisplay(controlledCamera.targetDisplay);
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

        private static int MouseDisplayIndex()
        {
            var mouse = MousePosition();
            var relative = Display.RelativeMouseAt(mouse);

            if (relative != Vector3.zero || Mathf.RoundToInt(relative.z) > 0)
            {
                return Mathf.RoundToInt(relative.z);
            }

            if (TryNativeMouseDisplayIndex(out var nativeDisplayIndex))
            {
                return nativeDisplayIndex;
            }

            if (Display.displays.Length > 1 && mouse.x >= Display.displays[0].systemWidth)
            {
                return 1;
            }

            return 0;
        }

        private static Vector3 MousePositionForDisplay(int displayIndex)
        {
            var mouse = MousePosition();
            var relative = Display.RelativeMouseAt(mouse);

            if (relative != Vector3.zero || Mathf.RoundToInt(relative.z) > 0)
            {
                return new Vector3(relative.x, relative.y, 0f);
            }

            if (TryNativeMousePositionForDisplay(displayIndex, out var nativeMouse))
            {
                return nativeMouse;
            }

            if (displayIndex == 1 && Display.displays.Length > 1 && mouse.x >= Display.displays[0].systemWidth)
            {
                return new Vector3(mouse.x - Display.displays[0].systemWidth, mouse.y, 0f);
            }

            return mouse;
        }

        private static bool TryNativeMouseDisplayIndex(out int displayIndex)
        {
            displayIndex = 0;

            if (!GetCursorPos(out var point))
            {
                return false;
            }

            var firstWidth = PrimaryMonitorWidth();

            if (point.x >= firstWidth)
            {
                displayIndex = 1;
                return true;
            }

            if (point.x >= 0 && point.x < firstWidth)
            {
                displayIndex = 0;
                return true;
            }

            return false;
        }

        private static bool TryNativeMousePositionForDisplay(int displayIndex, out Vector3 position)
        {
            position = Vector3.zero;

            if (!GetCursorPos(out var point))
            {
                return false;
            }

            if (displayIndex == 1)
            {
                var x = point.x - PrimaryMonitorWidth();
                var y = PrimaryMonitorHeight() - point.y;
                position = new Vector3(x, y, 0f);
                return x >= 0f;
            }

            position = new Vector3(point.x, PrimaryMonitorHeight() - point.y, 0f);
            return displayIndex == 0;
        }

        private static int PrimaryMonitorWidth()
        {
            var width = GetSystemMetrics(0);
            return width > 0 ? width : Display.displays[0].systemWidth;
        }

        private static int PrimaryMonitorHeight()
        {
            var height = GetSystemMetrics(1);
            return height > 0 ? height : Display.displays[0].systemHeight;
        }

        private static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return 0f;
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
#else
            return UnityEngine.Input.mouseScrollDelta.y;
#endif
        }
    }
}
