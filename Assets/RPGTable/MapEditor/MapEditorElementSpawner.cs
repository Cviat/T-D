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

        private Sprite selectedSprite;

        public void Select(Sprite sprite)
        {
            selectedSprite = sprite;
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
            if (selectedSprite == null || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (PrimaryPressed())
            {
                SpawnAtMouse();
            }
        }

        private void SpawnAtMouse()
        {
            var mouse = MousePosition();
            mouse.z = Mathf.Abs(worldCamera.transform.position.z);
            var world = worldCamera.ScreenToWorldPoint(mouse);
            world.z = -0.1f;

            var element = new GameObject(selectedSprite.name);
            element.transform.position = world;
            element.transform.localScale = Vector3.one;

            var renderer = element.AddComponent<SpriteRenderer>();
            renderer.sprite = selectedSprite;
            renderer.sortingOrder = -15;

            element.AddComponent<BoxCollider2D>();
            element.AddComponent<PlacedMapElement>();
        }

        private static bool PrimaryPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
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
