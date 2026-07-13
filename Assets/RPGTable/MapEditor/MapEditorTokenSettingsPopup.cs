using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RPGTable.Core;

namespace RPGTable.MapEditor
{
    public sealed class MapEditorTokenSettingsPopup : MonoBehaviour
    {
        private static MapEditorTokenSettingsPopup current;

        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;
        private PlacedMapToken targetToken;

        public static void Show(Vector2 screenPosition, PlacedMapToken token)
        {
            if (current == null)
            {
                current = Create();
            }

            current.ShowInternal(screenPosition, token);
        }

        private static MapEditorTokenSettingsPopup Create()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Map Editor Token Settings Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var popupObject = new GameObject("Token Settings Popup", typeof(RectTransform));
            popupObject.transform.SetParent(canvasObject.transform, false);

            var popup = popupObject.AddComponent<MapEditorTokenSettingsPopup>();
            popup.canvasRectTransform = canvasObject.GetComponent<RectTransform>();
            popup.rectTransform = popupObject.GetComponent<RectTransform>();
            popup.rectTransform.sizeDelta = new Vector2(180f, 170f);

            var background = popupObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

            var layout = popupObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var border = new GameObject("Border", typeof(RectTransform));
            border.transform.SetParent(popupObject.transform, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;
            var borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var outline = border.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            popup.CreateMenuItem("Враг (Enemy)", new Color(0.45f, 0.15f, 0.15f, 1f), Color.white, () => popup.SetTokenTeam(TokenTeam.Enemy));
            popup.CreateMenuItem("Союзник (Ally)", new Color(0.15f, 0.45f, 0.15f, 1f), Color.white, () => popup.SetTokenTeam(TokenTeam.Ally));
            popup.CreateMenuItem("Нейтральный", new Color(0.45f, 0.38f, 0.15f, 1f), Color.white, () => popup.SetTokenTeam(TokenTeam.Neutral));
            popup.CreateMenuItem("Удалить", new Color(0.25f, 0.25f, 0.25f, 1f), Color.yellow, () => popup.DeleteToken());

            popupObject.SetActive(false);
            return popup;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void CreateMenuItem(string label, Color normalColor, Color textColor, Action onClick)
        {
            var btnGo = new GameObject(label, typeof(RectTransform));
            btnGo.transform.SetParent(rectTransform, false);

            var bg = btnGo.AddComponent<Image>();
            bg.color = normalColor;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => {
                onClick?.Invoke();
                gameObject.SetActive(false);
            });

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            var txtRect = textGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var txt = textGo.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = textColor;
            txt.raycastTarget = false;
        }

        private void ShowInternal(Vector2 screenPosition, PlacedMapToken token)
        {
            targetToken = token;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                null,
                out var localPosition);

            rectTransform.anchoredPosition = ClampToCanvas(localPosition + new Vector2(90f, -85f));
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
            if (!gameObject.activeSelf) return;

            if (EscapePressed() || MouseClickedOutside())
            {
                gameObject.SetActive(false);
            }
        }

        private bool MouseClickedOutside()
        {
#if ENABLE_INPUT_SYSTEM
            var mousePressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            var mousePos = UnityEngine.InputSystem.Mouse.current != null ? (Vector2)UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
#else
            var mousePressed = UnityEngine.Input.GetMouseButtonDown(0);
            var mousePos = (Vector2)UnityEngine.Input.mousePosition;
#endif
            if (!mousePressed) return false;

            return !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, null);
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void SetTokenTeam(TokenTeam team)
        {
            if (targetToken != null)
            {
                targetToken.SetTeam(team);
            }
        }

        private void DeleteToken()
        {
            if (targetToken != null)
            {
                Destroy(targetToken.gameObject);
            }
        }
    }
}
