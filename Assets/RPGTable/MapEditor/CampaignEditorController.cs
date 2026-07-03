using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignEditorController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private RectTransform mapListRoot;
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private InputField descriptionInput;
        [SerializeField] private Image coverPreview;

        private readonly List<CampaignMapNode> nodes = new List<CampaignMapNode>();
        private readonly List<CampaignLinkView> links = new List<CampaignLinkView>();
        private CampaignExitPin selectedPin;
        private string currentCampaignName;
        private string coverImagePath;
        private string startMapId;

        public void Initialize(RectTransform listRoot, RectTransform campaignBoardRoot)
        {
            mapListRoot = listRoot;
            boardRoot = campaignBoardRoot;
        }

        public void Initialize(RectTransform listRoot, RectTransform campaignBoardRoot, InputField campaignDescriptionInput, Image campaignCoverPreview)
        {
            mapListRoot = listRoot;
            boardRoot = campaignBoardRoot;
            descriptionInput = campaignDescriptionInput;
            coverPreview = campaignCoverPreview;
        }

        private void Start()
        {
            ReloadMapList();
        }

        public void BackToMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            Debug.LogWarning($"Scene '{mainMenuSceneName}' is not in Build Settings yet.");
        }

        public void RequestSaveCampaign()
        {
            MapEditorMapDialog.ShowSave(currentCampaignName, SaveCampaign, "Save Campaign");
        }

        public void RequestOpenCampaign()
        {
            CampaignEditorDialog.ShowOpenCampaign(UserCampaignStore.GetCampaignPaths(), OpenCampaign);
        }

        public void RequestImportCover()
        {
            var path = UserCampaignStore.ImportCoverImageWithDialog();

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            coverImagePath = path;
            RefreshCoverPreview();
        }

        public void SelectPin(CampaignExitPin pin)
        {
            if (selectedPin == null)
            {
                selectedPin = pin;
                pin.SetSelected(true);
                return;
            }

            if (selectedPin == pin)
            {
                selectedPin.SetSelected(false);
                selectedPin = null;
                return;
            }

            if (selectedPin.Owner != pin.Owner)
            {
                CreateLink(selectedPin, pin);
            }

            selectedPin.SetSelected(false);
            selectedPin = null;
        }

        private void Update()
        {
            foreach (var link in links)
            {
                link.Refresh();
            }
        }

        private void ReloadMapList()
        {
            if (mapListRoot == null)
            {
                return;
            }

            for (var i = mapListRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(mapListRoot.GetChild(i).gameObject);
            }

            var mapPaths = UserMapStore.GetMapPaths();

            if (mapPaths.Count == 0)
            {
                CreateListLabel("No saved maps");
                return;
            }

            foreach (var path in mapPaths)
            {
                CreateMapListItem(path);
            }
        }

        private void AddMapNode(string mapPath, Vector2 boardPosition, string existingId)
        {
            var mapData = UserMapStore.LoadMap(mapPath);

            if (mapData == null || boardRoot == null)
            {
                return;
            }

            var nodeObject = new GameObject(mapData.name, typeof(RectTransform));
            nodeObject.transform.SetParent(boardRoot, false);

            var nodeRect = nodeObject.GetComponent<RectTransform>();
            nodeRect.sizeDelta = new Vector2(240f, 180f);
            nodeRect.anchoredPosition = string.IsNullOrWhiteSpace(existingId)
                ? new Vector2(80f + nodes.Count * 28f, -80f - nodes.Count * 28f)
                : boardPosition;

            nodeObject.AddComponent<Image>().color = new Color(0.075f, 0.068f, 0.058f, 0.98f);

            var node = nodeObject.AddComponent<CampaignMapNode>();
            var nodeId = string.IsNullOrWhiteSpace(existingId) ? Guid.NewGuid().ToString("N") : existingId;

            if (string.IsNullOrWhiteSpace(startMapId))
            {
                startMapId = nodeId;
            }

            node.Initialize(
                this,
                nodeId,
                mapPath,
                mapData.name,
                mapData.exitPoints,
                startMapId == nodeId);

            nodes.Add(node);
            RefreshStartMapVisuals();
        }

        public void RemoveMapNode(CampaignMapNode node)
        {
            if (node == null)
            {
                return;
            }

            if (selectedPin != null && selectedPin.Owner == node)
            {
                selectedPin.SetSelected(false);
                selectedPin = null;
            }

            for (var i = links.Count - 1; i >= 0; i--)
            {
                if (!links[i].Involves(node))
                {
                    continue;
                }

                var linkObject = links[i].gameObject;
                links.RemoveAt(i);
                Destroy(linkObject);
            }

            nodes.Remove(node);

            if (startMapId == node.Id)
            {
                startMapId = nodes.Count > 0 ? nodes[0].Id : null;
                RefreshStartMapVisuals();
            }

            Destroy(node.gameObject);
        }

        public void SetStartMap(CampaignMapNode node)
        {
            if (node == null)
            {
                return;
            }

            startMapId = node.Id;
            RefreshStartMapVisuals();
        }

        private void CreateLink(CampaignExitPin fromPin, CampaignExitPin toPin)
        {
            foreach (var link in links)
            {
                if (link.Matches(fromPin, toPin))
                {
                    return;
                }
            }

            var lineObject = new GameObject("Campaign Link", typeof(RectTransform));
            lineObject.transform.SetParent(boardRoot, false);
            lineObject.transform.SetAsFirstSibling();

            var image = lineObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.7f, 1f, 0.85f);

            var linkView = lineObject.AddComponent<CampaignLinkView>();
            linkView.Initialize(fromPin, toPin);
            links.Add(linkView);
        }

        private void SaveCampaign(string campaignName)
        {
            var data = new SavedCampaignData
            {
                description = descriptionInput == null ? string.Empty : descriptionInput.text,
                coverImagePath = coverImagePath,
                startMapId = startMapId,
                maps = new SavedCampaignMapNodeData[nodes.Count],
                links = new SavedCampaignLinkData[links.Count]
            };

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                data.maps[i] = new SavedCampaignMapNodeData
                {
                    id = node.Id,
                    mapPath = node.MapPath,
                    boardPosition = node.RectTransform.anchoredPosition
                };
            }

            for (var i = 0; i < links.Count; i++)
            {
                data.links[i] = links[i].ToData();
            }

            if (!string.IsNullOrWhiteSpace(UserCampaignStore.SaveCampaign(campaignName, data)))
            {
                currentCampaignName = campaignName.Trim();
            }
        }

        private void OpenCampaign(string path)
        {
            var data = UserCampaignStore.LoadCampaign(path);

            if (data == null)
            {
                return;
            }

            ClearBoard();
            currentCampaignName = data.name;
            coverImagePath = data.coverImagePath;
            startMapId = data.startMapId;
            if (descriptionInput != null)
            {
                descriptionInput.text = data.description ?? string.Empty;
            }

            RefreshCoverPreview();

            if (data.maps != null)
            {
                foreach (var map in data.maps)
                {
                    AddMapNode(map.mapPath, map.boardPosition, map.id);
                }
            }

            if (data.links == null)
            {
                return;
            }

            foreach (var link in data.links)
            {
                var fromPin = FindPin(link.fromMapId, link.fromExitId);
                var toPin = FindPin(link.toMapId, link.toExitId);

                if (fromPin != null && toPin != null)
                {
                    CreateLink(fromPin, toPin);
                }
            }
        }

        private CampaignExitPin FindPin(string mapId, string exitId)
        {
            foreach (var node in nodes)
            {
                if (node.Id == mapId)
                {
                    return node.FindPin(exitId);
                }
            }

            return null;
        }

        private void ClearBoard()
        {
            nodes.Clear();
            links.Clear();
            selectedPin = null;
            startMapId = null;

            for (var i = boardRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(boardRoot.GetChild(i).gameObject);
            }
        }

        private void RefreshCoverPreview()
        {
            if (coverPreview == null)
            {
                return;
            }

            var sprite = UserCampaignStore.LoadCoverSprite(coverImagePath);
            coverPreview.sprite = sprite;
            coverPreview.color = sprite == null ? new Color(0.15f, 0.14f, 0.12f, 1f) : Color.white;
            coverPreview.preserveAspect = true;
        }

        private void RefreshStartMapVisuals()
        {
            foreach (var node in nodes)
            {
                node.SetStart(node.Id == startMapId);
            }
        }

        private void CreateMapListItem(string mapPath)
        {
            var itemObject = new GameObject(UserMapStore.GetDisplayName(mapPath), typeof(RectTransform));
            itemObject.transform.SetParent(mapListRoot, false);

            var image = itemObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);

            var item = itemObject.AddComponent<CampaignMapListItem>();
            item.Initialize(this, mapPath);

            var previewSprite = UserMapStore.LoadPreviewSprite(mapPath);

            if (previewSprite != null)
            {
                var previewObject = new GameObject("Preview", typeof(RectTransform));
                previewObject.transform.SetParent(itemObject.transform, false);
                var previewRect = previewObject.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0f, 0.5f);
                previewRect.anchorMax = new Vector2(0f, 0.5f);
                previewRect.pivot = new Vector2(0f, 0.5f);
                previewRect.anchoredPosition = new Vector2(8f, 0f);
                previewRect.sizeDelta = new Vector2(56f, 42f);
                var preview = previewObject.AddComponent<Image>();
                preview.sprite = previewSprite;
                preview.preserveAspect = true;
                preview.raycastTarget = false;
            }

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(itemObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(previewSprite == null ? 8f : 72f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            var text = textObject.AddComponent<Text>();
            text.text = UserMapStore.GetDisplayName(mapPath);
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 17;
            text.color = Color.white;
            text.raycastTarget = false;

            var layout = itemObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
        }

        private void CreateListLabel(string label)
        {
            var labelObject = new GameObject(label, typeof(RectTransform));
            labelObject.transform.SetParent(mapListRoot, false);
            var text = labelObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            labelObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }

        public void DropMapOnBoard(string mapPath, PointerEventData eventData)
        {
            if (boardRoot == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                boardRoot,
                eventData.position,
                eventData.pressEventCamera,
                out var localPosition);

            AddMapNode(mapPath, localPosition, null);
        }
    }
}
