using System;
using RPGTable.CharacterEditor;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.Runtime
{
    public sealed class GMCharacterWindow : MonoBehaviour
    {
        private static GMCharacterWindow current;

        private CampaignRuntimeToken token;
        private CampaignGameLoader loader;
        private SavedCharacterData characterData;
        private RectTransform contentRoot;
        private InputField xpInput;

        public static void Open(CampaignRuntimeToken targetToken, CampaignGameLoader gameLoader)
        {
            if (targetToken == null)
            {
                return;
            }

            if (current != null)
            {
                Destroy(current.gameObject);
            }

            EnsureEventSystem();

            var canvasObject = new GameObject("GM Character Window Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1700;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            current = canvasObject.AddComponent<GMCharacterWindow>();
            current.Initialize(targetToken, gameLoader);
        }

        private void Initialize(CampaignRuntimeToken targetToken, CampaignGameLoader gameLoader)
        {
            token = targetToken;
            loader = gameLoader != null ? gameLoader : FindAnyObjectByType<CampaignGameLoader>();
            characterData = ResolveCharacterData(token);

            BuildWindow();
            Refresh();
        }

        private static SavedCharacterData ResolveCharacterData(CampaignRuntimeToken targetToken)
        {
            if (targetToken == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(targetToken.PlayerId))
            {
                return CampaignGameSession.FindPlayer(targetToken.PlayerId)?.characterRuntimeData;
            }

            var key = string.IsNullOrWhiteSpace(targetToken.RuntimeId) ? targetToken.DisplayName : targetToken.RuntimeId;
            if (string.IsNullOrWhiteSpace(key))
            {
                key = targetToken.GetEntityId().ToString();
            }

            if (!CampaignTradeWindow.NpcRuntimeCharacterData.TryGetValue(key, out var data))
            {
                data = string.IsNullOrWhiteSpace(targetToken.CharacterPath)
                    ? new SavedCharacterData()
                    : UserCharacterStore.LoadCharacter(targetToken.CharacterPath);

                if (data == null)
                {
                    data = new SavedCharacterData();
                }

                CampaignTradeWindow.NpcRuntimeCharacterData[key] = data;
            }

            return data;
        }

        private void BuildWindow()
        {
            var dimmer = CreatePanel("Dimmer", transform, new Color(0f, 0f, 0f, 0.42f));
            var dimRect = dimmer.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            var panel = CreatePanel("GM Character Window", transform, new Color(0.055f, 0.047f, 0.04f, 0.98f));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 680f);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.58f, 0.22f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var title = CreateLabel("Карточка персонажа", panel.transform, 24, FontStyle.Bold);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(20f, -58f);
            titleRect.offsetMax = new Vector2(-70f, -12f);
            title.alignment = TextAnchor.MiddleLeft;

            var closeButton = CreateButton("X", panel.transform, new Color(0.24f, 0.08f, 0.06f, 1f));
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(42f, 36f);
            closeRect.anchoredPosition = new Vector2(-14f, -14f);
            closeButton.GetComponent<Button>().onClick.AddListener(Close);

            var scrollObject = CreatePanel("Scroll", panel.transform, Color.clear);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(20f, 20f);
            scrollRectTransform.offsetMax = new Vector2(-20f, -70f);

            var scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = CreatePanel("Viewport", scrollObject.transform, new Color(0f, 0f, 0f, 0.16f));
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRoot;
        }

        private void Refresh()
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);

            if (characterData == null)
            {
                AddSection("Нет данных персонажа", "У этой фишки нет связанного персонажа.");
                return;
            }

            AddHeaderSection();
            AddAttributesSection();
            AddEquipmentSection();
            AddInventorySection();
            AddSkillsSection();
            AddActionsSection();
        }

        private void AddHeaderSection()
        {
            var owner = !string.IsNullOrWhiteSpace(token.PlayerId) ? "Игрок" : "NPC";
            AddSection(
                "Основное",
                $"{token.DisplayName}\n" +
                $"Тип: {owner}\n" +
                $"Класс: {Safe(characterData.characterClass)}\n" +
                $"Уровень: {Mathf.Max(1, characterData.level)}   XP: {Mathf.Max(0, characterData.xp)}\n" +
                $"XP до след. уровня: {CampaignGameSession.GetRequiredXpForNextLevel(characterData.level)}\n" +
                $"HP: {token.CurrentHp}/{token.MaxHp}   Броня: {token.CurrentArmor}/{token.MaxArmor}\n" +
                $"Очки характеристик: {Mathf.Max(0, characterData.attributePoints)}   Очки навыков: {Mathf.Max(0, characterData.skillPoints)}");
        }

        private void AddAttributesSection()
        {
            var section = CreateSection("Характеристики");
            AddStatRow(section.transform, "STR", characterData.strength, () => AddAttributePoint("STR"));
            AddStatRow(section.transform, "AGI", characterData.agility, () => AddAttributePoint("AGI"));
            AddStatRow(section.transform, "INT", characterData.intelligence, () => AddAttributePoint("INT"));
            AddStatRow(section.transform, "HOL", characterData.holiness, () => AddAttributePoint("HOL"));
        }

        private void AddStatRow(Transform parent, string statName, int value, Action onPlus)
        {
            var row = CreatePanel(statName, parent, new Color(0.08f, 0.07f, 0.06f, 0.92f));
            row.AddComponent<LayoutElement>().preferredHeight = 34f;

            var label = CreateLabel($"{statName}: {value}", row.transform, 16, FontStyle.Bold);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-54f, 0f);
            label.alignment = TextAnchor.MiddleLeft;

            if (characterData.attributePoints <= 0)
            {
                return;
            }

            var plus = CreateButton("+", row.transform, new Color(0.2f, 0.38f, 0.14f, 1f));
            var plusRect = plus.GetComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(1f, 0.5f);
            plusRect.anchorMax = new Vector2(1f, 0.5f);
            plusRect.pivot = new Vector2(1f, 0.5f);
            plusRect.sizeDelta = new Vector2(40f, 28f);
            plusRect.anchoredPosition = new Vector2(-4f, 0f);
            plus.GetComponent<Button>().onClick.AddListener(() => onPlus?.Invoke());
        }

        private void AddEquipmentSection()
        {
            AddSection(
                "Экипировка",
                $"Шлем: {Safe(characterData.eqHelmet)}\n" +
                $"Броня: {Safe(characterData.eqArmor)}\n" +
                $"Оружие 1: {Safe(characterData.eqWeapon)}\n" +
                $"Оружие 2: {Safe(characterData.eqWeapon2)}\n" +
                $"Щит: {Safe(characterData.eqShield)}\n" +
                $"Ботинки: {Safe(characterData.eqBoots)}\n" +
                $"Амулет: {Safe(characterData.eqAmulet)}   Кольцо: {Safe(characterData.eqRing)}\n" +
                $"Артефакт: {Safe(characterData.eqArtifact)}   Пояс: {Safe(characterData.eqBelt)}");
        }

        private void AddInventorySection()
        {
            AddSection("Инвентарь", FormatSlots(characterData.backpackSlots, true));
        }

        private void AddSkillsSection()
        {
            AddSection(
                "Скилы",
                "Оружие 1:\n" + FormatSlots(characterData.attackSlots, false) + "\n\n" +
                "Оружие 2:\n" + FormatSlots(characterData.attack2Slots, false) + "\n\n" +
                "Защита:\n" + FormatSlots(characterData.defenseSlots, false));
        }

        private void AddActionsSection()
        {
            var section = CreateSection("Действия GM");

            var row = CreatePanel("XP Row", section.transform, Color.clear);
            row.AddComponent<LayoutElement>().preferredHeight = 84f;

            xpInput = CreateInput(row.transform, "XP", "100");
            var inputRect = xpInput.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0.5f);
            inputRect.anchorMax = new Vector2(0f, 0.5f);
            inputRect.pivot = new Vector2(0f, 0.5f);
            inputRect.sizeDelta = new Vector2(120f, 34f);
            inputRect.anchoredPosition = new Vector2(0f, 20f);

            var xpButton = CreateButton("Дать XP", row.transform, new Color(0.18f, 0.14f, 0.1f, 1f));
            var xpRect = xpButton.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0f, 0.5f);
            xpRect.anchorMax = new Vector2(0f, 0.5f);
            xpRect.pivot = new Vector2(0f, 0.5f);
            xpRect.sizeDelta = new Vector2(130f, 34f);
            xpRect.anchoredPosition = new Vector2(132f, 20f);
            xpButton.GetComponent<Button>().onClick.AddListener(GiveXp);

            var levelButton = CreateButton("Повысить уровень", row.transform, new Color(0.18f, 0.26f, 0.13f, 1f));
            var levelRect = levelButton.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 0.5f);
            levelRect.anchorMax = new Vector2(0f, 0.5f);
            levelRect.pivot = new Vector2(0f, 0.5f);
            levelRect.sizeDelta = new Vector2(190f, 34f);
            levelRect.anchoredPosition = new Vector2(274f, 20f);
            levelButton.GetComponent<Button>().onClick.AddListener(LevelUp);

            var hpButton = CreateButton("Дать HP +5", row.transform, new Color(0.16f, 0.16f, 0.28f, 1f));
            var hpRect = hpButton.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0f, 0.5f);
            hpRect.anchorMax = new Vector2(0f, 0.5f);
            hpRect.pivot = new Vector2(0f, 0.5f);
            hpRect.sizeDelta = new Vector2(130f, 34f);
            hpRect.anchoredPosition = new Vector2(476f, 20f);
            hpButton.GetComponent<Button>().onClick.AddListener(() => GiveHp(5));

            var armorButton = CreateButton("\u0412\u043e\u0441\u0441\u0442. \u0431\u0440\u043e\u043d\u044e", row.transform, new Color(0.12f, 0.2f, 0.3f, 1f));
            var armorRect = armorButton.GetComponent<RectTransform>();
            armorRect.anchorMin = new Vector2(0f, 0.5f);
            armorRect.anchorMax = new Vector2(0f, 0.5f);
            armorRect.pivot = new Vector2(0f, 0.5f);
            armorRect.sizeDelta = new Vector2(150f, 34f);
            armorRect.anchoredPosition = new Vector2(0f, -20f);
            armorButton.GetComponent<Button>().onClick.AddListener(RestoreArmor);
        }

        private GameObject CreateSection(string title)
        {
            var section = CreatePanel(title, contentRoot, new Color(0.075f, 0.064f, 0.052f, 0.94f));
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = section.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = CreateLabel(title, section.transform, 18, FontStyle.Bold);
            header.alignment = TextAnchor.MiddleLeft;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            return section;
        }

        private void AddSection(string title, string body)
        {
            var section = CreateSection(title);
            var text = CreateLabel(body, section.transform, 15, FontStyle.Normal);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(34f, 20f * (body.Split('\n').Length + 1));
        }

        private void AddAttributePoint(string statName)
        {
            if (characterData == null || characterData.attributePoints <= 0)
            {
                return;
            }

            if (statName == "STR") characterData.strength++;
            else if (statName == "AGI") characterData.agility++;
            else if (statName == "INT") characterData.intelligence++;
            else if (statName == "HOL") characterData.holiness++;
            else return;

            characterData.attributePoints--;
            NotifyChanged();
            Refresh();
        }

        private void GiveXp()
        {
            if (characterData == null || token == null)
            {
                return;
            }

            var amount = 0;
            if (xpInput != null)
            {
                int.TryParse(xpInput.text, out amount);
            }

            if (amount <= 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(token.PlayerId))
            {
                CampaignGameSession.GrantCharacterXp(token.PlayerId, amount);
                SyncTokenFromPlayer();
            }
            else
            {
                characterData.xp = Mathf.Max(0, characterData.xp + amount);
                ApplyEligibleNpcXpLevels();
            }

            NotifyChanged();
            Refresh();
        }

        private void LevelUp()
        {
            if (token == null || characterData == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(token.PlayerId))
            {
                CampaignGameSession.GrantCharacterLevel(token.PlayerId);
                SyncTokenFromPlayer();
            }
            else
            {
                ApplyNpcLevelBonus(1);
            }

            NotifyChanged();
            Refresh();
        }

        private void ApplyEligibleNpcXpLevels()
        {
            var gainedLevels = 0;
            var level = Mathf.Max(1, characterData.level);
            var xp = Mathf.Max(0, characterData.xp);

            while (xp >= CampaignGameSession.GetRequiredXpForNextLevel(level))
            {
                gainedLevels++;
                level++;
            }

            if (gainedLevels > 0)
            {
                ApplyNpcLevelBonus(gainedLevels);
            }
        }

        private void ApplyNpcLevelBonus(int levelCount)
        {
            if (levelCount <= 0)
            {
                return;
            }

            var hpBonus = levelCount * 5;
            characterData.level = Mathf.Max(1, characterData.level) + levelCount;
            characterData.attributePoints += levelCount * 3;
            characterData.skillPoints += levelCount;
            characterData.maxHp = Mathf.Max(1, characterData.maxHp) + hpBonus;
            token.MaxHp = Mathf.Max(1, token.MaxHp) + hpBonus;
            if (!token.IsDead)
            {
                token.CurrentHp = Mathf.Min(token.CurrentHp + hpBonus, token.MaxHp);
            }
        }

        private void SyncTokenFromPlayer()
        {
            if (token == null || string.IsNullOrWhiteSpace(token.PlayerId))
            {
                return;
            }

            var player = CampaignGameSession.FindPlayer(token.PlayerId);
            if (player == null)
            {
                return;
            }

            token.MaxHp = player.maxHp;
            token.CurrentHp = player.currentHp;
            token.MaxArmor = player.maxArmor;
            token.CurrentArmor = player.currentArmor;
        }

        private void GiveHp(int amount)
        {
            if (token == null || amount <= 0 || token.IsDead)
            {
                return;
            }

            token.CurrentHp = Mathf.Min(token.CurrentHp + amount, token.MaxHp);
            NotifyChanged();
            Refresh();
        }

        private void RestoreArmor()
        {
            if (token == null)
            {
                return;
            }

            var id = string.IsNullOrWhiteSpace(token.PlayerId) ? token.RuntimeId : token.PlayerId;
            var mapId = ResolveCurrentMapId();
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (CampaignGameSession.RestoreTokenArmor(id, mapId))
            {
                token.CurrentArmor = token.MaxArmor;
            }
            else
            {
                token.CurrentArmor = Mathf.Max(0, token.MaxArmor);
            }

            SyncTokenFromPlayer();
            NotifyChanged();
            Refresh();
        }

        private void NotifyChanged()
        {
            CampaignGameSession.TriggerPlayersChanged();

            if (loader != null && loader.UI != null)
            {
                loader.UI.RefreshActiveTokensPanel();
                loader.UI.RefreshEntityInspector(token);
            }
        }

        private string ResolveCurrentMapId()
        {
            if (loader != null && loader.Context != null && loader.Context.CurrentMapNode != null)
            {
                return loader.Context.CurrentMapNode.id;
            }

            return "";
        }

        private static string FormatSlots(string[] slots, bool numbered)
        {
            if (slots == null || slots.Length == 0)
            {
                return "-";
            }

            var result = "";
            for (var i = 0; i < slots.Length; i++)
            {
                var value = Safe(slots[i]);
                if (value == "-")
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += "\n";
                }

                result += numbered ? $"{i + 1}. {value}" : $"{i + 1}: {value}";
            }

            return result.Length == 0 ? "-" : result;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text CreateLabel(string text, Transform parent, int fontSize, FontStyle style)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateButton(string text, Transform parent, Color color)
        {
            var go = CreatePanel(text, parent, color);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var label = CreateLabel(text, go.transform, 15, FontStyle.Bold);
            label.color = new Color(0.95f, 0.9f, 0.78f, 1f);
            return go;
        }

        private static InputField CreateInput(Transform parent, string placeholder, string value)
        {
            var go = CreatePanel("Input", parent, new Color(0.02f, 0.018f, 0.015f, 1f));
            var input = go.AddComponent<InputField>();

            var text = CreateLabel(value, go.transform, 16, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            input.textComponent = text;
            input.text = value;
            input.contentType = InputField.ContentType.IntegerNumber;

            var placeholderText = CreateLabel(placeholder, go.transform, 14, FontStyle.Italic);
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            input.placeholder = placeholderText;
            return input;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static void EnsureEventSystem()
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
                Destroy(legacyModule);
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

        private void Close()
        {
            if (current == this)
            {
                current = null;
            }

            Destroy(gameObject);
        }
    }
}
