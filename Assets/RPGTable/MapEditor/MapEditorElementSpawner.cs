using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapEditorElementSpawner : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;

        private GameObject draggedElement;
        private PlacedMapElement draggedPlacedElement;

        public void BeginDrag(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (draggedElement != null)
            {
                Destroy(draggedElement);
            }

            draggedElement = CreateElement(sprite);
            draggedPlacedElement = draggedElement.GetComponent<PlacedMapElement>();
            draggedPlacedElement.SetDraggingFromPalette(true);
        }

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (draggedElement == null)
            {
                return;
            }

            MoveDraggedToMouse();

            if (PrimaryReleased() && !IsPointerOverUi())
            {
                draggedPlacedElement.SetDraggingFromPalette(false);
                draggedElement = null;
                draggedPlacedElement = null;
            }
        }

        private void MoveDraggedToMouse()
        {
            draggedElement.transform.position = MouseWorldPosition();
        }

        private GameObject CreateElement(Sprite sprite)
        {
            var element = new GameObject(sprite.name);
            element.transform.position = MouseWorldPosition();
            element.transform.localScale = Vector3.one;

            var renderer = element.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -15;

            element.AddComponent<BoxCollider2D>();
            element.AddComponent<PlacedMapElement>();
            return element;
        }

        private Vector3 MouseWorldPosition()
        {
            var mouse = MousePosition();
            mouse.z = Mathf.Abs(worldCamera.transform.position.z);
            var world = worldCamera.ScreenToWorldPoint(mouse);
            world.z = -0.1f;
            return world;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool PrimaryReleased()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonUp(0);
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
    }
}
