using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapEditorDeletePopup : MonoBehaviour
    {
        private const float Width = 132f;
        private const float Height = 42f;

        private static MapEditorDeletePopup current;

        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;

        public static void Show(Vector2 screenPosition, string label, Action onDelete)
        {
            if (current == null)
            {
                current = Create();
            }

            current.ShowMenuInternal(screenPosition, new[] { new MenuAction(label, onDelete) });
        }

        public static void ShowMenu(Vector2 screenPosition, params MenuAction[] actions)
        {
            if (current == null)
            {
                current = Create();
            }

            current.ShowMenuInternal(screenPosition, actions);
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

            var layout = popupObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

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

        private void ShowMenuInternal(Vector2 screenPosition, MenuAction[] actions)
        {
            ClearChildren(transform);

            if (actions == null || actions.Length == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            rectTransform.sizeDelta = new Vector2(actions.Length > 1 ? 210f : Width, 8f + Height * actions.Length);

            foreach (var action in actions)
            {
                CreateButton(action.Label, action.Action);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                null,
                out var localPosition);

            rectTransform.anchoredPosition = ClampToCanvas(localPosition + new Vector2(72f, -18f));
            gameObject.SetActive(true);
        }

        private void CreateButton(string label, Action action)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);

            var background = buttonObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.055f, 0.045f, 0.96f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                action?.Invoke();
            });

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

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
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

        public readonly struct MenuAction
        {
            public readonly string Label;
            public readonly Action Action;

            public MenuAction(string label, Action action)
            {
                Label = label;
                Action = action;
            }
        }
    }
}
