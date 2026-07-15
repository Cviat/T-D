using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
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
