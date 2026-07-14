using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;
using RPGTable.CharacterEditor;

namespace RPGTable.Runtime
{
    public class CampaignTradeWindow : MonoBehaviour
    {
        // Session-persistent cache for NPC/Chest inventories so items remain in them during the game
        public static Dictionary<string, SavedCharacterData> NpcRuntimeCharacterData = new Dictionary<string, SavedCharacterData>();

        private string currentPlayerId;
        private CampaignRuntimeToken targetToken;
        private SavedCharacterData targetCharData;

        private RectTransform bankContent;
        private RectTransform targetContent;
        private RectTransform backpackContent;
        private Text headerLabel;

        private static CampaignTradeWindow currentInstance;
        private static GameObject mirrorInstance;

        public static void Open(CampaignRuntimeToken targetToken, string currentPlayerId)
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
            currentInstance.Initialize(targetToken, currentPlayerId);

            SpawnMirror(targetToken, currentPlayerId);
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

        private static void SpawnMirror(CampaignRuntimeToken targetToken, string currentPlayerId)
        {
            if (CampaignPlayerViewManager.Instance == null || CampaignPlayerViewManager.Instance.PlayerViewInterface == null)
            {
                return;
            }

            var canvasTransform = CampaignPlayerViewManager.Instance.PlayerViewInterface.transform;
            mirrorInstance = new GameObject("PlayerViewTradeMirror", typeof(RectTransform));
            mirrorInstance.transform.SetParent(canvasTransform, false);
            var mirror = mirrorInstance.AddComponent<CampaignPlayerViewTradeMirror>();
            mirror.Initialize(targetToken, currentPlayerId);
        }

