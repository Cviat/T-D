using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class CharacterEditorDialog : MonoBehaviour
    {
        public static void ShowOpenCharacter(IReadOnlyList<string> characterPaths, Action<string> onOpen)
        {
            ShowOpenCharacter(characterPaths, onOpen, null);
        }

        public static void ShowOpenCharacter(IReadOnlyList<string> characterPaths, Action<string> onOpen, Action onCreate)
        {
            var canvasObject = new GameObject("Character Open Dialog", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1300;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 600f);
            panel.AddComponent<Image>().color = new Color(0.045f, 0.041f, 0.037f, 0.98f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(panel.transform, "Characters");

            if (onCreate != null)
            {
                CreateButton(panel.transform, "Create Character", () =>
                {
                    Destroy(canvasObject);
                    onCreate.Invoke();
                });
            }

            var scrollRoot = new GameObject("Scroll View", typeof(RectTransform));
            scrollRoot.transform.SetParent(panel.transform, false);
            var scrollLayoutElement = scrollRoot.AddComponent<LayoutElement>();
            scrollLayoutElement.flexibleHeight = 1f;

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 35f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 14f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            if (characterPaths.Count == 0)
            {
                CreateLabel(content.transform, "No saved characters");
            }
            else
            {
                foreach (var path in characterPaths)
                {
                    var captured = path;
                    CreateButton(content.transform, UserCharacterStore.GetDisplayName(path), () =>
                    {
                        Destroy(canvasObject);
                        onOpen?.Invoke(captured);
                    });
                }
            }

            CreateButton(panel.transform, "Cancel", () => Destroy(canvasObject));
        }

        private static Font GetDefaultFont()
        {
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude);
            foreach (var t in texts)
            {
                if (t.font != null) return t.font;
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void CreateLabel(Transform parent, string label)
        {
            var labelObject = new GameObject(label, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            var text = labelObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = GetDefaultFont();
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 24;
            text.color = Color.white;
            labelObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        }

        private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = GetDefaultFont();
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }
    }
}
