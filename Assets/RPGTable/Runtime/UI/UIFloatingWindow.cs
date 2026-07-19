using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class UIFloatingWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private RectTransform resizeHandle;
        [SerializeField] private GameObject contentParent;
        [SerializeField] private Button minimizeButton;
        [SerializeField] private Text minimizeButtonText;

        private Vector2 normalSize = new Vector2(400f, 300f);
        private bool isMinimized;

        public RectTransform WindowRect => windowRect;
        public RectTransform TitleBar => titleBar;
        public RectTransform ResizeHandle => resizeHandle;
        public GameObject ContentParent => contentParent;

        public void Initialize(string title)
        {
            if (windowRect == null) windowRect = GetComponent<RectTransform>();

            normalSize = windowRect.sizeDelta;

            if (minimizeButton != null)
            {
                minimizeButton.onClick.RemoveAllListeners();
                minimizeButton.onClick.AddListener(ToggleMinimize);
            }

            // Setup Drag Handler on Title Bar
            if (titleBar != null)
            {
                var drag = titleBar.gameObject.GetComponent<UIWindowDragHandler>();
                if (drag == null) drag = titleBar.gameObject.AddComponent<UIWindowDragHandler>();
                drag.Initialize(windowRect);
            }

            // Setup Resize Handler on Resize Handle
            if (resizeHandle != null)
            {
                var resize = resizeHandle.gameObject.GetComponent<UIResizeHandler>();
                if (resize == null) resize = resizeHandle.gameObject.AddComponent<UIResizeHandler>();
                resize.Initialize(windowRect, this);
            }

            UpdateMinimizeUI();
        }

        private void Start()
        {
            Initialize("Campaign Graph");
        }

        public void ToggleMinimize()
        {
            gameObject.SetActive(false);
        }

        private void UpdateMinimizeUI()
        {
            if (minimizeButtonText != null)
            {
                minimizeButtonText.text = isMinimized ? "[+]" : "[-]";
            }
        }
    }

    public sealed class UIWindowDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        private RectTransform targetRect;
        private Canvas canvas;

        public void Initialize(RectTransform target)
        {
            targetRect = target;
            canvas = target.GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetRect != null)
            {
                targetRect.SetAsLastSibling(); // Bring to front when clicked
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetRect == null || canvas == null) return;

            // Move the window based on delta, adjusted by Canvas scale factor
            targetRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
            ClampToCanvas();
        }

        private void ClampToCanvas()
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) return;

            // Simple clamping to keep title bar within canvas
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);

            Vector3[] windowCorners = new Vector3[4];
            targetRect.GetWorldCorners(windowCorners);

            // Calculate offset if out of canvas bounds
            float minX = canvasCorners[0].x;
            float maxX = canvasCorners[2].x;
            float minY = canvasCorners[0].y;
            float maxY = canvasCorners[2].y;

            Vector3 pos = targetRect.position;

            // Clamp title bar position
            float width = windowCorners[2].x - windowCorners[0].x;
            float height = windowCorners[2].y - windowCorners[0].y;

            pos.x = Mathf.Clamp(pos.x, minX + width * 0.1f, maxX - width * 0.1f);
            pos.y = Mathf.Clamp(pos.y, minY + height * 0.1f, maxY - height * 0.1f);

            targetRect.position = pos;
        }
    }

    public sealed class UIResizeHandler : MonoBehaviour, IDragHandler
    {
        private RectTransform targetRect;
        private UIFloatingWindow parentWindow;
        private Vector2 minSize = new Vector2(250f, 180f);
        private Vector2 maxSize = new Vector2(1000f, 800f);

        public void Initialize(RectTransform target, UIFloatingWindow window)
        {
            targetRect = target;
            parentWindow = window;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetRect == null) return;

            var canvas = targetRect.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Vector2 delta = eventData.delta / canvas.scaleFactor;
            Vector2 size = targetRect.sizeDelta;

            // Resize delta grows downwards and rightwards
            size.x += delta.x;
            size.y -= delta.y; // Inverted because UI coordinates decrease downwards

            size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);

            targetRect.sizeDelta = size;
        }
    }
}
