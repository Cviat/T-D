using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignEditorDialog : MonoBehaviour
    {
        public static void ShowOpenCampaign(IReadOnlyList<string> campaignPaths, Action<string> onOpen)
        {
            var canvasObject = new GameObject("Campaign Open Dialog Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520f, 420f);
            panel.AddComponent<Image>().color = new Color(0.075f, 0.065f, 0.055f, 0.98f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateDialogLabel(panel.transform, "Import Campaign");

            if (campaignPaths.Count == 0)
            {
                CreateDialogLabel(panel.transform, "No saved campaigns");
            }
            else
            {
                foreach (var path in campaignPaths)
                {
                    var captured = path;
                    CreateDialogButton(panel.transform, UserCampaignStore.GetDisplayName(path), () =>
                    {
                        Destroy(canvasObject);
                        onOpen?.Invoke(captured);
                    });
                }
            }

            CreateDialogButton(panel.transform, "Cancel", () => Destroy(canvasObject));
        }

        private static void CreateDialogLabel(Transform parent, string label)
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

        private static void CreateDialogButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
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
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }
    }
}
