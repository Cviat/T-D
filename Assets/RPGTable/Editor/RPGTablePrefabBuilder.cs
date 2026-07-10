using System.IO;
using RPGTable.Runtime;
using RPGTable.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Editor
{
    public static class RPGTablePrefabBuilder
    {
        private const string PrefabFolder = "Assets/RPGTable/Resources/Prefabs";
        private const string TokenCardPath = PrefabFolder + "/TokenCard.prefab";
        private const string MapCardPath = PrefabFolder + "/MapCard.prefab";
        private const string GMBottomToolsPath = PrefabFolder + "/GMBottomTools.prefab";
        private const string TokenWorldBarsPath = PrefabFolder + "/TokenWorldBars.prefab";
        private const string CombatInspectorPath = PrefabFolder + "/CombatInspector.prefab";

        private static Sprite defaultUiSprite;

        [MenuItem("RPG Table/Create UI Prefabs")]
        public static void CreatePrefabs()
        {
            Directory.CreateDirectory(PrefabFolder);
            LoadDefaultUiSprite();

            BuildTokenCardPrefab();
            BuildMapCardPrefab();
            BuildGMBottomToolsPrefab();
            BuildTokenWorldBarsPrefab();
            BuildCombatInspectorPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("RPG Table GM UI Prefabs created successfully in: " + PrefabFolder);
        }

        [MenuItem("RPG Table/Create Combat Inspector Prefab")]
        public static void CreateCombatInspectorPrefab()
        {
            Directory.CreateDirectory(PrefabFolder);
            LoadDefaultUiSprite();
            BuildCombatInspectorPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatInspector prefab created successfully: " + CombatInspectorPath);
        }

        private static void LoadDefaultUiSprite()
        {
            defaultUiSprite = null;
        }

        private static void BuildTokenCardPrefab()
        {
            var card = CreateUIElement("Token Card", new Vector2(230f, 58f));
            card.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 0.95f);
            if (defaultUiSprite != null)
            {
                card.GetComponent<Image>().sprite = defaultUiSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;
            }
            card.AddComponent<LayoutElement>().preferredHeight = 58f;
            var button = card.AddComponent<Button>();

            // Portrait image
            var portrait = CreateUIElement("Portrait", new Vector2(40f, 40f), card.transform);
            var portRect = portrait.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0f, 0.5f);
            portRect.anchorMax = new Vector2(0f, 0.5f);
            portRect.anchoredPosition = new Vector2(25f, 0f);
            var portImg = portrait.AddComponent<Image>();
            portImg.color = new Color(0f, 0f, 0f, 0.5f);

            // Name Label
            var nameLbl = CreateTextElement("NameLabel", "Token Name", 13, FontStyle.Bold, card.transform);
            var nameRect = nameLbl.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.offsetMin = new Vector2(56f, 8f);
            nameRect.offsetMax = new Vector2(-10f, 22f);
            nameLbl.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            // HP Bar Background
            var hpBarBg = CreateUIElement("HP Bar Bg", new Vector2(0f, 10f), card.transform);
            var hpBgRect = hpBarBg.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 0.5f);
            hpBgRect.anchorMax = new Vector2(1f, 0.5f);
            hpBgRect.offsetMin = new Vector2(56f, -20f);
            hpBgRect.offsetMax = new Vector2(-10f, -8f);
            hpBarBg.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
            if (defaultUiSprite != null)
            {
                hpBarBg.GetComponent<Image>().sprite = defaultUiSprite;
                hpBarBg.GetComponent<Image>().type = Image.Type.Sliced;
            }

            // HP Bar Fill
            var hpBarFill = CreateUIElement("HP Bar Fill", Vector2.zero, hpBarBg.transform);
            var hpFillRect = hpBarFill.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpBarFill.AddComponent<Image>().color = new Color(0.12f, 0.7f, 0.12f, 1f);
            if (defaultUiSprite != null)
            {
                hpBarFill.GetComponent<Image>().sprite = defaultUiSprite;
                hpBarFill.GetComponent<Image>().type = Image.Type.Sliced;
            }

            // HP Text
            var hpTxt = CreateTextElement("HP Text", "50/50", 8, FontStyle.Bold, hpBarBg.transform);
            var hpTxtRect = hpTxt.GetComponent<RectTransform>();
            hpTxtRect.anchorMin = Vector2.zero;
            hpTxtRect.anchorMax = Vector2.one;
            hpTxtRect.offsetMin = Vector2.zero;
            hpTxtRect.offsetMax = Vector2.zero;

            // Attach View component & Wire references
            var view = card.AddComponent<TokenCardView>();
            view.portraitImage = portImg;
            view.nameText = nameLbl.GetComponent<Text>();
            view.hpBarFill = hpFillRect;
            view.hpText = hpTxt.GetComponent<Text>();
            view.selectButton = button;

            PrefabUtility.SaveAsPrefabAsset(card, TokenCardPath);
            Object.DestroyImmediate(card);
        }

        private static void BuildMapCardPrefab()
        {
            var card = CreateUIElement("Map Card", new Vector2(110f, 70f));
            card.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 0.95f);
            if (defaultUiSprite != null)
            {
                card.GetComponent<Image>().sprite = defaultUiSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;
            }
            var button = card.AddComponent<Button>();

            // Map Preview
            var preview = CreateUIElement("Map Preview", Vector2.zero, card.transform);
            var prevRect = preview.GetComponent<RectTransform>();
            prevRect.anchorMin = Vector2.zero;
            prevRect.anchorMax = Vector2.one;
            prevRect.offsetMin = new Vector2(4f, 22f);
            prevRect.offsetMax = new Vector2(-4f, -4f);
            var prevImg = preview.AddComponent<Image>();
            prevImg.color = new Color(0f, 0f, 0f, 0.3f);

            // Map Title label at the bottom
            var titleLbl = CreateTextElement("MapTitle", "Location", 11, FontStyle.Bold, card.transform);
            var titleRect = titleLbl.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 0f);
            titleRect.offsetMin = new Vector2(2f, 2f);
            titleRect.offsetMax = new Vector2(-2f, 20f);

            // Wire view
            var view = card.AddComponent<MapCardView>();
            view.previewImage = prevImg;
            view.nameText = titleLbl.GetComponent<Text>();
            view.selectButton = button;

            PrefabUtility.SaveAsPrefabAsset(card, MapCardPath);
            Object.DestroyImmediate(card);
        }

        private static void BuildGMBottomToolsPrefab()
        {
            var root = CreateUIElement("GMBottomTools", new Vector2(300f, 65f));
            root.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.7f);
            if (defaultUiSprite != null)
            {
                root.GetComponent<Image>().sprite = defaultUiSprite;
                root.GetComponent<Image>().type = Image.Type.Sliced;
            }

            var horizontalList = new GameObject("Toolset Group", typeof(RectTransform));
            horizontalList.transform.SetParent(root.transform, false);
            var listRect = horizontalList.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(6f, 6f);
            listRect.offsetMax = new Vector2(-6f, -6f);

            var layout = horizontalList.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var pvCamBtn = CreateButtonElement("PV Camera", "Камера игроков", 110f, horizontalList.transform);
            var drawBtn = CreateButtonElement("Draw", "Рисование", 85f, horizontalList.transform);
            var measureBtn = CreateButtonElement("Measure", "Линейка", 85f, horizontalList.transform);

            var view = root.AddComponent<GMBottomToolsView>();
            view.playerViewCameraButton = pvCamBtn.GetComponent<Button>();
            view.drawButton = drawBtn.GetComponent<Button>();
            view.measureButton = measureBtn.GetComponent<Button>();

            PrefabUtility.SaveAsPrefabAsset(root, GMBottomToolsPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildTokenWorldBarsPrefab()
        {
            var root = CreateUIElement("Token World Bars", new Vector2(120f, 50f));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var healthRoot = CreateBarRoot("Health", root.transform, new Vector2(0f, 10f), new Color(0.12f, 0.12f, 0.12f, 0.85f));
            var healthFill = CreateBarFill("Fill", healthRoot.transform, new Color(0.82f, 0.12f, 0.12f, 1f));
            var healthText = CreateBarText("Text", healthRoot.transform);

            var armorRoot = CreateBarRoot("Armor", root.transform, new Vector2(0f, -10f), new Color(0.12f, 0.12f, 0.12f, 0.85f));
            var armorFill = CreateBarFill("Fill", armorRoot.transform, new Color(0.62f, 0.66f, 0.75f, 1f));
            var armorText = CreateBarText("Text", armorRoot.transform);

            var view = root.AddComponent<TokenWorldBarsView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("healthRoot").objectReferenceValue = healthRoot;
            serialized.FindProperty("healthFill").objectReferenceValue = healthFill;
            serialized.FindProperty("healthText").objectReferenceValue = healthText;
            serialized.FindProperty("armorRoot").objectReferenceValue = armorRoot;
            serialized.FindProperty("armorFill").objectReferenceValue = armorFill;
            serialized.FindProperty("armorText").objectReferenceValue = armorText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TokenWorldBarsPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildCombatInspectorPrefab()
        {
            var root = CreateUIElement("Combat Inspector", new Vector2(300f, 300f));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            root.AddComponent<LayoutElement>().preferredHeight = 300f;

            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.08f, 0.07f, 0.055f, 0.96f);
            ApplyGuiSprite(rootImage, "big_background");

            var portraitFrame = CreateUIElement("Portrait Frame", new Vector2(82f, 82f), root.transform);
            var portraitFrameRect = portraitFrame.GetComponent<RectTransform>();
            portraitFrameRect.anchorMin = new Vector2(0f, 1f);
            portraitFrameRect.anchorMax = new Vector2(0f, 1f);
            portraitFrameRect.pivot = new Vector2(0f, 1f);
            portraitFrameRect.anchoredPosition = new Vector2(10f, -10f);
            var portraitFrameImage = portraitFrame.AddComponent<Image>();
            portraitFrameImage.color = Color.white;
            ApplyGuiSprite(portraitFrameImage, "lil_roundbackground");

            var portrait = CreateUIElement("Portrait", new Vector2(66f, 66f), portraitFrame.transform);
            var portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            var portraitImage = portrait.AddComponent<Image>();
            portraitImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            portraitImage.preserveAspect = true;

            var portraitOverlay = CreateUIElement("Portrait Overlay", new Vector2(82f, 82f), portraitFrame.transform);
            var portraitOverlayRect = portraitOverlay.GetComponent<RectTransform>();
            portraitOverlayRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitOverlayRect.pivot = new Vector2(0.5f, 0.5f);
            portraitOverlayRect.anchoredPosition = Vector2.zero;
            var portraitOverlayImage = portraitOverlay.AddComponent<Image>();
            portraitOverlayImage.color = Color.white;
            portraitOverlayImage.raycastTarget = false;
            ApplyGuiSprite(portraitOverlayImage, "lil_roundframe");

            var nameLabel = CreateTextElement("Name", "Фишка", 15, FontStyle.Bold, root.transform);
            PlaceRect(nameLabel.GetComponent<RectTransform>(), new Vector2(100f, -10f), new Vector2(190f, 24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            ConfigureBestFit(nameLabel.GetComponent<Text>(), 10, 15, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            nameLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var stateLabel = CreateTextElement("State", "HP: 0/0", 12, FontStyle.Bold, root.transform);
            ConfigureBestFit(stateLabel.GetComponent<Text>(), 9, 12, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            PlaceRect(stateLabel.GetComponent<RectTransform>(), new Vector2(100f, -36f), new Vector2(190f, 20f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            stateLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var armorLabel = CreateTextElement("Armor", "Броня: 0/0", 11, FontStyle.Normal, root.transform);
            PlaceRect(armorLabel.GetComponent<RectTransform>(), new Vector2(100f, -58f), new Vector2(92f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            armorLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var rollsLabel = CreateTextElement("Rolls", "Броски: 0/0", 11, FontStyle.Normal, root.transform);
            PlaceRect(rollsLabel.GetComponent<RectTransform>(), new Vector2(198f, -58f), new Vector2(92f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            rollsLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var movementLabel = CreateTextElement("Movement", "Ход: свободно", 11, FontStyle.Normal, root.transform);
            PlaceRect(movementLabel.GetComponent<RectTransform>(), new Vector2(100f, -78f), new Vector2(190f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            ConfigureBestFit(movementLabel.GetComponent<Text>(), 8, 11, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            movementLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var descLabel = CreateTextElement("Description", "Нет описания.", 10, FontStyle.Italic, root.transform);
            PlaceRect(descLabel.GetComponent<RectTransform>(), new Vector2(10f, -98f), new Vector2(280f, 36f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var descText = descLabel.GetComponent<Text>();
            descText.alignment = TextAnchor.UpperLeft;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ConfigureBestFit(descText, 8, 10, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            descText.verticalOverflow = VerticalWrapMode.Truncate;

            var statsLabel = CreateTextElement("Stats", "Размер: 1x1", 10, FontStyle.Normal, root.transform);
            PlaceRect(statsLabel.GetComponent<RectTransform>(), new Vector2(10f, -138f), new Vector2(130f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            statsLabel.GetComponent<Text>().alignment = TextAnchor.UpperLeft;

            var weaponLabel = CreateTextElement("Weapon", "Оружие: нет", 11, FontStyle.Bold, root.transform);
            ConfigureBestFit(weaponLabel.GetComponent<Text>(), 8, 11, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            PlaceRect(weaponLabel.GetComponent<RectTransform>(), new Vector2(10f, -174f), new Vector2(280f, 20f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            weaponLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var attacksLabel = CreateTextElement("Attacks", "Атаки: -", 10, FontStyle.Normal, root.transform);
            PlaceRect(attacksLabel.GetComponent<RectTransform>(), new Vector2(10f, -196f), new Vector2(280f, 46f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            attacksLabel.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            ConfigureBestFit(attacksLabel.GetComponent<Text>(), 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);

            var defenseLabel = CreateTextElement("Defense", "Защита: -", 10, FontStyle.Normal, root.transform);
            PlaceRect(defenseLabel.GetComponent<RectTransform>(), new Vector2(10f, -242f), new Vector2(170f, 36f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            defenseLabel.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            ConfigureBestFit(defenseLabel.GetComponent<Text>(), 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);

            var statusesLabel = CreateTextElement("Statuses", "Статусы: -", 10, FontStyle.Normal, root.transform);
            PlaceRect(statusesLabel.GetComponent<RectTransform>(), new Vector2(184f, -242f), new Vector2(106f, 36f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            statusesLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            ConfigureBestFit(statusesLabel.GetComponent<Text>(), 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);

            var switchButton = CreateInspectorButton("Switch Weapon", "Сменить оружие", root.transform);
            PlaceRect(switchButton.GetComponent<RectTransform>(), new Vector2(150f, 10f), new Vector2(140f, 28f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            var switchText = switchButton.GetComponentInChildren<Text>();

            var hpInputGo = CreateInputElement("HP Input", "5", root.transform);
            PlaceRect(hpInputGo.GetComponent<RectTransform>(), new Vector2(10f, 10f), new Vector2(38f, 28f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            var hpInput = hpInputGo.GetComponent<InputField>();

            var damageButton = CreateInspectorButton("Damage", "-", root.transform);
            PlaceRect(damageButton.GetComponent<RectTransform>(), new Vector2(54f, 10f), new Vector2(40f, 28f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            var healButton = CreateInspectorButton("Heal", "+", root.transform);
            PlaceRect(healButton.GetComponent<RectTransform>(), new Vector2(98f, 10f), new Vector2(40f, 28f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            var view = root.AddComponent<EntityInspectorView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("portraitImage").objectReferenceValue = portraitImage;
            serialized.FindProperty("nameLabel").objectReferenceValue = nameLabel.GetComponent<Text>();
            serialized.FindProperty("stateLabel").objectReferenceValue = stateLabel.GetComponent<Text>();
            serialized.FindProperty("descLabel").objectReferenceValue = descLabel.GetComponent<Text>();
            serialized.FindProperty("statsLabel").objectReferenceValue = statsLabel.GetComponent<Text>();
            serialized.FindProperty("armorLabel").objectReferenceValue = armorLabel.GetComponent<Text>();
            serialized.FindProperty("rollsLabel").objectReferenceValue = rollsLabel.GetComponent<Text>();
            serialized.FindProperty("movementLabel").objectReferenceValue = movementLabel.GetComponent<Text>();
            serialized.FindProperty("weaponLabel").objectReferenceValue = weaponLabel.GetComponent<Text>();
            serialized.FindProperty("attacksLabel").objectReferenceValue = attacksLabel.GetComponent<Text>();
            serialized.FindProperty("defenseLabel").objectReferenceValue = defenseLabel.GetComponent<Text>();
            serialized.FindProperty("statusesLabel").objectReferenceValue = statusesLabel.GetComponent<Text>();
            serialized.FindProperty("hpInput").objectReferenceValue = hpInput;
            serialized.FindProperty("damageButton").objectReferenceValue = damageButton.GetComponent<Button>();
            serialized.FindProperty("healButton").objectReferenceValue = healButton.GetComponent<Button>();
            serialized.FindProperty("weaponSwitchButton").objectReferenceValue = switchButton.GetComponent<Button>();
            serialized.FindProperty("weaponSwitchText").objectReferenceValue = switchText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CombatInspectorPath);
            Object.DestroyImmediate(root);
        }

        // ── Assembly Helpers ─────────────────────────────────────────────

        private static GameObject CreateUIElement(string name, Vector2 size, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private static GameObject CreateTextElement(string name, string label, int fontSize, FontStyle style, Transform parent)
        {
            var go = CreateUIElement(name, Vector2.zero, parent);
            var text = go.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        private static GameObject CreateButtonElement(string name, string label, float width, Transform parent)
        {
            var go = CreateUIElement(name, new Vector2(width, 0f), parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (defaultUiSprite != null)
            {
                img.sprite = defaultUiSprite;
                img.type = Image.Type.Sliced;
            }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<LayoutElement>().preferredWidth = width;

            var text = CreateTextElement("Label", label, 13, FontStyle.Bold, go.transform);
            var tRect = text.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(6f, 3f);
            tRect.offsetMax = new Vector2(-6f, -3f);

            return go;
        }

        private static GameObject CreateBarRoot(string name, Transform parent, Vector2 anchoredPosition, Color backgroundColor)
        {
            var go = CreateUIElement(name, new Vector2(110f, 18f), parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.AddComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = false;
            if (defaultUiSprite != null)
            {
                image.sprite = defaultUiSprite;
                image.type = Image.Type.Sliced;
            }

            return go;
        }

        private static RectTransform CreateBarFill(string name, Transform parent, Color color)
        {
            var go = CreateUIElement(name, Vector2.zero, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (defaultUiSprite != null)
            {
                image.sprite = defaultUiSprite;
                image.type = Image.Type.Sliced;
            }

            return rect;
        }

        private static Text CreateBarText(string name, Transform parent)
        {
            var go = CreateTextElement(name, "0/0", 10, FontStyle.Bold, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static void PlaceRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ConfigureBestFit(Text text, int minSize, int maxSize, HorizontalWrapMode horizontalWrap, VerticalWrapMode verticalWrap)
        {
            if (text == null)
            {
                return;
            }

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = horizontalWrap;
            text.verticalOverflow = verticalWrap;
        }

        private static void ApplyGuiSprite(Image image, string spriteName)
        {
            if (image == null || string.IsNullOrWhiteSpace(spriteName))
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/GUI_Parts/Gui_parts/{spriteName}.png");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        private static GameObject CreateInspectorButton(string name, string label, Transform parent)
        {
            var go = CreateUIElement(name, new Vector2(100f, 28f), parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.13f, 1f);
            ApplyGuiSprite(image, "button2");

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = CreateTextElement("Text", label, 10, FontStyle.Bold, go.transform);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);
            textObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static GameObject CreateInputElement(string name, string value, Transform parent)
        {
            var go = CreateUIElement(name, new Vector2(38f, 28f), parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.06f, 1f);
            ApplyGuiSprite(image, "button_frame");

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = value;

            var textObject = CreateTextElement("Text", value, 11, FontStyle.Bold, go.transform);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);
            var text = textObject.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            input.textComponent = text;

            var placeholderObject = CreateTextElement("Placeholder", value, 11, FontStyle.Normal, go.transform);
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(4f, 2f);
            placeholderRect.offsetMax = new Vector2(-4f, -2f);
            var placeholder = placeholderObject.GetComponent<Text>();
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            input.placeholder = placeholder;

            return go;
        }
    }
}
