using System;
using System.Collections.Generic;
using RPGTable.Core;
using RPGTable.MapEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class CampaignGraphWindow : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private RectTransform contentContainer; // Target for nodes and links
        [SerializeField] private ScrollRect scrollRect;

        [Header("Prefabs & Assets")]
        [SerializeField] private GameObject nodePrefab;

        private SavedCampaignData campaignData;
        private string activeNodeId;
        private Action<string> onSwitchMap;
        private Func<string, SavedMapData> getMapData;

        private readonly Dictionary<string, RectTransform> spawnedNodes = new Dictionary<string, RectTransform>();
        private readonly List<GameObject> spawnedLinks = new List<GameObject>();

        public void Populate(
            SavedCampaignData campaign,
            string currentMapId,
            Func<string, SavedMapData> getMap,
            Action<string> onSwitch)
        {
            campaignData = campaign;
            activeNodeId = currentMapId;
            getMapData = getMap;
            onSwitchMap = onSwitch;

            ClearGraph();

            if (campaignData == null)
            {

                return;
            }
            if (campaignData.maps == null)
            {

                return;
            }



            // 1. Spawn Nodes
            foreach (var node in campaignData.maps)
            {
                var map = getMapData(node.mapPath);
                var rawTitle = map == null ? "Карта" : map.name;
                var title = FormatMapName(rawTitle);

                var isCurrent = node.id == activeNodeId;



                var nodeGo = Instantiate(nodePrefab, contentContainer, false);
                nodeGo.transform.localScale = Vector3.one;
                if (nodeGo == null)
                {
                    Debug.LogError("[CampaignGraphWindow] Failed to instantiate nodePrefab!");
                    continue;
                }
                var nodeRect = nodeGo.GetComponent<RectTransform>();
                nodeRect.anchorMin = new Vector2(0f, 1f);
                nodeRect.anchorMax = new Vector2(0f, 1f);
                nodeRect.pivot = new Vector2(0f, 1f);

                // Set node position (CampaignEditor coordinates) and force Z to 0
                nodeRect.anchoredPosition = new Vector2(node.boardPosition.x, node.boardPosition.y);
                nodeRect.localPosition = new Vector3(nodeRect.localPosition.x, nodeRect.localPosition.y, 0f);
                spawnedNodes[node.id] = nodeRect;



                // Setup node visual card
                var card = nodeGo.GetComponent<MapCardView>();
                if (card != null)
                {
                    var previewSprite = UserMapStore.LoadPreviewSprite(node.mapPath);
                    var id = node.id;
                    card.Setup(title, previewSprite, isCurrent, () => onSwitchMap?.Invoke(id));
                }
            }

            // 2. Spawn Links (Edges)
            if (campaignData.links != null)
            {
                foreach (var link in campaignData.links)
                {
                    if (spawnedNodes.TryGetValue(link.fromMapId, out var fromRect) &&
                        spawnedNodes.TryGetValue(link.toMapId, out var toRect))
                    {
                        CreateLinkConnection(fromRect, toRect);
                    }
                }
            }

            FitGraphContainer();
        }

        private void CreateLinkConnection(RectTransform fromNode, RectTransform toNode)
        {
            GameObject linkGo = new GameObject("LinkConnection", typeof(RectTransform), typeof(Image));
            linkGo.transform.SetParent(contentContainer, false);
            linkGo.transform.SetAsFirstSibling(); // Draw links behind nodes
            spawnedLinks.Add(linkGo);

            var img = linkGo.GetComponent<Image>();
            img.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); // Gray connection lines

            var linkRect = linkGo.GetComponent<RectTransform>();
            linkRect.anchorMin = new Vector2(0f, 1f);
            linkRect.anchorMax = new Vector2(0f, 1f);
            linkRect.pivot = new Vector2(0f, 0.5f);
            
            // Calculate start and end position from centers of nodes
            var start = fromNode.anchoredPosition + new Vector2(fromNode.rect.width * 0.5f, -fromNode.rect.height * 0.5f);
            var end = toNode.anchoredPosition + new Vector2(toNode.rect.width * 0.5f, -toNode.rect.height * 0.5f);

            var direction = end - start;
            var distance = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            linkRect.sizeDelta = new Vector2(distance, 4f);
            linkRect.anchoredPosition = start;
            linkRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FitGraphContainer()
        {
            if (spawnedNodes.Count == 0) return;

            // Find bounds of all nodes in original coordinates
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var nodeRect in spawnedNodes.Values)
            {
                var pos = nodeRect.anchoredPosition;
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }

            float margin = 150f;
            float width = (maxX - minX) + margin * 2f;
            float height = (maxY - minY) + margin * 2f;

            contentContainer.sizeDelta = new Vector2(width, height);

            // Shift nodes to fit within top-left space (X positive, Y negative)
            foreach (var pair in spawnedNodes)
            {
                var originalPos = pair.Value.anchoredPosition;
                float xShifted = originalPos.x - minX + margin;
                float yShifted = originalPos.y - maxY - margin;
                pair.Value.anchoredPosition = new Vector2(xShifted, yShifted);
                

            }

            // Re-render links using the newly shifted node positions
            foreach (var link in spawnedLinks)
            {
                if (link != null) Destroy(link);
            }
            spawnedLinks.Clear();

            if (campaignData != null && campaignData.links != null)
            {
                foreach (var link in campaignData.links)
                {
                    if (spawnedNodes.TryGetValue(link.fromMapId, out var fromRect) &&
                        spawnedNodes.TryGetValue(link.toMapId, out var toRect))
                    {
                        CreateLinkConnection(fromRect, toRect);
                    }
                }
            }

            // Focus ScrollRect on the active node
            if (activeNodeId != null && spawnedNodes.TryGetValue(activeNodeId, out var activeRect))
            {
                FocusOnNode(activeRect);
            }
        }

        private void FocusOnNode(RectTransform targetNode)
        {
            // Center the ScrollRect content on the active node
            Canvas.ForceUpdateCanvases();
            var viewport = scrollRect.viewport;
            if (viewport == null) return;

            var containerSize = contentContainer.rect.size;
            var viewportSize = viewport.rect.size;

            var localPos = targetNode.anchoredPosition;

            float denomX = containerSize.x - viewportSize.x;
            float denomY = containerSize.y - viewportSize.y;

            float posX = denomX > 0.01f ? (localPos.x - viewportSize.x * 0.5f) / denomX : 0.5f;
            // Y is negative, so we use absolute value for normalized scrolling from top (1.0) to bottom (0.0)
            float posY = denomY > 0.01f ? 1f - (Mathf.Abs(localPos.y) - viewportSize.y * 0.5f) / denomY : 0.5f;

            if (float.IsNaN(posX) || float.IsInfinity(posX)) posX = 0.5f;
            if (float.IsNaN(posY) || float.IsInfinity(posY)) posY = 0.5f;

            scrollRect.normalizedPosition = new Vector2(Mathf.Clamp01(posX), Mathf.Clamp01(posY));
        }

        private void ClearGraph()
        {
            foreach (var node in spawnedNodes.Values)
            {
                if (node != null) Destroy(node.gameObject);
            }
            spawnedNodes.Clear();

            foreach (var link in spawnedLinks)
            {
                if (link != null) Destroy(link);
            }
            spawnedLinks.Clear();
        }

        private string FormatMapName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Карта";
            var formatted = rawName.Replace("_", " ");
            if (formatted.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                formatted = formatted.Substring(0, formatted.Length - 5);
            }
            return formatted;
        }
    }
}
