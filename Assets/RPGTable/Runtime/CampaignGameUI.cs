using System;
using System.Collections.Generic;
using RPGTable.MapEditor;
using RPGTable.TokenEditor;
using RPGTable.Core;
using RPGTable.Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.Runtime
{
    /// <summary>
    /// Builds and refreshes the Game Master UI using Unity Prefabs: token bank, active token lists with HP,
    /// campaign map panel with previews, scenario panel, entity inspector, view/toolset, 
    /// and transition prompt. Exposes static input / UI helper methods.
    /// </summary>
    internal sealed class CampaignGameUI
    {
        private RectTransform topMapsRoot;
        private RectTransform leftPanelRoot;
        private RectTransform rightPanelRoot;
        private RectTransform rightInspectorRoot;
        private RectTransform bottomToolsetRoot;

        private Text promptText;
        private GameObject promptPanel;
        private GameObject playerViewControlButton;
        private GMBottomToolsView bottomToolsView;
        private Button combatToggleButton;
        private Text combatToggleText;

        private enum LeftTab { ActiveTokens, TokenBank }
        private LeftTab currentLeftTab = LeftTab.ActiveTokens;

        private Action<string> onSwitchMapCallback;
        private Action<CampaignPlayerData> onSelectPlayerCallback;
        private Action<string> onSelectBankTokenCallback;
        private Action onPromptConfirmCallback;
        private Action onPromptCancelCallback;

        private static Sprite defaultUiSprite;
        private CampaignRuntimeToken selectedToken;

        public bool IsPromptVisible => promptPanel != null && promptPanel.activeSelf;

        private CampaignUIManager uiManager;

        public void BuildUi(
            Action onPromptConfirm,
            Action onPromptCancel,
            Action onTogglePVCamera,
            Action<string> onBankTokenSelected)
        {
            onSelectBankTokenCallback = onBankTokenSelected;
            onPromptConfirmCallback = onPromptConfirm;
            onPromptCancelCallback = onPromptCancel;

            uiManager = UnityEngine.Object.FindAnyObjectByType<CampaignUIManager>();
            if (uiManager != null)
            {
                topMapsRoot = uiManager.TopMapsRoot;
                leftPanelRoot = uiManager.LeftPanelRoot;
                bottomToolsetRoot = uiManager.BottomToolsetRoot;
                rightPanelRoot = uiManager.RightScenarioRoot;
                rightInspectorRoot = uiManager.RightInspectorRoot;
                if (rightInspectorRoot == null)
                {
                    rightInspectorRoot = CreateRuntimeInspectorRoot(uiManager.transform);
                }
                
                var bottomToolsView = bottomToolsetRoot.GetComponentInChildren<GMBottomToolsView>();
                if (bottomToolsView != null)
                {
                    playerViewControlButton = bottomToolsView.playerViewCameraButton.gameObject;
                    CreateCombatToggleButton(bottomToolsView);
                }

                uiManager.Initialize(onPromptConfirm, onPromptCancel, onTogglePVCamera, onBankTokenSelected);
                
                if (uiManager.ActiveTabBtn != null) uiManager.ActiveTabBtn.onClick.AddListener(() => SwitchLeftTab(LeftTab.ActiveTokens));
                if (uiManager.BankTabBtn != null) uiManager.BankTabBtn.onClick.AddListener(() => SwitchLeftTab(LeftTab.TokenBank));
                
                RefreshLeftPanel();
            }
            else
            {
                Debug.LogError("CampaignUIManager is missing in the scene. Please ensure the UI is baked or placed via prefab.");
            }
        }

        private void SwitchLeftTab(LeftTab tab)
        {
            currentLeftTab = tab;
            RefreshLeftPanel();
        }

        private static RectTransform CreateRuntimeInspectorRoot(Transform parent)
        {
            var root = new GameObject("Right Inspector Root", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 300f);
            rect.anchoredPosition = new Vector2(-12f, 0f);

            return rect;
        }

        public void RefreshActiveTokensPanel()
        {
            if (leftPanelRoot == null) return;
            if (currentLeftTab == LeftTab.ActiveTokens)
            {
                RefreshLeftPanel();
            }
        }

        private void RefreshLeftPanel()
        {
            if (leftPanelRoot == null) return;
            ClearChildren(leftPanelRoot);

            var uiManager = GameObject.FindAnyObjectByType<CampaignUIManager>();
            if (uiManager != null)
            {
                if (uiManager.ActiveTabBtn != null)
                {
                    var activeImg = uiManager.ActiveTabBtn.GetComponent<Image>();
                    if (activeImg != null) activeImg.color = currentLeftTab == LeftTab.ActiveTokens ? new Color(0.24f, 0.14f, 0.045f, 1f) : new Color(0.12f, 0.12f, 0.12f, 1f);
                }
                
                if (uiManager.BankTabBtn != null)
                {
                    var bankImg = uiManager.BankTabBtn.GetComponent<Image>();
                    if (bankImg != null) bankImg.color = currentLeftTab == LeftTab.TokenBank ? new Color(0.24f, 0.14f, 0.045f, 1f) : new Color(0.12f, 0.12f, 0.12f, 1f);
                }

                if (currentLeftTab == LeftTab.ActiveTokens)
                {
                    if (uiManager.TabTitleLabel != null) uiManager.TabTitleLabel.text = "ИГРОКИ";
                    BuildActiveTokensList();
                }
                else
                {
                    if (uiManager.TabTitleLabel != null) uiManager.TabTitleLabel.text = "БАНК";
                    BuildTokenBank(leftPanelRoot, onSelectBankTokenCallback);
                }
            }
        }

        private void BuildActiveTokensList()
        {
            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            if (loader == null || loader.Context == null || loader.Context.TokenRoot == null)
            {
                return;
            }

            var activeTokens = loader.Context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>();

            var players = new List<CampaignRuntimeToken>();
            var allies = new List<CampaignRuntimeToken>();
            var enemies = new List<CampaignRuntimeToken>();

            foreach (var token in activeTokens)
            {
                if (token.Team == TokenTeam.Player)
                {
                    players.Add(token);
                }
                else if (token.Team == TokenTeam.Ally || token.Team == TokenTeam.Neutral)
                {
                    allies.Add(token);
                }
                else if (token.Team == TokenTeam.Enemy)
                {
                    enemies.Add(token);
                }
            }

            var cardPrefab = Resources.Load<GameObject>("Prefabs/TokenCard");

            if (players.Count > 0)
            {
                CreateCategoryHeader("ИГРОКИ", new Color(0.1f, 0.25f, 0.45f, 1f));
                foreach (var p in players)
                {
                    CreateActiveTokenCard(p, cardPrefab);
                }
            }

            if (allies.Count > 0)
            {
                CreateCategoryHeader("СОЮЗНИКИ", new Color(0.12f, 0.4f, 0.12f, 1f));
                foreach (var a in allies)
                {
                    CreateActiveTokenCard(a, cardPrefab);
                }
            }

            if (enemies.Count > 0)
            {
                CreateCategoryHeader("ВРАГИ", new Color(0.5f, 0.12f, 0.12f, 1f));
                foreach (var e in enemies)
                {
                    CreateActiveTokenCard(e, cardPrefab);
                }
            }

            RefreshInitiativeList(activeTokens);
        }

        private void CreateCategoryHeader(string text, Color color)
        {
            var header = CreatePanel("Header", leftPanelRoot, color);
            header.AddComponent<LayoutElement>().preferredHeight = 24f;
            var label = CreateLabel(text, header.transform, 11, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            label.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateActiveTokenCard(CampaignRuntimeToken runtimeToken, GameObject cardPrefab)
        {
            if (cardPrefab == null || uiManager == null || leftPanelRoot == null) return;
            
            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            
            var cardGo = UnityEngine.Object.Instantiate(cardPrefab, leftPanelRoot);
            var cardView = cardGo.GetComponent<TokenCardView>();
            if (cardView != null)
            {
                var isSelected = loader != null && loader.Context != null && loader.Context.SelectedToken == runtimeToken;
                
                Sprite portraitSprite = null;
                if (!string.IsNullOrEmpty(runtimeToken.CharacterPath))
                {
                    var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(runtimeToken.CharacterPath);
                    if (charData != null) portraitSprite = RPGTable.CharacterEditor.UserCharacterStore.LoadSprite(charData.portraitPath);
                }
                
                if (portraitSprite == null)
                {
                    var tokenData = UserTokenStore.LoadToken(runtimeToken.TokenPath);
                    if (tokenData != null)
                    {
                        portraitSprite = UserTokenStore.LoadSprite(tokenData.portraitPath);
                    }
                }
                
                cardView.Setup(runtimeToken.DisplayName, portraitSprite, runtimeToken.CurrentHp, runtimeToken.MaxHp, runtimeToken.IsDead, () => {
                    if (loader != null) loader.SelectRuntimeToken(runtimeToken);
                });

                if (isSelected)
                {
                    var img = cardGo.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.24f, 0.16f, 0.08f, 0.95f);
                }
            }
        }

        private static void BuildTokenBank(RectTransform tokenList, Action<string> onSelect)
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<CampaignUIManager>();
            if (manager == null || manager.TokenBankItemPrefab == null) return;

            foreach (var charPath in RPGTable.CharacterEditor.UserCharacterStore.GetCharacterPaths())
            {
                var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(charPath);
                if (charData == null) continue;

                var displayName = charData.name;
                var portraitSprite = RPGTable.CharacterEditor.UserCharacterStore.LoadSprite(charData.portraitPath);
                
                var itemGo = UnityEngine.Object.Instantiate(manager.TokenBankItemPrefab, tokenList);
                var cardView = itemGo.GetComponent<TokenCardView>();
                if (cardView != null)
                {
                    cardView.Setup(displayName, portraitSprite, charData.maxHp, charData.maxHp, false, () => onSelect?.Invoke(charPath));
                }
                else
                {
                    // Fallback for simple buttons
                    var btn = itemGo.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => onSelect?.Invoke(charPath));
                    }
                    var text = itemGo.GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.text = displayName;
                    }
                }
            }
        }

        public void RefreshPlayerPanel(
            IReadOnlyList<CampaignPlayerData> players,
            string currentMapNodeId,
            string selectedPlayerId,
            Action<CampaignPlayerData> onSelect)
        {
            onSelectPlayerCallback = onSelect;
            RefreshActiveTokensPanel();
        }

        public void RefreshMapPanel(
            SavedCampaignData campaign,
            string currentMapNodeId,
            Func<string, SavedMapData> getMap,
            Action<string> onSwitch)
        {
            if (topMapsRoot == null || campaign?.maps == null)
            {
                return;
            }

            onSwitchMapCallback = onSwitch;
            ClearChildren(topMapsRoot);

            var mapCardPrefab = Resources.Load<GameObject>("Prefabs/MapCard");

            foreach (var node in campaign.maps)
            {
                var map = getMap(node.mapPath);
                var rawTitle = map == null ? "Карта" : map.name;
                var title = FormatMapName(rawTitle);

                var isCurrent = node.id == currentMapNodeId;

                if (uiManager != null && uiManager.MapCardPrefab != null)
                {
                    var cardGo = UnityEngine.Object.Instantiate(uiManager.MapCardPrefab, topMapsRoot);
                    var cardView = cardGo.GetComponent<MapCardView>();
                    if (cardView != null)
                    {
                        var previewSprite = UserMapStore.LoadPreviewSprite(node.mapPath);
                        var id = node.id;
                        cardView.Setup(title, previewSprite, isCurrent, () => onSwitch?.Invoke(id));
                    }
                }
            }
        }

        private string FormatMapName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Карта";
            var formatted = rawName.Replace("_", " ");
            if (formatted.Length > 15)
            {
                formatted = formatted.Substring(0, 13) + "..";
            }
            return formatted;
        }

               public void RefreshEntityInspector(CampaignRuntimeToken token)
        {
            selectedToken = token;
            if (rightInspectorRoot == null) return;
            if (token == null)
            {
                rightInspectorRoot.gameObject.SetActive(false);
                return;
            }

            rightInspectorRoot.gameObject.SetActive(true);
            ClearChildren(rightInspectorRoot);

            var inspectorPrefab = uiManager != null && uiManager.InspectorContentPrefab != null
                ? uiManager.InspectorContentPrefab
                : Resources.Load<GameObject>("Prefabs/CombatInspector");

            if (inspectorPrefab != null)
            {
                var inspectorGo = UnityEngine.Object.Instantiate(inspectorPrefab, rightInspectorRoot);
                var inspectorView = inspectorGo.GetComponent<EntityInspectorView>();
                if (inspectorView != null)
                {
                    var tokenData = UserTokenStore.LoadToken(token.TokenPath);
                    var charData = string.IsNullOrEmpty(token.CharacterPath) ? null : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(token.CharacterPath);

                    Sprite portrait = null;
                    if (charData != null) portrait = RPGTable.CharacterEditor.UserCharacterStore.LoadSprite(charData.portraitPath);
                    if (portrait == null && tokenData != null) portrait = UserTokenStore.LoadSprite(tokenData.portraitPath);
                    
                    inspectorView.Setup(token, tokenData, charData, portrait, 
                        damage => ApplyDamage(token, damage), 
                        heal => ApplyHeal(token, heal));
                }
            }
        }

        private void ApplyDamage(CampaignRuntimeToken token, int amount)
        {
            if (token == null) return;

            if (!CampaignGameSession.IsCombatActive && amount > 0)
            {
                CombatManager.Instance.StartCombat();
                var combatBtn = GameObject.Find("Combat Toggle Button");
                if (combatBtn != null)
                {
                    var btn = combatBtn.GetComponent<Button>();
                    var text = combatBtn.GetComponentInChildren<Text>();
                    UpdateCombatButtonState();
                }
            }

            token.CurrentHp = Mathf.Max(0, token.CurrentHp - amount);

            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            if (token.CurrentHp <= 0 && !token.IsDead)
            {
                if (loader != null)
                {
                    loader.KillRuntimeToken(token);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(token.PlayerId))
                {
                    var player = CampaignGameSession.FindPlayer(token.PlayerId);
                    if (player != null)
                    {
                        player.currentHp = token.CurrentHp;
                    }
                }
                RefreshEntityInspector(token);
                RefreshActiveTokensPanel();
            }
        }

        private void ApplyHeal(CampaignRuntimeToken token, int amount)
        {
            if (token == null) return;
            token.CurrentHp = Mathf.Clamp(token.CurrentHp + amount, 0, token.MaxHp);

            if (token.IsDead && token.CurrentHp > 0)
            {
                var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
                if (loader != null)
                {
                    loader.ReviveRuntimeToken(token);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(token.PlayerId))
                {
                    var player = CampaignGameSession.FindPlayer(token.PlayerId);
                    if (player != null)
                    {
                        player.currentHp = token.CurrentHp;
                    }
                }
            }

            RefreshEntityInspector(token);
            RefreshActiveTokensPanel();
        }

        // ─── Initiative List ───
        private void RefreshInitiativeList(CampaignRuntimeToken[] tokens)
        {
            if (uiManager == null || uiManager.InitiativeScrollContent == null) return;
            var content = uiManager.InitiativeScrollContent;

            ClearChildren(content);

            var list = new List<CampaignRuntimeToken>(tokens);
            list.Sort((a, b) => {
                if (a.IsDead != b.IsDead) return a.IsDead.CompareTo(b.IsDead);
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            if (uiManager.InitiativeItemPrefab != null)
            {
                foreach (var token in list)
                {
                    var rowGo = UnityEngine.Object.Instantiate(uiManager.InitiativeItemPrefab, content);
                    var rowView = rowGo.GetComponent<InitiativeRowView>();
                    if (rowView != null)
                    {
                        rowView.Setup(token);
                    }
                }
            }
        }

        public void ShowPrompt(string text)
        {
            if (uiManager == null) return;
            
            if (promptPanel == null)
            {
                promptPanel = CreatePanel("PromptPanel", uiManager.transform, new Color(0, 0, 0, 0.8f));
                var rt = promptPanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                
                var dialogWindow = CreatePanel("DialogWindow", promptPanel.transform, new Color(0.1f, 0.15f, 0.2f, 1f));
                var dialogRt = dialogWindow.GetComponent<RectTransform>();
                dialogRt.anchorMin = new Vector2(0.5f, 0.5f);
                dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
                dialogRt.sizeDelta = new Vector2(400, 200);
                dialogRt.anchoredPosition = Vector2.zero;

                promptText = CreateLabel("", dialogRt, 18, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(10, 50), new Vector2(-10, -10));
                promptText.alignment = TextAnchor.MiddleCenter;

                var buttonsLayout = CreatePanel("ButtonsLayout", dialogRt, Color.clear);
                var buttonsRt = buttonsLayout.GetComponent<RectTransform>();
                buttonsRt.anchorMin = new Vector2(0, 0);
                buttonsRt.anchorMax = new Vector2(1, 0);
                buttonsRt.offsetMin = new Vector2(20, 10);
                buttonsRt.offsetMax = new Vector2(-20, 60);

                var hl = buttonsLayout.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.MiddleCenter;
                hl.childControlWidth = true;
                hl.childControlHeight = true;
                hl.spacing = 20;

                var yesBtn = CreateButton("Да", buttonsLayout.transform, new Color(0.2f, 0.5f, 0.2f, 1f));
                yesBtn.GetComponent<Button>().onClick.AddListener(() => {
                    onPromptConfirmCallback?.Invoke();
                    HidePrompt();
                });

                var noBtn = CreateButton("Нет", buttonsLayout.transform, new Color(0.5f, 0.2f, 0.2f, 1f));
                noBtn.GetComponent<Button>().onClick.AddListener(() => {
                    onPromptCancelCallback?.Invoke();
                    HidePrompt();
                });
            }

            promptPanel.SetActive(true);
            promptPanel.transform.SetAsLastSibling();
            if (promptText != null)
            {
                promptText.text = text;
            }
        }

        public void HidePrompt()
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }
        }

        public void UpdatePlayerViewControlButton(bool active)
        {
            if (bottomToolsView != null)
            {
                bottomToolsView.SetPlayerViewCameraStatus(active);
            }
            else if (playerViewControlButton != null)
            {
                playerViewControlButton.GetComponent<Image>().color = active
                    ? new Color(0.24f, 0.14f, 0.045f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }

        internal static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        internal static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        internal static bool PrimaryMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        internal static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector3.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        internal static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return 0f;
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
#else
            return UnityEngine.Input.mouseScrollDelta.y;
#endif
        }

        internal static void EnsureEventSystem()
        {
            var eventSystemObject = EventSystem.current == null ? null : EventSystem.current.gameObject;

            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            foreach (var legacyModule in eventSystemObject.GetComponents<StandaloneInputModule>())
            {
                UnityEngine.Object.Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        // ── Temporary Static UI helpers for Player View ────────────────────
        // TODO: Remove once Player View is migrated to prefabs

        internal static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        internal static GameObject CreateButton(string label, Transform parent, Color color)
        {
            var buttonObject = CreatePanel(label, parent, color);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            buttonObject.AddComponent<LayoutElement>();
            CreateLabel(label, buttonObject.transform, 16, FontStyle.Bold,
                Vector2.zero, Vector2.one,
                new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return buttonObject;
        }

        internal static Text CreateLabel(string label, Transform parent, int fontSize, FontStyle style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        internal static RectTransform CreateVerticalList(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float spacing)
        {
            var rect = CreateListRoot(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static RectTransform CreateListRoot(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var listObject = new GameObject(name, typeof(RectTransform));
            listObject.transform.SetParent(parent, false);
            var rect = listObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private GameObject endTurnBtnGo;

        private void CreateCombatToggleButton(GMBottomToolsView bottomTools)
        {
            if (bottomTools == null || bottomTools.playerViewCameraButton == null) return;

            var camBtn = bottomTools.playerViewCameraButton;
            
            // 1. Combat Toggle Button
            var combatBtnGo = UnityEngine.Object.Instantiate(camBtn.gameObject, camBtn.transform.parent, false);
            combatBtnGo.name = "Combat Toggle Button";
            combatBtnGo.transform.SetSiblingIndex(camBtn.transform.GetSiblingIndex() + 1);

            var text = combatBtnGo.GetComponentInChildren<Text>();
            if (text == null)
            {
                var textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(combatBtnGo.transform, false);
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                text = textObj.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontStyle = FontStyle.Bold;
                text.fontSize = 14;
                text.color = Color.white;
            }
            
            var btn = combatBtnGo.GetComponent<Button>();
            combatToggleButton = btn;
            combatToggleText = text;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ToggleCombatActiveState);

            // 2. End Turn Button
            endTurnBtnGo = UnityEngine.Object.Instantiate(camBtn.gameObject, camBtn.transform.parent, false);
            endTurnBtnGo.name = "End Turn Button";
            endTurnBtnGo.transform.SetSiblingIndex(combatBtnGo.transform.GetSiblingIndex() + 1);

            var etText = endTurnBtnGo.GetComponentInChildren<Text>();
            if (etText == null)
            {
                var textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(endTurnBtnGo.transform, false);
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                etText = textObj.AddComponent<Text>();
                etText.alignment = TextAnchor.MiddleCenter;
                etText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                etText.fontStyle = FontStyle.Bold;
                etText.fontSize = 14;
                etText.color = Color.white;
            }
            etText.text = "Конец хода";

            var etImage = endTurnBtnGo.GetComponent<Image>();
            if (etImage != null)
            {
                etImage.color = new Color(0.25f, 0.45f, 0.65f, 1f);
            }

            var etBtn = endTurnBtnGo.GetComponent<Button>();
            etBtn.onClick.RemoveAllListeners();
            etBtn.onClick.AddListener(() => {
                if (CampaignGameSession.IsCombatActive)
                {
                    CombatManager.Instance.EndTokenTurn();
                }
            });

            UpdateCombatButtonState();
        }

        private void ToggleCombatActiveState()
        {
            CampaignGameSession.IsCombatActive = !CampaignGameSession.IsCombatActive;
            
            if (CampaignGameSession.IsCombatActive)
            {
                CombatManager.Instance.StartCombat();
            }
            else
            {
                CombatManager.Instance.EndCombat();
            }

            UpdateCombatButtonState();
        }

        private void UpdateCombatButtonState()
        {
            bool active = CampaignGameSession.IsCombatActive;
            if (combatToggleText != null)
            {
                combatToggleText.text = active ? "Бой: ВКЛ" : "Бой: ВЫКЛ";
            }

            var image = combatToggleButton == null ? null : combatToggleButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = active
                    ? new Color(0.6f, 0.15f, 0.15f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            if (endTurnBtnGo != null)
            {
                endTurnBtnGo.SetActive(active);
            }
        }

        public CampaignRuntimeToken UIInstanceSelectedToken => selectedToken;

        private GameObject gmTurnBar;
        private GameObject pvTurnBar;

        public void RefreshCombatUI()
        {
            bool combat = CampaignGameSession.IsCombatActive;
            UpdateCombatButtonState();

            if (topMapsRoot != null)
            {
                topMapsRoot.gameObject.SetActive(!combat);
            }

            if (combat)
            {
                if (gmTurnBar == null && topMapsRoot != null)
                {
                    gmTurnBar = new GameObject("GM Turn Bar", typeof(RectTransform));
                    gmTurnBar.transform.SetParent(topMapsRoot.parent, false);
                    var rect = gmTurnBar.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.sizeDelta = new Vector2(900f, 85f);
                    rect.anchoredPosition = new Vector2(0f, -5f);
                }

                if (gmTurnBar != null)
                {
                    UpdateTurnBarContent(gmTurnBar.transform);
                }

                var pvCanvas = GameObject.Find("Player View Interface");
                if (pvCanvas != null)
                {
                    if (pvTurnBar == null)
                    {
                        pvTurnBar = new GameObject("Player View Turn Bar", typeof(RectTransform));
                        pvTurnBar.transform.SetParent(pvCanvas.transform, false);
                        var rect = pvTurnBar.GetComponent<RectTransform>();
                        rect.anchorMin = new Vector2(0.5f, 1f);
                        rect.anchorMax = new Vector2(0.5f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        rect.sizeDelta = new Vector2(900f, 85f);
                        rect.anchoredPosition = new Vector2(0f, -5f);
                    }

                    if (pvTurnBar != null)
                    {
                        UpdateTurnBarContent(pvTurnBar.transform);
                    }
                }
            }
            else
            {
                if (gmTurnBar != null)
                {
                    UnityEngine.Object.Destroy(gmTurnBar);
                    gmTurnBar = null;
                }
                if (pvTurnBar != null)
                {
                    UnityEngine.Object.Destroy(pvTurnBar);
                    pvTurnBar = null;
                }
            }
        }

        private void UpdateTurnBarContent(Transform turnBarRoot)
        {
            for (int i = turnBarRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(turnBarRoot.GetChild(i).gameObject);
            }

            var layout = turnBarRoot.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = turnBarRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var combatMgr = CombatManager.Instance;
            foreach (var token in combatMgr.Queue)
            {
                if (token == null || token.IsDead) continue;

                Sprite portrait = null;
                var tokenData = UserTokenStore.LoadToken(token.TokenPath);
                var charData = string.IsNullOrEmpty(token.CharacterPath) ? null : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(token.CharacterPath);
                if (charData != null) portrait = RPGTable.CharacterEditor.UserCharacterStore.LoadSprite(charData.portraitPath);
                if (portrait == null && tokenData != null) portrait = UserTokenStore.LoadSprite(tokenData.portraitPath);

                var card = new GameObject("TokenTurnCard", typeof(RectTransform));
                card.transform.SetParent(turnBarRoot, false);
                var cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(70f, 70f);

                var bg = card.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

                var portGo = new GameObject("Portrait", typeof(RectTransform));
                portGo.transform.SetParent(card.transform, false);
                var portRect = portGo.GetComponent<RectTransform>();
                portRect.anchorMin = Vector2.zero;
                portRect.anchorMax = Vector2.one;
                portRect.offsetMin = new Vector2(4f, 4f);
                portRect.offsetMax = new Vector2(-4f, -4f);
                var portImg = portGo.AddComponent<Image>();
                portImg.sprite = portrait;
                portImg.preserveAspect = true;

                float hpPercent = (float)token.CurrentHp / token.MaxHp;
                if (hpPercent < 1f)
                {
                    var redGo = new GameObject("RedOverlay", typeof(RectTransform));
                    redGo.transform.SetParent(card.transform, false);
                    var redRect = redGo.GetComponent<RectTransform>();
                    redRect.anchorMin = new Vector2(0f, 0f);
                    redRect.anchorMax = new Vector2(1f, 0f);
                    redRect.pivot = new Vector2(0.5f, 0f);
                    redRect.offsetMin = new Vector2(4f, 4f);
                    redRect.offsetMax = new Vector2(-4f, 4f);
                    redRect.sizeDelta = new Vector2(0f, (1f - hpPercent) * 62f);
                    var redImg = redGo.AddComponent<Image>();
                    redImg.color = new Color(0.85f, 0.15f, 0.15f, 0.5f);
                }

                if (token == combatMgr.ActiveToken)
                {
                    var outline = card.AddComponent<Outline>();
                    outline.effectColor = new Color(0.2f, 0.9f, 0.2f, 1f);
                    outline.effectDistance = new Vector2(3f, -3f);
                }

                var nameGo = new GameObject("Name", typeof(RectTransform));
                nameGo.transform.SetParent(card.transform, false);
                var nameRect = nameGo.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(1f, 0f);
                nameRect.pivot = new Vector2(0.5f, 0f);
                nameRect.anchoredPosition = new Vector2(0f, -12f);
                nameRect.sizeDelta = new Vector2(0f, 16f);
                var nameTxt = nameGo.AddComponent<Text>();
                nameTxt.text = token.DisplayName;
                nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameTxt.fontSize = 9;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.color = Color.white;
                var nameOutline = nameGo.AddComponent<Outline>();
                nameOutline.effectColor = Color.black;
                nameOutline.effectDistance = new Vector2(1f, -1f);
            }

            var roundCard = new GameObject("RoundCard", typeof(RectTransform));
            roundCard.transform.SetParent(turnBarRoot, false);
            var roundRect = roundCard.GetComponent<RectTransform>();
            roundRect.sizeDelta = new Vector2(80f, 70f);

            var roundBg = roundCard.AddComponent<Image>();
            roundBg.color = new Color(0.25f, 0.22f, 0.1f, 1f);

            var roundOutline = roundCard.AddComponent<Outline>();
            roundOutline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
            roundOutline.effectDistance = new Vector2(2f, -2f);

            var roundTextGo = new GameObject("Text", typeof(RectTransform));
            roundTextGo.transform.SetParent(roundCard.transform, false);
            var roundTextRect = roundTextGo.GetComponent<RectTransform>();
            roundTextRect.anchorMin = Vector2.zero;
            roundTextRect.anchorMax = Vector2.one;
            roundTextRect.offsetMin = Vector2.zero;
            roundTextRect.offsetMax = Vector2.zero;
            var roundTxt = roundTextGo.AddComponent<Text>();
            roundTxt.text = $"ХОД {combatMgr.CurrentTurnNumber}";
            roundTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundTxt.fontStyle = FontStyle.Bold;
            roundTxt.fontSize = 12;
            roundTxt.alignment = TextAnchor.MiddleCenter;
            roundTxt.color = Color.white;
        }
    }
}
