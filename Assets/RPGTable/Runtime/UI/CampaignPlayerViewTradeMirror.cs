using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;

namespace RPGTable.Runtime
{
    public class CampaignPlayerViewTradeMirror : MonoBehaviour
    {
        private string targetPlayerId;
        private RectTransform giveContent;
        private RectTransform takeContent;

        public void Initialize(string playerId)
        {
            targetPlayerId = playerId;

            var player = CampaignGameSession.FindPlayer(playerId);
            var charName = player != null ? player.name : "Игрок";

            // Centered Main Background Panel
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 450f);
            rect.anchoredPosition = Vector2.zero;

            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.83f, 0.68f, 0.35f, 1f); // Gold outline
            outline.effectDistance = new Vector2(2f, 2f);

            // Title Header
            var headerGo = CampaignGameUI.CreatePanel("Header", transform, new Color(0.14f, 0.12f, 0.1f, 1f));
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 50f);
            headerRt.anchoredPosition = Vector2.zero;

            var headerLabel = CampaignGameUI.CreateLabel($"ОБМЕН С ГЕЙМ-МАСТЕРОМ: {charName.ToUpper()}", headerGo.transform, 18, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            headerLabel.alignment = TextAnchor.MiddleCenter;
            headerLabel.color = new Color(0.83f, 0.68f, 0.35f, 1f);

            // Columns Layout Parent
            var columnsGo = new GameObject("Columns", typeof(RectTransform));
            columnsGo.transform.SetParent(transform, false);
            var colRt = columnsGo.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0f, 0f);
            colRt.anchorMax = new Vector2(1f, 1f);
            colRt.offsetMin = new Vector2(20f, 20f);
            colRt.offsetMax = new Vector2(-20f, -70f);

            var layout = columnsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            // Column 1: Offered Items
            var giveCol = CreateColumn("ВЫ ПОЛУЧИТЕ:", columnsGo.transform);
            CreateListContent("GiveList", giveCol.transform, out giveContent);

            // Column 2: Removed Items
            var takeCol = CreateColumn("У ВАС ЗАБЕРУТ:", columnsGo.transform);
            CreateListContent("TakeList", takeCol.transform, out takeContent);
        }

        private GameObject CreateColumn(string title, Transform parent)
        {
            var col = CampaignGameUI.CreatePanel(title, parent, new Color(0.12f, 0.1f, 0.08f, 1f));
            var outline = col.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.25f, 0.2f, 0.5f);
            outline.effectDistance = new Vector2(1f, 1f);

            var layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var colHeader = new GameObject("ColHeader", typeof(RectTransform));
            colHeader.transform.SetParent(col.transform, false);
            colHeader.AddComponent<LayoutElement>().preferredHeight = 28f;
            
            var label = CampaignGameUI.CreateLabel(title, colHeader.transform, 14, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.83f, 0.68f, 0.35f, 1f);

            return col;
        }

        private void CreateListContent(string name, Transform parent, out RectTransform contentRt)
        {
            var listObject = new GameObject(name, typeof(RectTransform));
            listObject.transform.SetParent(parent, false);
            listObject.AddComponent<LayoutElement>().preferredHeight = 280f;
            contentRt = listObject.GetComponent<RectTransform>();

            var layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        public void Refresh(List<string> offeredItems, HashSet<int> markedForRemovalIndices)
        {
            ClearChildren(giveContent);
            ClearChildren(takeContent);

            // Populate offered items
            if (offeredItems.Count == 0)
            {
                CreateRow("Нет предметов для передачи", giveContent, Color.gray);
            }
            else
            {
                foreach (var item in offeredItems)
                {
                    CreateRow(item, giveContent, new Color(0.12f, 0.35f, 0.12f, 1f));
                }
            }

            // Populate removed items
            var player = CampaignGameSession.FindPlayer(targetPlayerId);
            if (player != null && player.characterRuntimeData != null)
            {
                var backpack = player.characterRuntimeData.backpackSlots;
                bool hasRemovals = false;
                foreach (var idx in markedForRemovalIndices)
                {
                    if (backpack != null && idx >= 0 && idx < backpack.Length && !string.IsNullOrEmpty(backpack[idx]))
                    {
                        CreateRow(backpack[idx], takeContent, new Color(0.4f, 0.12f, 0.12f, 1f));
                        hasRemovals = true;
                    }
                }

                if (!hasRemovals)
                {
                    CreateRow("Нет предметов для изъятия", takeContent, Color.gray);
                }
            }
        }

        private void CreateRow(string text, Transform parent, Color color)
        {
            var rowGo = CampaignGameUI.CreatePanel(text, parent, color);
            rowGo.AddComponent<LayoutElement>().preferredHeight = 36f;
            
            var label = CampaignGameUI.CreateLabel(text, rowGo.transform, 12, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            foreach (Transform child in t)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
