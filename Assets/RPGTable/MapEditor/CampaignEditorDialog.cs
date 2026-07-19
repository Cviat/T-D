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
            panelRect.sizeDelta = new Vector2(560f, 480f);
            panel.AddComponent<Image>().color = new Color(0.075f, 0.065f, 0.055f, 0.98f);

            var scrollView = new GameObject("ScrollView", typeof(RectTransform));
            scrollView.transform.SetParent(panel.transform, false);
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            var scrollRectTransform = scrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(24f, 24f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -24f);

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollView.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRect;

            CreateDialogLabel(content.transform, "Import Campaign");

            if (campaignPaths.Count == 0)
            {
                CreateDialogLabel(content.transform, "No saved campaigns");
            }
            else
            {
                foreach (var path in campaignPaths)
                {
                    var captured = path;
                    CreateDialogButton(content.transform, UserCampaignStore.GetDisplayName(path), () =>
                    {
                        Destroy(canvasObject);
                        onOpen?.Invoke(captured);
                    });
                }
            }

            CreateDialogButton(content.transform, "Cancel", () => Destroy(canvasObject));
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
