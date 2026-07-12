using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;

namespace RPGTable.Runtime
{
    public class CampaignTradeWindow : MonoBehaviour
    {
        private string targetPlayerId;
        private List<string> offeredItems = new List<string>();
        private HashSet<int> markedForRemoval = new HashSet<int>();

        private RectTransform bankContent;
        private RectTransform offerContent;
        private RectTransform backpackContent;
        private Text headerLabel;

        private static CampaignTradeWindow currentInstance;
        private static GameObject mirrorInstance;

        public static void Open(string playerId)
        {
            if (currentInstance != null)
            {
                Destroy(currentInstance.gameObject);
            }
            if (mirrorInstance != null)
            {
                Destroy(mirrorInstance);
            }

            var canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("Could not find GM Canvas to spawn CampaignTradeWindow.");
                return;
            }

            var windowGo = new GameObject("CampaignTradeWindow", typeof(RectTransform));
            windowGo.transform.SetParent(canvas.transform, false);
            currentInstance = windowGo.AddComponent<CampaignTradeWindow>();
            currentInstance.Initialize(playerId);

            // Spawns mirror on Player View (if Display 2 is active)
            SpawnMirror(playerId);
        }

        private static Canvas FindMainCanvas()
        {
            var uiManager = FindAnyObjectByType<CampaignUIManager>();
            if (uiManager != null)
            {
                return uiManager.GetComponentInParent<Canvas>();
            }
            return FindAnyObjectByType<Canvas>();
        }

        private static void SpawnMirror(string playerId)
        {
            if (CampaignPlayerViewManager.Instance == null || CampaignPlayerViewManager.Instance.PlayerViewInterface == null)
            {
                return;
            }

            var canvasTransform = CampaignPlayerViewManager.Instance.PlayerViewInterface.transform;
            mirrorInstance = new GameObject("PlayerViewTradeMirror", typeof(RectTransform));
            mirrorInstance.transform.SetParent(canvasTransform, false);
            var mirror = mirrorInstance.AddComponent<CampaignPlayerViewTradeMirror>();
            mirror.Initialize(playerId);
        }

