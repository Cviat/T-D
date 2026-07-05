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

            if (characterPaths.Count == 0)
            {
                CreateLabel(panel.transform, "No saved characters");
            }
            else
            {
                foreach (var path in characterPaths)
                {
                    var captured = path;
                    CreateButton(panel.transform, UserCharacterStore.GetDisplayName(path), () =>
                    {
                        Destroy(canvasObject);
                        onOpen?.Invoke(captured);
                    });
                }
            }

            CreateButton(panel.transform, "Cancel", () => Destroy(canvasObject));
        }

        private static void CreateLabel(Transform parent, string label)
        {
            var labelObject = new GameObject(label, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            var text = labelObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }
    }
}
