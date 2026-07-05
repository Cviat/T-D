using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.MapEditor
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlacedMapElement : MonoBehaviour
    {
        private enum ResizeHandle
        {
            TopLeft,
            TopRight,
            BottomRight,
            BottomLeft
        }

        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private float maxScale = 20f;
        [SerializeField] private Color selectionColor = new Color(0.1f, 0.65f, 1f, 1f);
        [SerializeField] private float borderWidth = 0.035f;
        [SerializeField] private float handleSize = 0.35f;
        [SerializeField] private string sourceImagePath;

        private static PlacedMapElement selectedElement;
        private static Sprite handleSprite;

        private Camera mainCamera;
        private SpriteRenderer spriteRenderer;
        private bool selected;
        private bool draggingFromPalette;
        private bool moving;
        private bool resizing;
        private ResizeHandle activeHandle;
        private Vector3 dragOffset;
        private Vector3 resizeFixedCornerWorld;

        private GameObject selectionRoot;
        private LineRenderer borderRenderer;

        public string SourceImagePath => sourceImagePath;

        public void InitializeSource(string path)
        {
            sourceImagePath = path;
        }

        public void SetDraggingFromPalette(bool value)
        {
            draggingFromPalette = value;

            if (value)
            {
                Deselect();
                return;
            }

            Select();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureSelectionVisuals();
            SetSelectionVisualsVisible(false);
        }

        private void OnMouseDown()
        {
            if (draggingFromPalette)
            {
                return;
            }

            Select();
            moving = true;
            dragOffset = transform.position - MouseWorld();
        }

        private void OnMouseDrag()
        {
            if (draggingFromPalette || resizing || !moving)
            {
                return;
            }

            transform.position = MouseWorld() + dragOffset;
        }

        private void OnMouseUp()
        {
            moving = false;
        }

        private void OnMouseOver()
        {
            if (draggingFromPalette || IsPointerOverUi() || !SecondaryPressedThisFrame())
            {
                return;
            }

            Select();
            MapEditorDeletePopup.Show(MousePosition(), "Удалить", () => Destroy(gameObject));
        }

        private void Update()
        {
            if (!selected)
            {
                return;
            }

            if (EscapePressed())
            {
                Deselect();
            }
        }

        private void OnDestroy()
        {
            if (selectedElement == this)
            {
                selectedElement = null;
            }
        }

        public void BeginResize(int handleIndex)
        {
            if (draggingFromPalette)
            {
                return;
            }

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

            var movingCornerLocal = CornerLocal(activeHandle);
            var movingCornerWorld = transform.TransformPoint(movingCornerLocal);
            var startVector = movingCornerWorld - resizeFixedCornerWorld;
            var currentVector = MouseWorld() - resizeFixedCornerWorld;
            var denominator = Vector3.Dot(startVector, startVector);

            if (denominator <= Mathf.Epsilon)
            {
                return;
            }

            var factor = Vector3.Dot(currentVector, startVector) / denominator;
            var nextScale = Mathf.Clamp(transform.localScale.x * factor, minScale, maxScale);
            ScaleAroundWorldPoint(nextScale, resizeFixedCornerWorld);
        }

        public void EndResize()
        {
            resizing = false;
        }

        private void Select()
        {
            if (selectedElement != null && selectedElement != this)
            {
                selectedElement.Deselect();
            }

            selectedElement = this;
            selected = true;
            SetSelectionVisualsVisible(true);
        }

        private void Deselect()
        {
            if (selectedElement == this)
            {
                selectedElement = null;
            }

            selected = false;
            moving = false;
            resizing = false;
            SetSelectionVisualsVisible(false);
        }

        private void ScaleAroundWorldPoint(float nextScale, Vector3 fixedWorldPoint)
        {
            var before = fixedWorldPoint;
            var fixedLocal = transform.InverseTransformPoint(fixedWorldPoint);
            transform.localScale = Vector3.one * nextScale;
            var after = transform.TransformPoint(fixedLocal);
            transform.position += before - after;
        }

        private void EnsureSelectionVisuals()
        {
            if (selectionRoot != null || spriteRenderer == null)
            {
                return;
            }

            selectionRoot = new GameObject("Selection");
            selectionRoot.transform.SetParent(transform, false);
            selectionRoot.transform.localPosition = Vector3.zero;
            selectionRoot.transform.localRotation = Quaternion.identity;
            selectionRoot.transform.localScale = Vector3.one;

            var bounds = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            var center = bounds.center;
            var extents = bounds.extents;

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
            borderRenderer.material = new Material(Shader.Find("Sprites/Default"));
            borderRenderer.sortingOrder = spriteRenderer.sortingOrder + 20;
            borderRenderer.SetPosition(0, center + new Vector3(-extents.x, extents.y, 0f));
            borderRenderer.SetPosition(1, center + new Vector3(extents.x, extents.y, 0f));
            borderRenderer.SetPosition(2, center + new Vector3(extents.x, -extents.y, 0f));
            borderRenderer.SetPosition(3, center + new Vector3(-extents.x, -extents.y, 0f));

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
            handleObject.transform.localScale = Vector3.one * handleSize;

            var renderer = handleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetHandleSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = spriteRenderer.sortingOrder + 21;

            var collider = handleObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var resizeHandle = handleObject.AddComponent<PlacedMapElementResizeHandle>();
            resizeHandle.Initialize(this, (int)handle);
        }

        private Vector3 CornerLocal(ResizeHandle handle)
        {
            var bounds = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds
                : new Bounds(Vector3.zero, Vector3.one);
            var center = bounds.center;
            var extents = bounds.extents;

            return handle switch
            {
                ResizeHandle.TopLeft => center + new Vector3(-extents.x, extents.y, 0f),
                ResizeHandle.TopRight => center + new Vector3(extents.x, extents.y, 0f),
                ResizeHandle.BottomRight => center + new Vector3(extents.x, -extents.y, 0f),
                _ => center + new Vector3(-extents.x, -extents.y, 0f),
            };
        }

        private Vector3 OppositeCornerLocal(ResizeHandle handle)
        {
            return handle switch
            {
                ResizeHandle.TopLeft => CornerLocal(ResizeHandle.BottomRight),
                ResizeHandle.TopRight => CornerLocal(ResizeHandle.BottomLeft),
                ResizeHandle.BottomRight => CornerLocal(ResizeHandle.TopLeft),
                _ => CornerLocal(ResizeHandle.TopRight),
            };
        }

        private void SetSelectionVisualsVisible(bool visible)
        {
            if (selectionRoot != null)
            {
                selectionRoot.SetActive(visible);
            }
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

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
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

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