        public void Initialize(string playerId)
        {
            targetPlayerId = playerId;
            offeredItems.Clear();
            markedForRemoval.Clear();

            var player = CampaignGameSession.FindPlayer(playerId);
            var charName = player != null ? player.name : "Игрок";

            // Main Background Panel
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 650f);
            rect.anchoredPosition = Vector2.zero;

            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.08f, 0.07f, 0.98f);

            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.83f, 0.68f, 0.35f, 1f); // Burnished Gold
            outline.effectDistance = new Vector2(2f, 2f);

            // Title Header
            var headerGo = CampaignGameUI.CreatePanel("Header", transform, new Color(0.18f, 0.14f, 0.12f, 1f));
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 40f);
            headerRt.anchoredPosition = Vector2.zero;

            headerLabel = CampaignGameUI.CreateLabel($"ОБМЕН ИНВЕНТАРЕМ: {charName.ToUpper()}", headerGo.transform, 16, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));
            headerLabel.alignment = TextAnchor.MiddleLeft;

            // Columns Layout Parent
            var columnsGo = new GameObject("Columns", typeof(RectTransform));
            columnsGo.transform.SetParent(transform, false);
            var colRt = columnsGo.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0f, 0f);
            colRt.anchorMax = new Vector2(1f, 1f);
            colRt.offsetMin = new Vector2(15f, 70f); // Bottom offset to leave room for buttons
            colRt.offsetMax = new Vector2(-15f, -55f); // Top offset to leave room for header

            var layout = columnsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            // Column 1: Item Bank
            var bankCol = CreateColumn("БАНК ПРЕДМЕТОВ", columnsGo.transform);
            CreateScrollView("BankScroll", bankCol.transform, out bankContent);

            // Column 2: Offered Items
            var offerCol = CreateColumn("ПРЕДЛОЖЕНИЕ ГМ (ОТДАТЬ)", columnsGo.transform);
            CreateScrollView("OfferScroll", offerCol.transform, out offerContent);

            // Column 3: Player Backpack
            var backpackCol = CreateColumn("РЮКЗАК ПЕРСОНАЖА (ИЗЪЯТЬ)", columnsGo.transform);
            CreateScrollView("BackpackScroll", backpackCol.transform, out backpackContent);

            // Bottom Buttons Panel
            var buttonsPanel = CampaignGameUI.CreatePanel("Buttons", transform, Color.clear);
            var buttonsRt = buttonsPanel.GetComponent<RectTransform>();
            buttonsRt.anchorMin = new Vector2(0f, 0f);
            buttonsRt.anchorMax = new Vector2(1f, 0f);
            buttonsRt.pivot = new Vector2(0.5f, 0f);
            buttonsRt.sizeDelta = new Vector2(0f, 60f);
            buttonsRt.anchoredPosition = new Vector2(0f, 10f);

            var buttonsLayout = buttonsPanel.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20f;
            buttonsLayout.padding = new RectOffset(40, 40, 10, 10);
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childForceExpandHeight = true;
            buttonsLayout.childForceExpandWidth = true;

            var commitBtnGo = CampaignGameUI.CreateButton("ПРИМЕНИТЬ ОБМЕН", buttonsPanel.transform, new Color(0.12f, 0.35f, 0.12f, 1f));
            commitBtnGo.GetComponent<Button>().onClick.AddListener(CommitTrade);

            var cancelBtnGo = CampaignGameUI.CreateButton("ОТМЕНИТЬ", buttonsPanel.transform, new Color(0.35f, 0.12f, 0.12f, 1f));
            cancelBtnGo.GetComponent<Button>().onClick.AddListener(CancelTrade);

            // Populates all columns initially
            RefreshUI();
        }

        private GameObject CreateColumn(string title, Transform parent)
        {
            var col = CampaignGameUI.CreatePanel(title, parent, new Color(0.15f, 0.12f, 0.1f, 1f));
            var outline = col.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.28f, 0.2f, 0.6f);
            outline.effectDistance = new Vector2(1f, 1f);

            var layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var colHeader = new GameObject("ColHeader", typeof(RectTransform));
            colHeader.transform.SetParent(col.transform, false);
            colHeader.AddComponent<LayoutElement>().preferredHeight = 24f;
            
            var label = CampaignGameUI.CreateLabel(title, colHeader.transform, 12, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.83f, 0.68f, 0.35f, 1f);

            return col;
        }

        private void CreateScrollView(string name, Transform parent, out RectTransform contentRt)
        {
            var scrollObject = new GameObject(name, typeof(RectTransform));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.AddComponent<LayoutElement>().preferredHeight = 440f;
            var scrollRt = scrollObject.GetComponent<RectTransform>();
            
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewRt = viewport.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            contentRt.anchoredPosition = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewRt;
            scrollRect.content = contentRt;
        }

        private void RefreshUI()
        {
            // 1. Refresh Item Bank
            ClearChildren(bankContent);
            var itemCards = Resources.LoadAll<ItemCard>("ItemCards");
            foreach (var item in itemCards)
            {
                if (item == null) continue;
                var cardBtn = CreateItemRow(item.title, GetItemStatsText(item), bankContent, new Color(0.2f, 0.16f, 0.12f, 1f));
                cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                    offeredItems.Add(item.title);
                    RefreshUI();
                });
            }

            // 2. Refresh Offered Items
            ClearChildren(offerContent);
            if (offeredItems.Count == 0)
            {
                CreatePlaceholderRow("Предложений нет", offerContent);
            }
            else
            {
                for (int i = 0; i < offeredItems.Count; i++)
                {
                    int index = i;
                    var name = offeredItems[index];
                    var itemCard = FindItemCardStatic(name);
                    var cardBtn = CreateItemRow(name, GetItemStatsText(itemCard), offerContent, new Color(0.12f, 0.24f, 0.12f, 1f));
                    cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                        offeredItems.RemoveAt(index);
                        RefreshUI();
                    });
                }
            }

            // 3. Refresh Player Backpack Slots
            ClearChildren(backpackContent);
            var player = CampaignGameSession.FindPlayer(targetPlayerId);
            if (player != null && player.characterRuntimeData != null)
            {
                var backpack = player.characterRuntimeData.backpackSlots;
                for (int i = 0; i < 8; i++)
                {
                    int slotIndex = i;
                    var name = (backpack != null && slotIndex < backpack.Length) ? backpack[slotIndex] : "";
                    
                    if (string.IsNullOrEmpty(name))
                    {
                        var cardBtn = CreateItemRow($"Слот {slotIndex + 1}: [ПУСТО]", "", backpackContent, new Color(0.1f, 0.08f, 0.07f, 0.4f));
                        cardBtn.GetComponent<Button>().interactable = false;
                    }
                    else
                    {
                        var isMarked = markedForRemoval.Contains(slotIndex);
                        var bgCol = isMarked ? new Color(0.4f, 0.12f, 0.12f, 1f) : new Color(0.22f, 0.18f, 0.15f, 1f);
                        var labelPrefix = isMarked ? "[УДАЛИТЬ] " : "";
                        var itemCard = FindItemCardStatic(name);

                        var cardBtn = CreateItemRow($"{labelPrefix}Слот {slotIndex + 1}: {name}", GetItemStatsText(itemCard), backpackContent, bgCol);
                        cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                            if (markedForRemoval.Contains(slotIndex))
                                markedForRemoval.Remove(slotIndex);
                            else
                                markedForRemoval.Add(slotIndex);
                            RefreshUI();
                        });
                    }
                }
            }

            // Notify mirror on Player View display
            if (mirrorInstance != null)
            {
                var mirror = mirrorInstance.GetComponent<CampaignPlayerViewTradeMirror>();
                if (mirror != null)
                {
                    mirror.Refresh(offeredItems, markedForRemoval);
                }
            }
        }

        private GameObject CreateItemRow(string title, string subtitle, Transform parent, Color color)
        {
            var rowGo = CampaignGameUI.CreatePanel(title, parent, color);
            rowGo.AddComponent<LayoutElement>().preferredHeight = 44f;
            
            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(1f, 1f);

            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = rowGo.GetComponent<Image>();

            // Title label
            var titleLbl = CampaignGameUI.CreateLabel(title, rowGo.transform, 11, FontStyle.Bold,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(-6f, -2f));
            titleLbl.alignment = TextAnchor.LowerLeft;

            // Subtitle stats label
            var subLbl = CampaignGameUI.CreateLabel(subtitle, rowGo.transform, 9, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(6f, 2f), new Vector2(-6f, 0f));
            subLbl.alignment = TextAnchor.UpperLeft;
            subLbl.color = new Color(0.83f, 0.68f, 0.35f, 0.9f);

            return rowGo;
        }

        private void CreatePlaceholderRow(string text, Transform parent)
        {
            var rowGo = CampaignGameUI.CreatePanel("Placeholder", parent, new Color(0, 0, 0, 0.15f));
            rowGo.AddComponent<LayoutElement>().preferredHeight = 36f;
            var label = CampaignGameUI.CreateLabel(text, rowGo.transform, 11, FontStyle.Italic,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.gray;
        }

        private string GetItemStatsText(ItemCard item)
        {
            if (item == null) return "";
            var parts = new List<string>();
            if (item.armorPoints > 0) parts.Add($"+{item.armorPoints} ARM");
            if (item.bonusHp > 0) parts.Add($"+{item.bonusHp} HP");
            if (item.bonusStr > 0) parts.Add($"+{item.bonusStr} STR");
            if (item.bonusAgi > 0) parts.Add($"+{item.bonusAgi} AGI");
            if (item.bonusInt > 0) parts.Add($"+{item.bonusInt} INT");
            if (item.bonusHol > 0) parts.Add($"+{item.bonusHol} HOL");
            
            if (item.itemType == ItemType.Weapon)
            {
                parts.Add(item.attackType.ToString());
                if (item.scaleStat1 != "None") parts.Add($"{item.scaleStat1}x{item.coef1}");
            }
            return string.Join(", ", parts);
        }

        private void CommitTrade()
        {
            var player = CampaignGameSession.FindPlayer(targetPlayerId);
            if (player != null && player.characterRuntimeData != null)
            {
                var data = player.characterRuntimeData;

                // 1. Remove items marked for deletion
                foreach (var idx in markedForRemoval)
                {
                    if (idx >= 0 && idx < data.backpackSlots.Length)
                    {
                        data.backpackSlots[idx] = "";
                    }
                }

                // 2. Add offered items to empty slots
                int offerIdx = 0;
                for (int i = 0; i < data.backpackSlots.Length; i++)
                {
                    if (offerIdx >= offeredItems.Count) break;

                    if (string.IsNullOrEmpty(data.backpackSlots[i]))
                    {
                        data.backpackSlots[i] = offeredItems[offerIdx];
                        offerIdx++;
                    }
                }

                // 3. Recalculate player stats
                var baseData = string.IsNullOrEmpty(player.characterPath) 
                    ? new RPGTable.CharacterEditor.SavedCharacterData() 
                    : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(player.characterPath);

                int baseHp = baseData != null ? baseData.maxHp : 10;
                int baseArmor = baseData != null ? baseData.maxArmor : 0;

                int extraHp = 0;
                int extraArmor = 0;

                string[] equipped = {
                    data.eqHelmet, data.eqArmor, data.eqWeapon, data.eqWeapon2,
                    data.eqShield, data.eqBoots, data.eqAmulet, data.eqRing,
                    data.eqArtifact, data.eqBelt
                };

                foreach (var itemName in equipped)
                {
                    if (string.IsNullOrEmpty(itemName)) continue;
                    var item = FindItemCardStatic(itemName);
                    if (item != null)
                    {
                        extraHp += item.bonusHp;
                        extraArmor += item.armorPoints;
                    }
                }

                player.maxHp = baseHp + extraHp;
                player.currentHp = Mathf.Min(player.currentHp, player.maxHp);
                player.maxArmor = baseArmor + extraArmor;
                player.currentArmor = Mathf.Min(player.currentArmor, player.maxArmor);

                CampaignGameSession.UpdateTokenCombatStats(
                    player.id, player.currentMapId,
                    player.currentHp, player.maxHp,
                    player.currentArmor, player.maxArmor,
                    player.currentMovementPoints, player.maxMovementPoints,
                    player.currentRolls, player.maxRolls,
                    player.activeWeaponIndex, player.rerollCoins,
                    player.statusEffects, player.isDead);

                CampaignGameSession.TriggerPlayersChanged();
            }

            CloseWindow();
        }

        private void CancelTrade()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (mirrorInstance != null)
            {
                Destroy(mirrorInstance);
                mirrorInstance = null;
            }
            Destroy(gameObject);
            currentInstance = null;
        }

        private static void ClearChildren(Transform t)
        {
            foreach (Transform child in t)
            {
                Destroy(child.gameObject);
            }
        }

        private static ItemCard FindItemCardStatic(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var items = Resources.LoadAll<ItemCard>("ItemCards");
            foreach (var item in items)
            {
                if (item != null && string.Equals(item.title, name, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
