using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.MapEditor
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlacedMapElement : MonoBehaviour
    {
        [SerializeField] private float scaleSpeed = 0.1f;
        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private float maxScale = 20f;

        private Camera mainCamera;
        private bool selected;
        private bool draggingFromPalette;
        private Vector3 dragOffset;

        public void SetDraggingFromPalette(bool value)
        {
            draggingFromPalette = value;
            selected = !value;
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void OnMouseDown()
        {
            if (draggingFromPalette)
            {
                return;
            }

            selected = true;
            dragOffset = transform.position - MouseWorld();
        }

        private void OnMouseDrag()
        {
            if (draggingFromPalette)
            {
                return;
            }

            transform.position = MouseWorld() + dragOffset;
        }

        private void Update()
        {
            if (!selected)
            {
                return;
            }

            var scroll = ScrollDelta();

            if (Mathf.Abs(scroll) > 0.01f)
            {
                var next = Mathf.Clamp(transform.localScale.x + scroll * scaleSpeed, minScale, maxScale);
                transform.localScale = Vector3.one * next;
            }

            if (EscapePressed())
            {
                selected = false;
            }
        }

        private Vector3 MouseWorld()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            var mouse = MousePosition();
            mouse.z = Mathf.Abs(mainCamera.transform.position.z);
            var world = mainCamera.ScreenToWorldPoint(mouse);
            world.z = transform.position.z;
            return world;
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

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
