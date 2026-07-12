using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;
using RPGTable.CharacterEditor;

namespace RPGTable.Runtime
{
    public class CampaignPlayerViewTradeMirror : MonoBehaviour
    {
        private CampaignRuntimeToken targetToken;
        private string currentPlayerId;

        private RectTransform targetContent;
        private RectTransform backpackContent;
        private Text headerLabel;
        private Text targetTitleLabel;

        public void Initialize(CampaignRuntimeToken targetToken, string currentPlayerId)
        {
            this.targetToken = targetToken;
            this.currentPlayerId = currentPlayerId;

            var player = CampaignGameSession.FindPlayer(currentPlayerId);
            var charName = player != null ? player.name : "Игрок";
            var targetName = targetToken.DisplayName;

            // Centered Main Background Panel
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(750f, 500f);
            rect.anchoredPosition = Vector2.zero;

            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

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

            headerLabel = CampaignGameUI.CreateLabel($"ОБМЕН ПРЕДМЕТАМИ: {targetName.ToUpper()} ➔ {charName.ToUpper()}", headerGo.transform, 16, FontStyle.Bold,
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

            // Column 1: Target Token Inventory
            var targetCol = CreateColumn($"ИНВЕНТАРЬ: {targetName.ToUpper()}", columnsGo.transform);
            targetTitleLabel = targetCol.GetComponentInChildren<Text>();
            CreateListContent("TargetList", targetCol.transform, out targetContent);

            // Column 2: Player Backpack
            var playerCol = CreateColumn($"РЮКЗАК ИГРОКА: {charName.ToUpper()}", columnsGo.transform);
            CreateListContent("PlayerList", playerCol.transform, out backpackContent);

            Refresh();
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
            
            var label = CampaignGameUI.CreateLabel(title, colHeader.transform, 12, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.83f, 0.68f, 0.35f, 1f);

            return col;
        }

        private void CreateListContent(string name, Transform parent, out RectTransform contentRt)
        {
            var listObject = new GameObject(name, typeof(RectTransform));
            listObject.transform.SetParent(parent, false);
            listObject.AddComponent<LayoutElement>().preferredHeight = 340f;
            contentRt = listObject.GetComponent<RectTransform>();

            var layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        public void Refresh()
        {
            ClearChildren(targetContent);
            ClearChildren(backpackContent);

            // 1. Resolve Target Character Data
            SavedCharacterData targetCharData = null;
            if (!string.IsNullOrEmpty(targetToken.PlayerId))
            {
                var targetPlayer = CampaignGameSession.FindPlayer(targetToken.PlayerId);
                targetCharData = targetPlayer?.characterRuntimeData;
            }
            else
            {
                string key = targetToken.RuntimeId;
                if (string.IsNullOrEmpty(key)) key = targetToken.DisplayName;
                if (CampaignTradeWindow.NpcRuntimeCharacterData.ContainsKey(key))
                {
                    targetCharData = CampaignTradeWindow.NpcRuntimeCharacterData[key];
                }
            }

            // Populate Target items
            if (targetCharData == null)
            {
                CreateRow("У этой цели нет инвентаря", targetContent, Color.gray);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    var name = targetCharData.backpackSlots?[i];
                    if (string.IsNullOrEmpty(name))
                    {
                        CreateRow("- Пусто -", targetContent, new Color(0.1f, 0.08f, 0.07f, 0.4f));
                    }
                    else
                    {
                        CreateRow(name, targetContent, new Color(0.2f, 0.16f, 0.14f, 1f));
                    }
                }
            }

            // Populate Player items
            var player = CampaignGameSession.FindPlayer(currentPlayerId);
            if (player != null && player.characterRuntimeData != null)
            {
                var backpack = player.characterRuntimeData.backpackSlots;
                for (int i = 0; i < 8; i++)
                {
                    var name = (backpack != null && i < backpack.Length) ? backpack[i] : "";
                    if (string.IsNullOrEmpty(name))
                    {
                        CreateRow("- Пусто -", backpackContent, new Color(0.1f, 0.08f, 0.07f, 0.4f));
                    }
                    else
                    {
                        CreateRow(name, backpackContent, new Color(0.12f, 0.28f, 0.12f, 1f));
                    }
                }
            }
        }

        private void CreateRow(string text, Transform parent, Color color)
        {
            var rowGo = CampaignGameUI.CreatePanel(text, parent, color);
            rowGo.AddComponent<LayoutElement>().preferredHeight = 32f;
            
            var label = CampaignGameUI.CreateLabel(text, rowGo.transform, 11, FontStyle.Bold,
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
