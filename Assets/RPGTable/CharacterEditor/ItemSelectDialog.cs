using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;

namespace RPGTable.CharacterEditor
{
    public sealed class ItemSelectDialog : MonoBehaviour
    {
        public static void Show(ItemType slotType, Sprite dialogFrame, Sprite dialogBg, Action<string> onSelect)
        {
            var canvasObject = new GameObject("Item Select Dialog", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1400; // On top of editor
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Background dimmer
            var dimmer = new GameObject("Dimmer", typeof(RectTransform));
            dimmer.transform.SetParent(canvasObject.transform, false);
            Stretch(dimmer);
            dimmer.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var dimmerBtn = dimmer.AddComponent<Button>();
            dimmerBtn.onClick.AddListener(() => Destroy(canvasObject));

            // Main Popup Panel
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 360f); // Tightly sized 360x360 panel
            
            // Frame layer: Draw the frame in its full gold colors!
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = Color.white; // Gold frame fully bright!
            panelImg.sprite = dialogFrame != null ? dialogFrame : FindFrameSprite();

            // Background layer: Draw the dark textured background inside the frame!
            var bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(panel.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.offsetMin = new Vector2(10f, 10f); // Sit neatly inside frame borders
            bgRt.offsetMax = new Vector2(-10f, -10f);
            
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = dialogBg;
            bgImg.color = dialogBg != null ? new Color(0.18f, 0.14f, 0.11f, 0.98f) : new Color(0.065f, 0.058f, 0.048f, 0.98f);

            // Scroll View filling the background container! (with 10px padding on edges to avoid frame edges)
            var scrollRoot = new GameObject("Scroll View", typeof(RectTransform));
            scrollRoot.transform.SetParent(bgObj.transform, false);
            var scrollRt = scrollRoot.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = Vector2.zero;
            scrollRt.sizeDelta = Vector2.zero;
            scrollRt.offsetMin = new Vector2(10f, 10f);
            scrollRt.offsetMax = new Vector2(-10f, -10f);

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 35f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.anchoredPosition = Vector2.zero;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Tight grid layout (4 columns of 80x80 cells, 0 spacing, centered)
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80f, 80f);
            grid.spacing = Vector2.zero;
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            var slotFrame = FindSlotFrameSprite();

            // Load and filter items
            var itemCards = Resources.LoadAll<ItemCard>("ItemCards");
            var matchingItems = new List<ItemCard>();

            foreach (var card in itemCards)
            {
                if (card == null) continue;
                if (slotType == ItemType.General || card.itemType == slotType)
                {
                    matchingItems.Add(card);
                }
            }

            // Cell index 0: Clear / Empty cell
            CreateItemCardButton(content.transform, "Снять предмет", null, slotFrame, () =>
            {
                Destroy(canvasObject);
                onSelect?.Invoke(string.Empty);
            });

            int loadedCount = matchingItems.Count;
            int totalCells = Mathf.Max(16, loadedCount + 1); // Pad to minimum of 16 cells (4x4 grid)

            // Populate matching items
            for (var i = 0; i < loadedCount; i++)
            {
                var card = matchingItems[i];
                var name = card.title;
                var icon = card.icon;
                CreateItemCardButton(content.transform, name, icon, slotFrame, () =>
                {
                    Destroy(canvasObject);
                    onSelect?.Invoke(name);
                });
            }

            // Populate remaining cells as empty cosmetic slots
            int remaining = totalCells - (loadedCount + 1);
            for (var i = 0; i < remaining; i++)
            {
                CreateItemCardButton(content.transform, string.Empty, null, slotFrame, null);
            }
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

        private static Sprite FindFrameSprite()
        {
            var images = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (var i in images)
            {
                if (i.gameObject.name == "Portrait Frame" && i.sprite != null) return i.sprite;
            }
            return null;
        }

        private static Sprite FindSlotFrameSprite()
        {
            var inputs = UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include);
            foreach (var inp in inputs)
            {
                var drop = inp.GetComponent<ItemDropSlot>();
                if (drop != null && inp.image != null && inp.image.sprite != null)
                {
                    return inp.image.sprite;
                }
            }
            return null;
        }

        private static void CreateItemCardButton(Transform parent, string label, Sprite itemIcon, Sprite slotFrame, UnityEngine.Events.UnityAction action)
        {
            var cardObj = new GameObject(label, typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);
            var cardImage = cardObj.AddComponent<Image>();
            cardImage.sprite = slotFrame;
            cardImage.color = Color.white;

            if (action != null)
            {
                var button = cardObj.AddComponent<Button>();
                button.targetGraphic = cardImage;
                button.onClick.AddListener(action);

                // Add Tooltip Trigger component
                var trigger = cardObj.AddComponent<ItemTooltipTrigger>();
                trigger.itemName = label;
            }

            // Icon centered and filling the slot (stretches with offset)
            if (itemIcon != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(cardObj.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8f, 8f);
                iconRect.offsetMax = new Vector2(-8f, -8f);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = itemIcon;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }
            else if (action != null && label == "Снять предмет")
            {
                // Draw a subtle red cross or empty silhouette for clearing
                var iconObj = new GameObject("Clear Cross", typeof(RectTransform));
                iconObj.transform.SetParent(cardObj.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(16f, 16f);
                iconRect.offsetMax = new Vector2(-16f, -16f);
                var txt = iconObj.AddComponent<Text>();
                txt.text = "✖";
                txt.font = GetDefaultFont();
                txt.fontStyle = FontStyle.Bold;
                txt.fontSize = 24;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.85f, 0.3f, 0.3f, 0.65f);
                txt.raycastTarget = false;
            }
        }

        private static void Stretch(GameObject gameObject)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
