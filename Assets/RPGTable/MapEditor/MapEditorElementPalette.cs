using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapEditorElementPalette : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private MapEditorElementSpawner spawner;

        public void ImportImage()
        {
            var importedPath = UserElementAssetStore.ImportImageWithDialog();

            if (!string.IsNullOrWhiteSpace(importedPath))
            {
                Reload();
            }
        }

        public void Reload()
        {
            Clear();

            foreach (var path in UserElementAssetStore.GetImagePaths())
            {
                var sprite = UserElementAssetStore.LoadSprite(path);

                if (sprite != null)
                {
                    CreateItem(path, sprite);
                }
            }
        }

        private void Start()
        {
            EnsureMapControls();
            Reload();
        }

        private void EnsureMapControls()
        {
            var mapController = GetComponent<MapEditorMapController>();

            if (mapController == null)
            {
                mapController = gameObject.AddComponent<MapEditorMapController>();
            }

            mapController.Initialize(spawner);

            EnsureMapButton("Back Button", "Back", new Vector2(0f, -18f), mapController, MapEditorMapButtonAction.Back);
            EnsureMapButton("Save Map Button", "Save Map", new Vector2(0f, -76f), mapController, MapEditorMapButtonAction.Save);
            EnsureMapButton("Open Map Button", "Import Map", new Vector2(0f, -134f), mapController, MapEditorMapButtonAction.Open);
            EnsureMapButton("Add Exit Button", "Add Exit", new Vector2(0f, -192f), mapController, MapEditorMapButtonAction.AddExit);
            EnsureMapButton("Add Spawn Button", "Add Spawn", new Vector2(0f, -250f), mapController, MapEditorMapButtonAction.AddSpawn);

            var importButton = transform.Find("Import Image Button") as RectTransform;

            if (importButton != null)
            {
                importButton.sizeDelta = new Vector2(240f, 48f);
                importButton.anchorMin = new Vector2(0.5f, 1f);
                importButton.anchorMax = new Vector2(0.5f, 1f);
                importButton.pivot = new Vector2(0.5f, 1f);
                importButton.anchoredPosition = new Vector2(0f, -308f);
            }

            if (contentRoot != null)
            {
                contentRoot.offsetMax = new Vector2(-18f, -374f);
            }
        }

        private void EnsureMapButton(
            string buttonName,
            string label,
            Vector2 anchoredPosition,
            MapEditorMapController mapController,
            MapEditorMapButtonAction action)
        {
            if (transform.Find(buttonName) != null)
            {
                return;
            }

            var buttonObject = CreateRuntimeTextButton(buttonName, label);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;

            var binder = buttonObject.AddComponent<MapEditorMapButton>();
            binder.Initialize(mapController, action);
        }

        private GameObject CreateRuntimeTextButton(string buttonName, string label)
        {
            var buttonObject = new GameObject(buttonName, typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 48f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            return buttonObject;
        }

        private void CreateItem(string path, Sprite sprite)
        {
            var item = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(RectTransform));
            item.transform.SetParent(contentRoot, false);

            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;

            var paletteItem = item.AddComponent<MapEditorPaletteItem>();
            paletteItem.Initialize(path, sprite, spawner, this);
        }

        private void Clear()
        {
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }

    public sealed class MapEditorPaletteItem : MonoBehaviour, IPointerClickHandler
    {
        private string path;
        private Sprite sprite;
        private MapEditorElementSpawner spawner;
        private MapEditorElementPalette palette;

        public void Initialize(
            string imagePath,
            Sprite itemSprite,
            MapEditorElementSpawner elementSpawner,
            MapEditorElementPalette elementPalette)
        {
            path = imagePath;
            sprite = itemSprite;
            spawner = elementSpawner;
            palette = elementPalette;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                spawner.BeginDrag(path, sprite);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            MapEditorDeletePopup.Show(eventData.position, "Удалить", DeleteImportedImage);
        }

        private void DeleteImportedImage()
        {
            if (UserElementAssetStore.DeleteImage(path))
            {
                palette.Reload();
            }
        }
    }

    public sealed class MapEditorDeletePopup : MonoBehaviour
    {
        private const float Width = 132f;
        private const float Height = 42f;

        private static MapEditorDeletePopup current;

        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;
        private Action deleteAction;

        public static void Show(Vector2 screenPosition, string label, Action onDelete)
        {
            if (current == null)
            {
                current = Create();
            }

            current.ShowInternal(screenPosition, label, onDelete);
        }

        private static MapEditorDeletePopup Create()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Map Editor Delete Popup Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var popupObject = new GameObject("Delete Popup", typeof(RectTransform));
            popupObject.transform.SetParent(canvasObject.transform, false);

            var popup = popupObject.AddComponent<MapEditorDeletePopup>();
            popup.canvasRectTransform = canvasObject.GetComponent<RectTransform>();
            popup.rectTransform = popupObject.GetComponent<RectTransform>();
            popup.rectTransform.sizeDelta = new Vector2(Width, Height);

            var background = popupObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.055f, 0.045f, 0.96f);

            var button = popupObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(popup.Delete);

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(popupObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 18;
            text.color = Color.white;
            text.raycastTarget = false;

            popupObject.SetActive(false);
            return popup;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void ShowInternal(Vector2 screenPosition, string label, Action onDelete)
        {
            deleteAction = onDelete;
            GetComponentInChildren<Text>().text = label;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                null,
                out var localPosition);

            rectTransform.anchoredPosition = ClampToCanvas(localPosition + new Vector2(72f, -18f));
            gameObject.SetActive(true);
        }

        private Vector2 ClampToCanvas(Vector2 position)
        {
            var halfCanvas = canvasRectTransform.rect.size * 0.5f;
            var halfPopup = rectTransform.sizeDelta * 0.5f;

            position.x = Mathf.Clamp(position.x, -halfCanvas.x + halfPopup.x, halfCanvas.x - halfPopup.x);
            position.y = Mathf.Clamp(position.y, -halfCanvas.y + halfPopup.y, halfCanvas.y - halfPopup.y);
            return position;
        }

        private void Update()
        {
            if (!gameObject.activeSelf || !EscapePressed())
            {
                return;
            }

            gameObject.SetActive(false);
        }

        private void Delete()
        {
            var action = deleteAction;
            gameObject.SetActive(false);
            action?.Invoke();
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }

    public enum MapEditorMapButtonAction
    {
        Back,
        Save,
        Open,
        AddExit,
        AddSpawn
    }

    [RequireComponent(typeof(Button))]
    public sealed class MapEditorMapButton : MonoBehaviour
    {
        [SerializeField] private MapEditorMapController controller;
        [SerializeField] private MapEditorMapButtonAction action;

        public void Initialize(MapEditorMapController mapController, MapEditorMapButtonAction buttonAction)
        {
            controller = mapController;
            action = buttonAction;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Click);
        }

        private void Click()
        {
            if (controller == null)
            {
                Debug.LogWarning("Map button has no controller.");
                return;
            }

            if (action == MapEditorMapButtonAction.Back)
            {
                controller.BackToMainMenu();
            }
            else if (action == MapEditorMapButtonAction.Save)
            {
                controller.RequestSaveMap();
            }
            else if (action == MapEditorMapButtonAction.Open)
            {
                controller.RequestOpenMap();
            }
            else if (action == MapEditorMapButtonAction.AddExit)
            {
                controller.BeginAddExitPoint();
            }
            else
            {
                controller.BeginAddSpawnZone();
            }
        }
    }

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
            borderRenderer.material = new Material(Shader.Find("Sprites/Default"));
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

    public sealed class MapSpawnZone : MonoBehaviour
    {
        private const float SpawnZoneZ = -0.18f;

        private enum ResizeHandle
        {
            TopLeft,
            TopRight,
            BottomRight,
            BottomLeft
        }

        private static Sprite markerSprite;
        private static Sprite handleSprite;
        private static MapSpawnZone selectedSpawnZone;

        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Vector2 size = new Vector2(1.2f, 1.2f);
        [SerializeField] private float minSize = 0.4f;
        [SerializeField] private Color selectionColor = new Color(0.25f, 1f, 0.45f, 1f);
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

        public void Initialize(string spawnId, string spawnName, Vector2 triggerSize)
        {
            id = spawnId;
            displayName = spawnName;
            size = triggerSize;
            name = string.IsNullOrWhiteSpace(spawnName) ? "Spawn Zone" : spawnName;
            EnsureVisuals();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureVisuals();
        }

        private void Update()
        {
            if (selectedSpawnZone == this && EscapePressed())
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

            transform.position = WithSpawnZoneZ(MouseWorld());
        }

        private void OnMouseUp()
        {
            moving = false;
        }

        private void OnDestroy()
        {
            if (selectedSpawnZone == this)
            {
                selectedSpawnZone = null;
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

            transform.position = new Vector3(center.x, center.y, SpawnZoneZ);
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
            renderer.color = new Color(0.1f, 1f, 0.35f, 0.32f);
            renderer.sortingOrder = 201;

            var collider = GetComponent<BoxCollider2D>();

            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = true;
            collider.size = Vector2.one;
            EnsureSelectionVisuals();
            ApplySize();
            SetSelectionVisualsVisible(selectedSpawnZone == this);
        }

        private void ApplySize()
        {
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void Select()
        {
            if (selectedSpawnZone != null && selectedSpawnZone != this)
            {
                selectedSpawnZone.Deselect();
            }

            selectedSpawnZone = this;
            SetSelectionVisualsVisible(true);
        }

        private void Deselect()
        {
            if (selectedSpawnZone == this)
            {
                selectedSpawnZone = null;
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
            borderRenderer.material = new Material(Shader.Find("Sprites/Default"));
            borderRenderer.sortingOrder = 222;
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
            renderer.sortingOrder = 223;

            var collider = handleObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var resizeHandle = handleObject.AddComponent<MapSpawnZoneResizeHandle>();
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

                if (child.GetComponent<MapSpawnZoneResizeHandle>() != null)
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
            world.z = SpawnZoneZ;
            return world;
        }

        private static Vector3 WithSpawnZoneZ(Vector3 position)
        {
            position.z = SpawnZoneZ;
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

    public sealed class MapEditorMapController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private MapEditorElementSpawner spawner;

        private string currentMapName;
        private bool placingExitPoint;
        private bool placingSpawnZone;

        public void Initialize(MapEditorElementSpawner elementSpawner)
        {
            spawner = elementSpawner;
        }

        public void RequestSaveMap()
        {
            MapEditorMapDialog.ShowSave(currentMapName, SaveMap);
        }

        public void RequestOpenMap()
        {
            MapEditorMapDialog.ShowOpen(UserMapStore.GetMapPaths(), OpenMap);
        }

        public void BeginAddExitPoint()
        {
            placingExitPoint = true;
            placingSpawnZone = false;
        }

        public void BeginAddSpawnZone()
        {
            placingSpawnZone = true;
            placingExitPoint = false;
        }

        public void BackToMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            Debug.LogWarning($"Scene '{mainMenuSceneName}' is not in Build Settings yet.");
        }

        private void Update()
        {
            if (!placingExitPoint && !placingSpawnZone)
            {
                return;
            }

            if (EscapePressed())
            {
                placingExitPoint = false;
                placingSpawnZone = false;
                return;
            }

            if (!PrimaryPressedThisFrame() || IsPointerOverUi())
            {
                return;
            }

            if (placingExitPoint)
            {
                CreateExitPoint(MouseWorld(), null, null, new Vector2(1.2f, 1.2f));
                placingExitPoint = false;
                return;
            }

            CreateSpawnZone(MouseWorld(), null, null, new Vector2(1.2f, 1.2f));
            placingSpawnZone = false;
        }

        private void SaveMap(string mapName)
        {
            var placedElements = FindObjectsByType<PlacedMapElement>(FindObjectsInactive.Exclude);
            var exitPoints = FindObjectsByType<MapExitPoint>(FindObjectsInactive.Exclude);
            var spawnZones = FindObjectsByType<MapSpawnZone>(FindObjectsInactive.Exclude);
            var saveableElements = new System.Collections.Generic.List<PlacedMapElement>();
            var saveableExitPoints = new System.Collections.Generic.List<MapExitPoint>();
            var saveableSpawnZones = new System.Collections.Generic.List<MapSpawnZone>();

            foreach (var element in placedElements)
            {
                if (!string.IsNullOrWhiteSpace(element.SourceImagePath))
                {
                    saveableElements.Add(element);
                }
            }

            foreach (var exitPoint in exitPoints)
            {
                saveableExitPoints.Add(exitPoint);
            }

            foreach (var spawnZone in spawnZones)
            {
                saveableSpawnZones.Add(spawnZone);
            }

            if (!string.IsNullOrWhiteSpace(UserMapStore.SaveMap(mapName, saveableElements, saveableExitPoints, saveableSpawnZones)))
            {
                currentMapName = mapName.Trim();
            }
        }

        private void OpenMap(string path)
        {
            var data = UserMapStore.LoadMap(path);

            if (data == null || spawner == null)
            {
                return;
            }

            spawner.ClearPlacedElements();
            ClearExitPoints();
            ClearSpawnZones();
            currentMapName = data.name;

            if (data.elements != null)
            {
                foreach (var element in data.elements)
                {
                    var sprite = UserElementAssetStore.LoadSprite(element.imagePath);

                    if (sprite != null)
                    {
                        spawner.CreatePlacedElement(element.imagePath, sprite, element.position, element.scale);
                    }
                }
            }

            if (data.exitPoints != null)
            {
                foreach (var exitPoint in data.exitPoints)
                {
                    CreateExitPoint(exitPoint.position, exitPoint.id, exitPoint.name, exitPoint.size);
                }
            }

            if (data.spawnZones == null)
            {
                return;
            }

            foreach (var spawnZone in data.spawnZones)
            {
                CreateSpawnZone(spawnZone.position, spawnZone.id, spawnZone.name, spawnZone.size);
            }
        }

        private static MapExitPoint CreateExitPoint(Vector3 position, string id, string displayName, Vector2 size)
        {
            var exitObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Exit Point" : displayName);
            position.z = -0.2f;
            exitObject.transform.position = position;
            var exitPoint = exitObject.AddComponent<MapExitPoint>();
            exitPoint.Initialize(
                string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
                string.IsNullOrWhiteSpace(displayName) ? "Exit" : displayName,
                size == Vector2.zero ? new Vector2(1.2f, 1.2f) : size);
            return exitPoint;
        }

        private static MapSpawnZone CreateSpawnZone(Vector3 position, string id, string displayName, Vector2 size)
        {
            var spawnObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Spawn Zone" : displayName);
            position.z = -0.18f;
            spawnObject.transform.position = position;
            var spawnZone = spawnObject.AddComponent<MapSpawnZone>();
            spawnZone.Initialize(
                string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
                string.IsNullOrWhiteSpace(displayName) ? "Spawn" : displayName,
                size == Vector2.zero ? new Vector2(1.2f, 1.2f) : size);
            return spawnZone;
        }

        private static void ClearExitPoints()
        {
            foreach (var exitPoint in FindObjectsByType<MapExitPoint>(FindObjectsInactive.Exclude))
            {
                Destroy(exitPoint.gameObject);
            }
        }

        private static void ClearSpawnZones()
        {
            foreach (var spawnZone in FindObjectsByType<MapSpawnZone>(FindObjectsInactive.Exclude))
            {
                Destroy(spawnZone.gameObject);
            }
        }

        private static Vector3 MouseWorld()
        {
            var camera = Camera.main;

            if (camera == null)
            {
                return Vector3.zero;
            }

            var mouse = MousePosition();
            mouse.z = Mathf.Abs(camera.transform.position.z);
            var world = camera.ScreenToWorldPoint(mouse);
            world.z = -0.2f;
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

        private static bool PrimaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
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

    public sealed class MapEditorMapDialog : MonoBehaviour
    {
        private static MapEditorMapDialog current;

        private RectTransform contentRoot;
        private Action<string> submitAction;

        public static void ShowSave(string currentName, Action<string> onSave, string title = "Save Map")
        {
            Ensure().ShowSaveInternal(currentName, onSave, title);
        }

        public static void ShowOpen(IReadOnlyList<string> mapPaths, Action<string> onOpen)
        {
            Ensure().ShowOpenInternal(mapPaths, onOpen);
        }

        private static MapEditorMapDialog Ensure()
        {
            if (current != null)
            {
                return current;
            }

            var canvasObject = new GameObject("Map Editor Map Dialog Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;

            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var overlay = new GameObject("Overlay", typeof(RectTransform));
            overlay.transform.SetParent(canvasObject.transform, false);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.48f);

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520f, 420f);
            panel.AddComponent<Image>().color = new Color(0.075f, 0.065f, 0.055f, 0.98f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(24f, 24f);
            contentRect.offsetMax = new Vector2(-24f, -24f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            current = panel.AddComponent<MapEditorMapDialog>();
            current.contentRoot = contentRect;
            canvasObject.SetActive(false);
            return current;
        }

        private void ShowSaveInternal(string currentName, Action<string> onSave, string title)
        {
            submitAction = onSave;
            Clear();
            transform.root.gameObject.SetActive(true);

            CreateLabel(title, 26, FontStyle.Bold);

            var input = CreateInputField(string.IsNullOrWhiteSpace(currentName) ? "New map" : currentName);
            input.Select();
            input.ActivateInputField();

            CreateButton("Save", () =>
            {
                if (string.IsNullOrWhiteSpace(input.text))
                {
                    return;
                }

                Close();
                submitAction?.Invoke(input.text);
            });

            CreateButton("Cancel", Close);
        }

        private void ShowOpenInternal(IReadOnlyList<string> mapPaths, Action<string> onOpen)
        {
            submitAction = onOpen;
            Clear();
            transform.root.gameObject.SetActive(true);

            CreateLabel("Import Map", 26, FontStyle.Bold);

            if (mapPaths.Count == 0)
            {
                CreateLabel("No saved maps", 20, FontStyle.Normal);
            }
            else
            {
                foreach (var path in mapPaths)
                {
                    var capturedPath = path;
                    CreateButton(UserMapStore.GetDisplayName(path), () =>
                    {
                        Close();
                        submitAction?.Invoke(capturedPath);
                    });
                }
            }

            CreateButton("Cancel", Close);
        }

        private void Clear()
        {
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private void Close()
        {
            transform.root.gameObject.SetActive(false);
        }

        private Text CreateLabel(string text, int fontSize, FontStyle style)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(contentRoot, false);
            var label = labelObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = style;
            label.fontSize = fontSize;
            label.color = Color.white;

            var layoutElement = labelObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 44f;
            return label;
        }

        private InputField CreateInputField(string value)
        {
            var inputObject = new GameObject("Map Name Input", typeof(RectTransform));
            inputObject.transform.SetParent(contentRoot, false);
            inputObject.AddComponent<Image>().color = new Color(0.92f, 0.9f, 0.86f, 1f);

            var input = inputObject.AddComponent<InputField>();

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(inputObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            input.textComponent = text;
            input.text = value;

            var layoutElement = inputObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 48f;
            return input;
        }

        private Button CreateButton(string label, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform));
            buttonObject.transform.SetParent(contentRoot, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 48f;
            return button;
        }
    }

}