        public void Initialize(CampaignRuntimeToken targetToken, string currentPlayerId)
        {
            this.targetToken = targetToken;
            this.currentPlayerId = currentPlayerId;

            // Resolve target character data
            if (!string.IsNullOrEmpty(targetToken.PlayerId))
            {
                var targetPlayer = CampaignGameSession.FindPlayer(targetToken.PlayerId);
                targetCharData = targetPlayer?.characterRuntimeData;
            }
            else if (!string.IsNullOrEmpty(targetToken.CharacterPath))
            {
                string key = targetToken.RuntimeId;
                if (string.IsNullOrEmpty(key)) key = targetToken.DisplayName;
                
                if (!NpcRuntimeCharacterData.ContainsKey(key))
                {
                    var loaded = UserCharacterStore.LoadCharacter(targetToken.CharacterPath);
                    if (loaded == null) loaded = new SavedCharacterData();
                    NpcRuntimeCharacterData[key] = loaded;
                }
                targetCharData = NpcRuntimeCharacterData[key];
            }

            // Ensure backpacks are initialized
            var player = CampaignGameSession.FindPlayer(currentPlayerId);
            if (player != null && player.characterRuntimeData != null)
            {
                if (player.characterRuntimeData.backpackSlots == null)
                {
                    player.characterRuntimeData.backpackSlots = new string[8];
                }
            }
            if (targetCharData != null && targetCharData.backpackSlots == null)
            {
                targetCharData.backpackSlots = new string[8];
            }

            var charName = player != null ? player.name : "Игрок";
            var targetName = targetToken.DisplayName;

            // Main Background Panel
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 650f);
            rect.anchoredPosition = Vector2.zero;

            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.08f, 0.07f, 0.98f);

            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.83f, 0.68f, 0.35f, 1f); // Gold outline
            outline.effectDistance = new Vector2(2f, 2f);

            // Title Header
            var headerGo = CampaignGameUI.CreatePanel("Header", transform, new Color(0.18f, 0.14f, 0.12f, 1f));
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 40f);
            headerRt.anchoredPosition = Vector2.zero;

            headerLabel = CampaignGameUI.CreateLabel($"ОБМЕН ИНВЕНТАРЕМ: {targetName.ToUpper()} ➔ {charName.ToUpper()}", headerGo.transform, 14, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));
            headerLabel.alignment = TextAnchor.MiddleLeft;

            // Columns Layout Parent
            var columnsGo = new GameObject("Columns", typeof(RectTransform));
            columnsGo.transform.SetParent(transform, false);
            var colRt = columnsGo.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0f, 0f);
            colRt.anchorMax = new Vector2(1f, 1f);
            colRt.offsetMin = new Vector2(15f, 70f);
            colRt.offsetMax = new Vector2(-15f, -55f);

            var layout = columnsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            // Column 1: Item Bank
            var bankCol = CreateColumn("БАНК ПРЕДМЕТОВ", columnsGo.transform);
            CreateScrollView("BankScroll", bankCol.transform, out bankContent);

            // Column 2: Target Token Backpack
            var targetColName = targetCharData != null ? $"ИНВЕНТАРЬ: {targetName.ToUpper()}" : "ИНВЕНТАРЬ ЦЕЛИ (ПУСТО)";
            var targetCol = CreateColumn(targetColName, columnsGo.transform);
            CreateScrollView("TargetScroll", targetCol.transform, out targetContent);

            // Column 3: Player Backpack
            var backpackCol = CreateColumn($"РЮКЗАК ИГРОКА: {charName.ToUpper()}", columnsGo.transform);
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
            buttonsLayout.padding = new RectOffset(200, 200, 10, 10);
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childForceExpandHeight = true;
            buttonsLayout.childForceExpandWidth = true;

            var closeBtnGo = CampaignGameUI.CreateButton("ЗАКРЫТЬ", buttonsPanel.transform, new Color(0.25f, 0.2f, 0.18f, 1f));
            closeBtnGo.GetComponent<Button>().onClick.AddListener(CloseWindow);

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
            
            var label = CampaignGameUI.CreateLabel(title, colHeader.transform, 11, FontStyle.Bold,
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
            var player = CampaignGameSession.FindPlayer(currentPlayerId);

            // 1. Refresh Item Bank
            ClearChildren(bankContent);
            var itemCards = Resources.LoadAll<ItemCard>("ItemCards");
            foreach (var item in itemCards)
            {
                if (item == null) continue;
                var cardBtn = CreateItemRow(item.title, GetItemStatsText(item), bankContent, new Color(0.2f, 0.16f, 0.12f, 1f), item.icon);
                cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                    if (AddItemToBackpack(player?.characterRuntimeData, item.title))
                    {
                        OnInventoryModified(player);
                        RefreshUI();
                    }
                });
            }

            // 2. Refresh Target Token Inventory
            ClearChildren(targetContent);
            if (targetCharData == null)
            {
                CreatePlaceholderRow("У этой цели нет инвентаря", targetContent);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    int slotIndex = i;
                    var name = targetCharData.backpackSlots[slotIndex];
                    if (string.IsNullOrEmpty(name))
                    {
                        var cardBtn = CreateItemRow($"Слот {slotIndex + 1}: [ПУСТО]", "", targetContent, new Color(0.1f, 0.08f, 0.07f, 0.3f), null);
                        cardBtn.GetComponent<Button>().interactable = false;
                    }
                    else
                    {
                        var itemCard = FindItemCardStatic(name);
                        var cardBtn = CreateItemRow($"Слот {slotIndex + 1}: {name}", GetItemStatsText(itemCard), targetContent, new Color(0.2f, 0.15f, 0.12f, 1f), itemCard != null ? itemCard.icon : null);
                        cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                            // Transfer from Target to Player
                            if (AddItemToBackpack(player?.characterRuntimeData, name))
                            {
                                targetCharData.backpackSlots[slotIndex] = "";
                                OnInventoryModified(player);
                                OnInventoryModified(null); // NPC changed
                                RefreshUI();
                            }
                        });
                    }
                }
            }

            // 3. Refresh Player Backpack
            ClearChildren(backpackContent);
            if (player != null && player.characterRuntimeData != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    int slotIndex = i;
                    var name = player.characterRuntimeData.backpackSlots[slotIndex];
                    if (string.IsNullOrEmpty(name))
                    {
                        var cardBtn = CreateItemRow($"Слот {slotIndex + 1}: [ПУСТО]", "", backpackContent, new Color(0.1f, 0.08f, 0.07f, 0.3f), null);
                        cardBtn.GetComponent<Button>().interactable = false;
                    }
                    else
                    {
                        var itemCard = FindItemCardStatic(name);
                        var bgCol = new Color(0.12f, 0.24f, 0.12f, 1f); // Green row for player items
                        var cardBtn = CreateItemRow($"Слот {slotIndex + 1}: {name}", GetItemStatsText(itemCard), backpackContent, bgCol, itemCard != null ? itemCard.icon : null);
                        
                        cardBtn.GetComponent<Button>().onClick.AddListener(() => {
                            if (targetCharData != null)
                            {
                                // Transfer from Player to Target
                                if (AddItemToBackpack(targetCharData, name))
                                {
                                    player.characterRuntimeData.backpackSlots[slotIndex] = "";
                                    OnInventoryModified(player);
                                    OnInventoryModified(null); // NPC changed
                                    RefreshUI();
                                }
                            }
                            else
                            {
                                // Discard item
                                player.characterRuntimeData.backpackSlots[slotIndex] = "";
                                OnInventoryModified(player);
                                RefreshUI();
                            }
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
                    mirror.Refresh();
                }
            }
        }

        private bool AddItemToBackpack(SavedCharacterData charData, string itemName)
        {
            if (charData == null) return false;
            if (charData.backpackSlots == null) charData.backpackSlots = new string[8];

            for (int i = 0; i < 8; i++)
            {
                if (string.IsNullOrEmpty(charData.backpackSlots[i]))
                {
                    charData.backpackSlots[i] = itemName;
                    return true;
                }
            }
            return false;
        }

        private void OnInventoryModified(CampaignPlayerData player)
        {
            if (player == null) return;

            var runtimeData = player.characterRuntimeData;
            int baseHp = runtimeData != null && runtimeData.maxHp > 0 ? runtimeData.maxHp : 10;
            int baseArmor = runtimeData != null ? runtimeData.maxArmor : 0;

            int extraHp = 0;
            int extraArmor = 0;

            if (runtimeData != null)
            {
                string[] equipped = {
                    runtimeData.eqHelmet, runtimeData.eqArmor,
                    runtimeData.eqWeapon, runtimeData.eqWeapon2,
                    runtimeData.eqShield, runtimeData.eqBoots,
                    runtimeData.eqAmulet, runtimeData.eqRing,
                    runtimeData.eqArtifact, runtimeData.eqBelt
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

        private GameObject CreateItemRow(string title, string subtitle, Transform parent, Color color, Sprite iconSprite)
        {
            var rowGo = CampaignGameUI.CreatePanel(title, parent, color);
            rowGo.AddComponent<LayoutElement>().preferredHeight = 46f;
            
            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(1f, 1f);

            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = rowGo.GetComponent<Image>();

            // 1. Icon Image (Occupies left part of card, full height square)
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rowGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = new Vector2(46f, 0f); // Width = Height = 46px
            
            var img = iconGo.AddComponent<Image>();
            if (iconSprite != null)
            {
                img.sprite = iconSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.12f, 0.1f, 0.08f, 0.8f); // Dark fallback box
            }
            
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 46f;
            iconLayout.preferredHeight = 46f;

            // 2. Text container (Vertical Stack next to the icon)
            var textContainerGo = new GameObject("TextContainer", typeof(RectTransform));
            textContainerGo.transform.SetParent(rowGo.transform, false);
            var tcRt = textContainerGo.GetComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0f, 0f);
            tcRt.anchorMax = new Vector2(1f, 1f);
            tcRt.pivot = new Vector2(0f, 0.5f);
            tcRt.offsetMin = new Vector2(54f, 4f); // 8px space after icon, 4px padding bottom
            tcRt.offsetMax = new Vector2(-6f, -4f); // 6px padding right, 4px padding top

            var tcLayout = textContainerGo.AddComponent<VerticalLayoutGroup>();
            tcLayout.spacing = 1f;
            tcLayout.childControlHeight = true;
            tcLayout.childControlWidth = true;
            tcLayout.childForceExpandHeight = false;
            tcLayout.childForceExpandWidth = true;
            
            var textLayout = textContainerGo.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 160f;

            // Title Label
            var titleLbl = CampaignGameUI.CreateLabel(title, textContainerGo.transform, 11, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleLbl.alignment = TextAnchor.MiddleLeft;

            // Subtitle Label
            var subLbl = CampaignGameUI.CreateLabel(subtitle, textContainerGo.transform, 9, FontStyle.Normal,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            subLbl.alignment = TextAnchor.MiddleLeft;
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
