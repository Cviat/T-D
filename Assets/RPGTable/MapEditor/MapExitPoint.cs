using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapExitPoint : MonoBehaviour
    {
        private const float ExitPointZ = -0.2f;

        private enum ResizeHandle
        {
            TopLeft,
            TopRight,
            BottomRight,
            BottomLeft
        }

        private static Sprite markerSprite;
        private static Sprite handleSprite;
        private static MapExitPoint selectedExitPoint;

        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Vector2 size = new Vector2(1.2f, 1.2f);
        [SerializeField] private float minSize = 0.4f;
        [SerializeField] private Color selectionColor = new Color(0.1f, 0.65f, 1f, 1f);
        [SerializeField] private float borderWidth = 0.035f;
        [SerializeField] private float handleSize = 0.22f;

        private bool moving;
        private bool resizing;
        private Camera mainCamera;
        private GameObject selectionRoot;
        private LineRenderer borderRenderer;
        private ResizeHandle activeHandle;
        private Vector3 resizeFixedCornerWorld;

        public string Id => id;
        public string DisplayName => displayName;
        public Vector2 Size => size;

        public void Initialize(string exitId, string exitName, Vector2 triggerSize)
        {
            id = exitId;
            displayName = exitName;
            size = triggerSize;
            name = string.IsNullOrWhiteSpace(exitName) ? "Exit Point" : exitName;
            EnsureVisuals();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureVisuals();
        }

        private void Update()
        {
            if (selectedExitPoint == this && EscapePressed())
            {
                Deselect();
            }
        }

        private void OnMouseDown()
        {
            if (IsPointerOverUi())
            {
                return;
            }

            Select();
            moving = true;
        }

        private void OnMouseDrag()
        {
            if (!moving || resizing)
            {
                return;
            }

            transform.position = WithExitPointZ(MouseWorld());
        }

        private void OnMouseUp()
        {
            moving = false;
        }

        private void OnDestroy()
        {
            if (selectedExitPoint == this)
            {
                selectedExitPoint = null;
            }
        }

        public void BeginResize(int handleIndex)
        {
            Select();
            moving = false;
            resizing = true;
            activeHandle = (ResizeHandle)handleIndex;
            resizeFixedCornerWorld = transform.TransformPoint(OppositeCornerLocal(activeHandle));
        }

        public void ResizeToMouse()
        {
            if (!resizing)
            {
                return;
            }

            var movingCornerWorld = MouseWorld();
            var center = (resizeFixedCornerWorld + movingCornerWorld) * 0.5f;
            var nextSize = new Vector2(
                Mathf.Max(minSize, Mathf.Abs(movingCornerWorld.x - resizeFixedCornerWorld.x)),
                Mathf.Max(minSize, Mathf.Abs(movingCornerWorld.y - resizeFixedCornerWorld.y)));

            transform.position = new Vector3(center.x, center.y, ExitPointZ);
            size = nextSize;
            ApplySize();
        }

        public void EndResize()
        {
            resizing = false;
        }

        private void OnMouseOver()
        {
            if (IsPointerOverUi() || !SecondaryPressedThisFrame())
            {
                return;
            }

            MapEditorDeletePopup.Show(MousePosition(), "Удалить", () => Destroy(gameObject));
        }

        private void EnsureVisuals()
        {
            var renderer = GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = GetMarkerSprite();
            renderer.color = new Color(1f, 0.25f, 0.9f, 0.75f);
            renderer.sortingOrder = 200;

            var collider = GetComponent<BoxCollider2D>();

            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = true;
            collider.size = Vector2.one;
            EnsureSelectionVisuals();
            ApplySize();
            SetSelectionVisualsVisible(selectedExitPoint == this);
        }

        private void ApplySize()
        {
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void Select()
        {
            if (selectedExitPoint != null && selectedExitPoint != this)
            {
                selectedExitPoint.Deselect();
            }

            selectedExitPoint = this;
            SetSelectionVisualsVisible(true);
        }

        private void Deselect()
        {
            if (selectedExitPoint == this)
            {
                selectedExitPoint = null;
            }

            moving = false;
            resizing = false;
            SetSelectionVisualsVisible(false);
        }

        private void EnsureSelectionVisuals()
        {
            if (selectionRoot != null)
            {
                return;
            }

            selectionRoot = new GameObject("Selection");
            selectionRoot.transform.SetParent(transform, false);
            selectionRoot.transform.localPosition = Vector3.zero;
            selectionRoot.transform.localRotation = Quaternion.identity;
            selectionRoot.transform.localScale = Vector3.one;

            var borderObject = new GameObject("Border");
            borderObject.transform.SetParent(selectionRoot.transform, false);
            borderRenderer = borderObject.AddComponent<LineRenderer>();
            borderRenderer.useWorldSpace = false;
            borderRenderer.loop = true;
            borderRenderer.positionCount = 4;
            borderRenderer.startWidth = borderWidth;
            borderRenderer.endWidth = borderWidth;
            borderRenderer.startColor = selectionColor;
            borderRenderer.endColor = selectionColor;
            borderRenderer.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            borderRenderer.sortingOrder = 220;
            borderRenderer.SetPosition(0, new Vector3(-0.5f, 0.5f, 0f));
            borderRenderer.SetPosition(1, new Vector3(0.5f, 0.5f, 0f));
            borderRenderer.SetPosition(2, new Vector3(0.5f, -0.5f, 0f));
            borderRenderer.SetPosition(3, new Vector3(-0.5f, -0.5f, 0f));

            CreateHandle(ResizeHandle.TopLeft);
            CreateHandle(ResizeHandle.TopRight);
            CreateHandle(ResizeHandle.BottomRight);
            CreateHandle(ResizeHandle.BottomLeft);
        }

        private void CreateHandle(ResizeHandle handle)
        {
            var handleObject = new GameObject(handle.ToString());
            handleObject.transform.SetParent(selectionRoot.transform, false);
            handleObject.transform.localPosition = CornerLocal(handle);

            var renderer = handleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetHandleSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 221;

            var collider = handleObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var resizeHandle = handleObject.AddComponent<MapExitPointResizeHandle>();
            resizeHandle.Initialize(this, (int)handle);
        }

        private void LateUpdate()
        {
            UpdateHandleScales();
        }

        private void UpdateHandleScales()
        {
            if (selectionRoot == null)
            {
                return;
            }

            var inverseScale = new Vector3(
                size.x <= Mathf.Epsilon ? handleSize : handleSize / size.x,
                size.y <= Mathf.Epsilon ? handleSize : handleSize / size.y,
                1f);

            for (var i = 0; i < selectionRoot.transform.childCount; i++)
            {
                var child = selectionRoot.transform.GetChild(i);

                if (child.GetComponent<MapExitPointResizeHandle>() != null)
                {
                    child.localScale = inverseScale;
                }
            }
        }

        private static Vector3 CornerLocal(ResizeHandle handle)
        {
            if (handle == ResizeHandle.TopLeft)
            {
                return new Vector3(-0.5f, 0.5f, 0f);
            }

            if (handle == ResizeHandle.TopRight)
            {
                return new Vector3(0.5f, 0.5f, 0f);
            }

            if (handle == ResizeHandle.BottomRight)
            {
                return new Vector3(0.5f, -0.5f, 0f);
            }

            return new Vector3(-0.5f, -0.5f, 0f);
        }

        private static Vector3 OppositeCornerLocal(ResizeHandle handle)
        {
            if (handle == ResizeHandle.TopLeft)
            {
                return CornerLocal(ResizeHandle.BottomRight);
            }

            if (handle == ResizeHandle.TopRight)
            {
                return CornerLocal(ResizeHandle.BottomLeft);
            }

            if (handle == ResizeHandle.BottomRight)
            {
                return CornerLocal(ResizeHandle.TopLeft);
            }

            return CornerLocal(ResizeHandle.TopRight);
        }

        private void SetSelectionVisualsVisible(bool visible)
        {
            if (selectionRoot != null)
            {
                selectionRoot.SetActive(visible);
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
            world.z = ExitPointZ;
            return world;
        }

        private static Vector3 WithExitPointZ(Vector3 position)
        {
            position.z = ExitPointZ;
            return position;
        }

        private static Sprite GetMarkerSprite()
        {
            if (markerSprite != null)
            {
                return markerSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            markerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return markerSprite;
        }

        private static Sprite GetHandleSprite()
        {
            if (handleSprite != null)
            {
                return handleSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            handleSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return handleSprite;
        }

        private static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector3.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static bool SecondaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(1);
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

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
