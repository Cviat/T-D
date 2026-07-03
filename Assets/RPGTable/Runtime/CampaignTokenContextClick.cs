using System;
using RPGTable.GameMaster;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.Runtime
{
    [RequireComponent(typeof(CampaignRuntimeToken))]
    public sealed class CampaignTokenContextClick : MonoBehaviour
    {
        private CampaignGameLoader loader;
        private CampaignRuntimeToken runtimeToken;

        public void Initialize(CampaignGameLoader gameLoader, CampaignRuntimeToken token)
        {
            loader = gameLoader;
            runtimeToken = token;
        }

        private void Awake()
        {
            if (runtimeToken == null)
            {
                runtimeToken = GetComponent<CampaignRuntimeToken>();
            }

            if (loader == null)
            {
                loader = FindFirstObjectByType<CampaignGameLoader>();
            }
        }

        private void OnMouseOver()
        {
            if (ViewModeController.Instance != null && ViewModeController.Instance.IsPlayerView)
            {
                return;
            }

            if (!SecondaryMousePressed() || IsPointerOverUi())
            {
                return;
            }

            CampaignTokenContextPopup.Show(MousePosition(), loader, runtimeToken);
        }

        private static bool SecondaryMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(1);
#endif
        }

        private static Vector2 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }

    public sealed class CampaignTokenContextPopup : MonoBehaviour
    {
        private const float Width = 158f;
        private const float Height = 168f;
        private const float PlayerTokenHeight = 128f;

        private static CampaignTokenContextPopup current;

        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;
        private GameObject deleteButtonObject;
        private CampaignGameLoader loader;
        private CampaignRuntimeToken runtimeToken;
        private int openedFrame = -1;

        public static void Show(Vector2 screenPosition, CampaignGameLoader gameLoader, CampaignRuntimeToken token)
        {
            if (gameLoader == null || token == null)
            {
                return;
            }

            if (current == null)
            {
                current = Create();
            }

            current.ShowInternal(screenPosition, gameLoader, token);
        }

        private static CampaignTokenContextPopup Create()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Campaign Token Context Popup Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;

            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var popupObject = new GameObject("Token Context Popup", typeof(RectTransform));
            popupObject.transform.SetParent(canvasObject.transform, false);

            var popup = popupObject.AddComponent<CampaignTokenContextPopup>();
            popup.canvasRectTransform = canvasObject.GetComponent<RectTransform>();
            popup.rectTransform = popupObject.GetComponent<RectTransform>();
            popup.rectTransform.sizeDelta = new Vector2(Width, Height);

            var background = popupObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.047f, 0.040f, 0.97f);

            var layout = popupObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            popup.deleteButtonObject = popup.CreateButton("Удалить", new Color(0.34f, 0.075f, 0.06f, 1f), popup.Delete);
            popup.CreateButton("Посмотреть", new Color(0.13f, 0.10f, 0.075f, 1f), popup.CloseOnly);
            popup.CreateButton("Показать", new Color(0.13f, 0.10f, 0.075f, 1f), popup.CloseOnly);
            popup.CreateButton("Убить", new Color(0.18f, 0.055f, 0.045f, 1f), popup.Kill);

            popupObject.SetActive(false);
            return popup;
        }

        private GameObject CreateButton(string label, Color color, Action action)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());

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
            text.fontSize = 17;
            text.color = Color.white;
            text.raycastTarget = false;

            return buttonObject;
        }

        private static void EnsureEventSystem()
        {
            var eventSystemObject = EventSystem.current == null ? null : EventSystem.current.gameObject;

            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            foreach (var legacyModule in eventSystemObject.GetComponents<StandaloneInputModule>())
            {
                Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private void ShowInternal(Vector2 screenPosition, CampaignGameLoader gameLoader, CampaignRuntimeToken token)
        {
            loader = gameLoader;
            runtimeToken = token;
            var isPlayerToken = !string.IsNullOrWhiteSpace(token.PlayerId);

            if (deleteButtonObject != null)
            {
                deleteButtonObject.SetActive(!isPlayerToken);
            }

            rectTransform.sizeDelta = new Vector2(Width, isPlayerToken ? PlayerTokenHeight : Height);
            openedFrame = Time.frameCount;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                null,
                out var localPosition);

            rectTransform.anchoredPosition = ClampToCanvas(localPosition + new Vector2(86f, -42f));
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
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (Time.frameCount == openedFrame)
            {
                return;
            }

            if (EscapePressed() || ClickedOutsidePopup())
            {
                CloseOnly();
            }
        }

        private void Delete()
        {
            var gameLoader = loader;
            var token = runtimeToken;
            CloseOnly();
            gameLoader?.DeleteRuntimeToken(token);
        }

        private void Kill()
        {
            var gameLoader = loader;
            var token = runtimeToken;
            CloseOnly();
            gameLoader?.KillRuntimeToken(token);
        }

        private void CloseOnly()
        {
            gameObject.SetActive(false);
            loader = null;
            runtimeToken = null;
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private bool ClickedOutsidePopup()
        {
            if (!AnyMousePressed())
            {
                return false;
            }

            return !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, MousePosition(), null);
        }

        private static bool AnyMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame);
#else
            return UnityEngine.Input.GetMouseButtonDown(0) ||
                   UnityEngine.Input.GetMouseButtonDown(1) ||
                   UnityEngine.Input.GetMouseButtonDown(2);
#endif
        }

        private static Vector2 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }
    }
}
