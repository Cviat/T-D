using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignMapNode : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        private readonly List<CampaignExitPin> pins = new List<CampaignExitPin>();
        private CampaignEditorController controller;
        private RectTransform rectTransform;
        private Text startText;
        private Text cutsceneText;

        public readonly List<SavedCampaignTokenData> presetTokens = new List<SavedCampaignTokenData>();

        public string Id { get; private set; }
        public string MapPath { get; private set; }
        public string CutscenePath { get; private set; }
        public string CutsceneType { get; private set; }
        public RectTransform RectTransform => rectTransform;

        public void Initialize(
            CampaignEditorController campaignController,
            string nodeId,
            string mapPath,
            string displayName,
            SavedMapExitPointData[] exitPoints,
            bool isStart,
            string cutscenePath = null,
            string cutsceneType = null)
        {
            controller = campaignController;
            Id = nodeId;
            MapPath = mapPath;
            CutscenePath = cutscenePath;
            CutsceneType = cutsceneType;
            rectTransform = GetComponent<RectTransform>();
            CreatePreview();
            CreateLabel(displayName);
            CreateStartButton();
            CreateCutsceneBadge();
            SetStart(isStart);
            RefreshCutsceneBadge();

            if (exitPoints == null)
            {
                return;
            }

            foreach (var exitPoint in exitPoints)
            {
                CreatePin(exitPoint);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
            {
                controller.OpenMapForPresetTokens(this);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            MapEditorDeletePopup.ShowMenu(
                eventData.position,
                new MapEditorDeletePopup.MenuAction("Добавить кат-сцену", () => controller.AddCutsceneToMap(this)),
                new MapEditorDeletePopup.MenuAction("Удалить", () => controller.RemoveMapNode(this)));
        }

        public void SetCutscene(string path, string type)
        {
            CutscenePath = path;
            CutsceneType = type;
            RefreshCutsceneBadge();
        }

        public CampaignExitPin FindPin(string exitId)
        {
            foreach (var pin in pins)
            {
                if (pin.ExitId == exitId)
                {
                    return pin;
                }
            }

            return null;
        }

        public void SetStart(bool isStart)
        {
            if (startText != null)
            {
                startText.text = isStart ? "START" : "Set Start";
                startText.color = isStart ? new Color(1f, 0.86f, 0.28f, 1f) : Color.white;
            }
        }

        private void CreateLabel(string displayName)
        {
            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, 44f);
            var text = textObject.AddComponent<Text>();
            text.text = displayName;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 18;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private void CreatePreview()
        {
            var previewObject = new GameObject("Preview", typeof(RectTransform));
            previewObject.transform.SetParent(transform, false);
            var previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0f, 0f);
            previewRect.anchorMax = new Vector2(1f, 1f);
            previewRect.offsetMin = new Vector2(10f, 50f);
            previewRect.offsetMax = new Vector2(-10f, -10f);

            var preview = previewObject.AddComponent<Image>();
            preview.sprite = UserMapStore.LoadPreviewSprite(MapPath);
            preview.color = preview.sprite == null ? new Color(0.16f, 0.15f, 0.13f, 1f) : Color.white;
            preview.preserveAspect = true;
            preview.raycastTarget = false;
        }

        private void CreateStartButton()
        {
            var buttonObject = new GameObject("Start Button", typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -10f);
            rect.sizeDelta = new Vector2(92f, 28f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.09f, 0.04f, 0.92f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => controller.SetStartMap(this));

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            startText = textObject.AddComponent<Text>();
            startText.alignment = TextAnchor.MiddleCenter;
            startText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            startText.fontStyle = FontStyle.Bold;
            startText.fontSize = 13;
            startText.raycastTarget = false;
        }

        private void CreateCutsceneBadge()
        {
            var badgeObject = new GameObject("Cutscene Badge", typeof(RectTransform));
            badgeObject.transform.SetParent(transform, false);
            var rect = badgeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.sizeDelta = new Vector2(104f, 28f);

            var image = badgeObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.18f, 0.28f, 0.94f);
            image.raycastTarget = false;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(badgeObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            cutsceneText = textObject.AddComponent<Text>();
            cutsceneText.alignment = TextAnchor.MiddleCenter;
            cutsceneText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cutsceneText.fontStyle = FontStyle.Bold;
            cutsceneText.fontSize = 13;
            cutsceneText.color = new Color(0.8f, 0.92f, 1f, 1f);
            cutsceneText.raycastTarget = false;
        }

        private void RefreshCutsceneBadge()
        {
            if (cutsceneText == null)
            {
                return;
            }

            cutsceneText.transform.parent.gameObject.SetActive(!string.IsNullOrWhiteSpace(CutscenePath));
            cutsceneText.text = CutsceneType == "video" ? "CUT VIDEO" : "CUT IMAGE";
        }

        private void CreatePin(SavedMapExitPointData exitPoint)
        {
            var pinObject = new GameObject(exitPoint.name, typeof(RectTransform));
            pinObject.transform.SetParent(transform, false);
            var pinRect = pinObject.GetComponent<RectTransform>();
            pinRect.sizeDelta = new Vector2(18f, 18f);
            pinRect.anchoredPosition = PinPosition(exitPoint.position);
            var image = pinObject.AddComponent<Image>();
            image.color = new Color(1f, 0.25f, 0.9f, 1f);
            var button = pinObject.AddComponent<Button>();
            button.targetGraphic = image;
            var pin = pinObject.AddComponent<CampaignExitPin>();
            pin.Initialize(controller, this, exitPoint.id);
            pins.Add(pin);
        }

        private Vector2 PinPosition(Vector3 mapPosition)
        {
            var half = rectTransform.sizeDelta * 0.5f;

            if (Mathf.Abs(mapPosition.x) > Mathf.Abs(mapPosition.y))
            {
                return mapPosition.x >= 0f ? new Vector2(half.x, 0f) : new Vector2(-half.x, 0f);
            }

            return mapPosition.y >= 0f ? new Vector2(0f, half.y) : new Vector2(0f, -half.y);
        }
    }
}
