using RPGTable.CharacterEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.Editor
{
    public static class RPGTableCharacterEditorBuilder
    {
        public const string ScenePath = "Assets/RPGTable/Scenes/CharacterEditor.unity";

        private static Sprite GetGuiSprite(string filename)
        {
            var path = "Assets/GUI_Parts/Gui_parts/" + filename;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("RPG Table/Build Character Editor Scene")]
        public static void BuildCharacterEditorScene()
        {
            System.IO.Directory.CreateDirectory("Assets/RPGTable/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CharacterEditor";

            CreateCamera();
            var canvasObject = new GameObject("Character Editor Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            // Load GUI Sprites
            var btnSprite = GetGuiSprite("button2.png");
            var frameSprite = GetGuiSprite("Frame_mid.png");
            var slotSprite = GetGuiSprite("Mini_frame1.png");
            var barSprite = GetGuiSprite("name_bar.png");
            var dialogBgSprite = GetGuiSprite("Mini_background.png");

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.055f, 0.052f, 0.047f, 1f));
            Stretch(root);

            var leftPanel = CreatePanel("Left Panel", root.transform, new Color(0.075f, 0.066f, 0.055f, 0.98f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.anchoredPosition = Vector2.zero;
            leftRect.sizeDelta = new Vector2(330f, 0f);

            var backButton = CreateStyledButton("Back Button", leftPanel.transform, "Back", new Vector2(280f, 48f), btnSprite);
            PositionTop(backButton.GetComponent<RectTransform>(), -20f);
            var saveButton = CreateStyledButton("Save Character Button", leftPanel.transform, "Save Character", new Vector2(280f, 48f), btnSprite);
            PositionTop(saveButton.GetComponent<RectTransform>(), -82f);
            var openButton = CreateStyledButton("Open Character Button", leftPanel.transform, "Import Character", new Vector2(280f, 48f), btnSprite);
            PositionTop(openButton.GetComponent<RectTransform>(), -144f);

            var title = CreateText("Title", root.transform, 36, FontStyle.Bold, TextAnchor.MiddleLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(370f, -92f);
            titleRect.offsetMax = new Vector2(-40f, -28f);
            title.text = "Character Editor";

            // Main Content Panel containing Tabs & Tab Content Panels
            var contentPanel = CreatePanel("Content Panel", root.transform, new Color(0.068f, 0.062f, 0.054f, 1f));
            var contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(370f, 120f);
            contentRect.offsetMax = new Vector2(-40f, -130f);

            // Tab Bar Panel
            var tabBar = CreatePanel("Tab Bar", contentPanel.transform, new Color(0.12f, 0.11f, 0.1f, 1f));
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = Vector2.zero;
            tabBarRect.sizeDelta = new Vector2(0f, 60f);

            var charTabBtnObj = CreateStyledButton("Character Tab Button", tabBar.transform, "ПЕРСОНАЖ", new Vector2(250f, 60f), btnSprite);
            var charTabBtnRt = charTabBtnObj.GetComponent<RectTransform>();
            charTabBtnRt.anchorMin = new Vector2(0f, 0.5f);
            charTabBtnRt.anchorMax = new Vector2(0f, 0.5f);
            charTabBtnRt.pivot = new Vector2(0f, 0.5f);
            charTabBtnRt.anchoredPosition = new Vector2(20f, 0f);
            var charTabBtn = charTabBtnObj.GetComponent<Button>();

            var statsTabBtnObj = CreateStyledButton("Stats Tab Button", tabBar.transform, "ХАРАКТЕРИСТИКИ", new Vector2(250f, 60f), btnSprite);
            var statsTabBtnRt = statsTabBtnObj.GetComponent<RectTransform>();
            statsTabBtnRt.anchorMin = new Vector2(0f, 0.5f);
            statsTabBtnRt.anchorMax = new Vector2(0f, 0.5f);
            statsTabBtnRt.pivot = new Vector2(0f, 0.5f);
            statsTabBtnRt.anchoredPosition = new Vector2(280f, 0f);
            var statsTabBtn = statsTabBtnObj.GetComponent<Button>();

            // Character Tab Panel
            var charTabPanel = new GameObject("Character Tab Panel", typeof(RectTransform));
            charTabPanel.transform.SetParent(contentPanel.transform, false);
            var charTabPanelRt = charTabPanel.GetComponent<RectTransform>();
            charTabPanelRt.anchorMin = Vector2.zero;
            charTabPanelRt.anchorMax = Vector2.one;
            charTabPanelRt.offsetMin = Vector2.zero;
            charTabPanelRt.offsetMax = new Vector2(0f, -60f);

            // Stats Tab Panel
            var statsTabPanel = new GameObject("Stats Tab Panel", typeof(RectTransform));
            statsTabPanel.transform.SetParent(contentPanel.transform, false);
            var statsTabPanelRt = statsTabPanel.GetComponent<RectTransform>();
            statsTabPanelRt.anchorMin = Vector2.zero;
            statsTabPanelRt.anchorMax = Vector2.one;
            statsTabPanelRt.offsetMin = Vector2.zero;
            statsTabPanelRt.offsetMax = new Vector2(0f, -60f);

            // ──── Populate Tab 1: Character (With Inventory & Equipment) ────
            var nameInput = CreateInput("Name Input", charTabPanel.transform, "Имя персонажа", false);
            var nameRect = nameInput.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f); nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(40f, -30f);
            nameRect.sizeDelta = new Vector2(500f, 40f);

            // Portrait block with Equipment Slots around it
            var portraitObject = new GameObject("Portrait", typeof(RectTransform));
            portraitObject.transform.SetParent(charTabPanel.transform, false);
            var portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 1f); portraitRect.anchorMax = new Vector2(0f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(340f, -280f); // Center of grid
            portraitRect.sizeDelta = new Vector2(280f, 280f);
            var portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = new Color(0.12f, 0.11f, 0.1f, 1f);
            portraitImage.preserveAspect = true;
            // Make portrait clickable to import photo
            portraitObject.AddComponent<Button>().targetGraphic = portraitImage;

            var portraitFrameObj = new GameObject("Portrait Frame", typeof(RectTransform));
            portraitFrameObj.transform.SetParent(charTabPanel.transform, false);
            var frameImgRect = portraitFrameObj.GetComponent<RectTransform>();
            frameImgRect.anchorMin = new Vector2(0f, 1f); frameImgRect.anchorMax = new Vector2(0f, 1f);
            frameImgRect.pivot = new Vector2(0.5f, 0.5f);
            frameImgRect.anchoredPosition = new Vector2(340f, -280f);
            frameImgRect.sizeDelta = new Vector2(300f, 300f);
            var frameImg = portraitFrameObj.AddComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            // Equipment Slots (size 96x96)
            var eqHelmet = CreateItemSlot("Slot Helmet", charTabPanel.transform, "Шлем", slotSprite);
            eqHelmet.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Helmet;
            PositionCenter(eqHelmet.GetComponent<RectTransform>(), 340f, -50f);

            var eqArmor = CreateItemSlot("Slot Armor", charTabPanel.transform, "Доспех", slotSprite);
            eqArmor.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Armor;
            PositionCenter(eqArmor.GetComponent<RectTransform>(), 340f, -510f);

            var eqBoots = CreateItemSlot("Slot Boots", charTabPanel.transform, "Обувь", slotSprite);
            eqBoots.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Boots;
            PositionCenter(eqBoots.GetComponent<RectTransform>(), 340f, -620f);

            var eqAmulet = CreateItemSlot("Slot Amulet", charTabPanel.transform, "Амулет", slotSprite);
            eqAmulet.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Amulet;
            PositionCenter(eqAmulet.GetComponent<RectTransform>(), 120f, -150f);

            var eqWeapon = CreateItemSlot("Slot Weapon", charTabPanel.transform, "Оружие 1", slotSprite);
            eqWeapon.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Weapon;
            PositionCenter(eqWeapon.GetComponent<RectTransform>(), 120f, -260f);

            var eqWeapon2 = CreateItemSlot("Slot Weapon 2", charTabPanel.transform, "Оружие 2", slotSprite);
            eqWeapon2.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Weapon;
            PositionCenter(eqWeapon2.GetComponent<RectTransform>(), 120f, -370f);

            var eqArtifact = CreateItemSlot("Slot Artifact", charTabPanel.transform, "Артефакт", slotSprite);
            eqArtifact.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Artifact;
            PositionCenter(eqArtifact.GetComponent<RectTransform>(), 120f, -480f);

            var eqRing = CreateItemSlot("Slot Ring", charTabPanel.transform, "Кольцо", slotSprite);
            eqRing.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Ring;
            PositionCenter(eqRing.GetComponent<RectTransform>(), 560f, -150f);

            var eqShield = CreateItemSlot("Slot Shield", charTabPanel.transform, "Щит", slotSprite);
            eqShield.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Shield;
            PositionCenter(eqShield.GetComponent<RectTransform>(), 560f, -260f);

            var eqBelt = CreateItemSlot("Slot Belt", charTabPanel.transform, "Пояс", slotSprite);
            eqBelt.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Belt;
            PositionCenter(eqBelt.GetComponent<RectTransform>(), 560f, -370f);

            // Backpack Section (Middle)
            var backpackTitlePanel = CreatePanel("Backpack Title Panel", charTabPanel.transform, Color.white);
            var bpTitleRt = backpackTitlePanel.GetComponent<RectTransform>();
            bpTitleRt.anchorMin = new Vector2(0f, 1f); bpTitleRt.anchorMax = new Vector2(0f, 1f);
            bpTitleRt.pivot = new Vector2(0.5f, 0.5f);
            bpTitleRt.anchoredPosition = new Vector2(856f, -50f);
            bpTitleRt.sizeDelta = new Vector2(280f, 32f);
            var bpTitleImg = backpackTitlePanel.GetComponent<Image>();
            bpTitleImg.sprite = barSprite;

            var backpackTitle = CreateText("Label", backpackTitlePanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(backpackTitle.gameObject);

            var backpackPanel = new GameObject("Backpack Grid", typeof(RectTransform));
            backpackPanel.transform.SetParent(charTabPanel.transform, false);
            var bpPanelRt = backpackPanel.GetComponent<RectTransform>();
            bpPanelRt.anchorMin = new Vector2(0f, 1f); bpPanelRt.anchorMax = new Vector2(0f, 1f);
            bpPanelRt.pivot = new Vector2(0.5f, 1f);
            bpPanelRt.anchoredPosition = new Vector2(856f, -100f);
            bpPanelRt.sizeDelta = new Vector2(220f, 440f);

            var bpLayout = backpackPanel.AddComponent<GridLayoutGroup>();
            bpLayout.cellSize = new Vector2(96f, 96f);
            bpLayout.spacing = new Vector2(16f, 16f);

            var backpackFields = new InputField[8];
            for (var i = 0; i < backpackFields.Length; i++)
            {
                backpackFields[i] = CreateItemSlot($"Backpack {i + 1}", backpackPanel.transform, "Пусто", slotSprite);
                backpackFields[i].GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.General;
            }

            // Biography Description (Bottom Right)
            var descTitlePanel = CreatePanel("Desc Title Panel", charTabPanel.transform, Color.white);
            var descTitleRt = descTitlePanel.GetComponent<RectTransform>();
            descTitleRt.anchorMin = new Vector2(0f, 1f); descTitleRt.anchorMax = new Vector2(0f, 1f);
            descTitleRt.pivot = new Vector2(0f, 1f);
            descTitleRt.anchoredPosition = new Vector2(680f, -520f);
            descTitleRt.sizeDelta = new Vector2(790f, 32f);
            var descTitleImg = descTitlePanel.GetComponent<Image>();
            descTitleImg.sprite = barSprite;

            var descTitle = CreateText("Биография / Описание", descTitlePanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(descTitle.gameObject);

            var descriptionInput = CreateInput("Description Input", charTabPanel.transform, "История, характер и особые заметки персонажа...", true);
            var descriptionRect = descriptionInput.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 1f); descriptionRect.anchorMax = new Vector2(0f, 1f);
            descriptionRect.pivot = new Vector2(0f, 1f);
            descriptionRect.anchoredPosition = new Vector2(680f, -560f);
            descriptionRect.sizeDelta = new Vector2(790f, 170f);

            // Selected Token Visual Preview block
            var tokenPanel = CreatePanel("Token Preview Panel", charTabPanel.transform, new Color(0.08f, 0.074f, 0.064f, 0.98f));
            var tokenPanelRect = tokenPanel.GetComponent<RectTransform>();
            tokenPanelRect.anchorMin = new Vector2(0f, 1f); tokenPanelRect.anchorMax = new Vector2(0f, 1f);
            tokenPanelRect.pivot = new Vector2(0f, 1f);
            tokenPanelRect.anchoredPosition = new Vector2(1110f, -40f);
            tokenPanelRect.sizeDelta = new Vector2(360f, 460f);

            // Container for stacked token sprites
            var tokenPreviewRoot = new GameObject("Token Preview Root", typeof(RectTransform));
            tokenPreviewRoot.transform.SetParent(tokenPanel.transform, false);
            var previewRootRt = tokenPreviewRoot.GetComponent<RectTransform>();
            previewRootRt.anchorMin = new Vector2(0.5f, 1f); previewRootRt.anchorMax = new Vector2(0.5f, 1f);
            previewRootRt.pivot = new Vector2(0.5f, 1f);
            previewRootRt.anchoredPosition = new Vector2(0f, -28f);
            previewRootRt.sizeDelta = new Vector2(300f, 300f);

            // Layer 1: Circle Mask
            var maskObject = new GameObject("Token Mask", typeof(RectTransform));
            maskObject.transform.SetParent(tokenPreviewRoot.transform, false);
            Stretch(maskObject);
            var maskImage = maskObject.AddComponent<Image>();
            maskImage.sprite = CreateCircleSprite();
            maskImage.color = Color.white;
            var mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Layer 1 Child: Token Portrait Preview
            var tokenPortraitObj = new GameObject("Token Portrait Preview", typeof(RectTransform));
            tokenPortraitObj.transform.SetParent(maskObject.transform, false);
            Stretch(tokenPortraitObj);
            var tokenPortraitImg = tokenPortraitObj.AddComponent<Image>();
            tokenPortraitImg.color = Color.clear;
            tokenPortraitImg.preserveAspect = true;

            // Layer 2: Token Frame Preview
            var tokenFrameObj = new GameObject("Token Frame Preview", typeof(RectTransform));
            tokenFrameObj.transform.SetParent(tokenPreviewRoot.transform, false);
            Stretch(tokenFrameObj);
            var tokenFrameImg = tokenFrameObj.AddComponent<Image>();
            tokenFrameImg.color = Color.clear;
            tokenFrameImg.preserveAspect = true;

            var selectTokenButton = CreateStyledButton("Select Token Button", tokenPanel.transform, "Select Token", new Vector2(150f, 48f), btnSprite);
            var selectTokenRect = selectTokenButton.GetComponent<RectTransform>();
            selectTokenRect.anchorMin = new Vector2(0f, 0f); selectTokenRect.anchorMax = new Vector2(0f, 0f);
            selectTokenRect.pivot = new Vector2(0f, 0f);
            selectTokenRect.anchoredPosition = new Vector2(20f, 26f);

            var createTokenButton = CreateStyledButton("Create Token Button", tokenPanel.transform, "Create Token", new Vector2(150f, 48f), btnSprite);
            var createTokenRect = createTokenButton.GetComponent<RectTransform>();
            createTokenRect.anchorMin = new Vector2(1f, 0f); createTokenRect.anchorMax = new Vector2(1f, 0f);
            createTokenRect.pivot = new Vector2(1f, 0f);
            createTokenRect.anchoredPosition = new Vector2(-20f, 26f);

            var tokenLabel = CreateText("Token Label", tokenPanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            var tokenLabelRect = tokenLabel.GetComponent<RectTransform>();
            tokenLabelRect.anchorMin = new Vector2(0f, 0f); tokenLabelRect.anchorMax = new Vector2(1f, 0f);
            tokenLabelRect.offsetMin = new Vector2(10f, 85f); tokenLabelRect.offsetMax = new Vector2(-10f, 120f);
            tokenLabel.text = "Фишка не выбрана";

            // ──── Populate Tab 2: Stats ────
            var classInput = CreateInput("Class Input", statsTabPanel.transform, "Класс", false);
            var classRect = classInput.GetComponent<RectTransform>();
            classRect.anchorMin = new Vector2(0f, 1f); classRect.anchorMax = new Vector2(0f, 1f);
            classRect.pivot = new Vector2(0f, 1f);
            classRect.anchoredPosition = new Vector2(40f, -40f);
            classRect.sizeDelta = new Vector2(160f, 36f);

            var levelInput = CreateInput("Level Input", statsTabPanel.transform, "Ур", false);
            var lvlRect = levelInput.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0f, 1f); lvlRect.anchorMax = new Vector2(0f, 1f);
            lvlRect.pivot = new Vector2(0f, 1f);
            lvlRect.anchoredPosition = new Vector2(210f, -40f);
            lvlRect.sizeDelta = new Vector2(50f, 36f);

            var xpInput = CreateInput("XP Input", statsTabPanel.transform, "XP", false);
            var xpRect = xpInput.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0f, 1f); xpRect.anchorMax = new Vector2(0f, 1f);
            xpRect.pivot = new Vector2(0f, 1f);
            xpRect.anchoredPosition = new Vector2(270f, -40f);
            xpRect.sizeDelta = new Vector2(90f, 36f);

            var statsTitle = CreateText("Характеристики", statsTabPanel.transform, 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            var statsTitleRect = statsTitle.GetComponent<RectTransform>();
            statsTitleRect.anchorMin = new Vector2(0f, 1f); statsTitleRect.anchorMax = new Vector2(0f, 1f);
            statsTitleRect.pivot = new Vector2(0f, 1f);
            statsTitleRect.anchoredPosition = new Vector2(40f, -90f);
            statsTitleRect.sizeDelta = new Vector2(320f, 30f);

            var (strVal, strPlus, strMinus) = CreateStatField(statsTabPanel.transform, "Сила (STR)", -130f);
            var (agiVal, agiPlus, agiMinus) = CreateStatField(statsTabPanel.transform, "Ловкость (AGI)", -170f);
            var (intVal, intPlus, intMinus) = CreateStatField(statsTabPanel.transform, "Интеллект (INT)", -210f);
            var (holVal, holPlus, holMinus) = CreateStatField(statsTabPanel.transform, "Святость (HOL)", -250f);



            // HP/Armor and Weapons (Column 2 of Stats Tab)
            var hpInput = CreateInput("HP Input", statsTabPanel.transform, "HP (e.g. 10)", false);
            var hpRect = hpInput.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0f, 1f); hpRect.anchorMax = new Vector2(0f, 1f);
            hpRect.pivot = new Vector2(0f, 1f);
            hpRect.anchoredPosition = new Vector2(420f, -40f);
            hpRect.sizeDelta = new Vector2(150f, 36f);

            var armorInput = CreateInput("Armor Input", statsTabPanel.transform, "Броня (e.g. 5)", false);
            var armorRect = armorInput.GetComponent<RectTransform>();
            armorRect.anchorMin = new Vector2(0f, 1f); armorRect.anchorMax = new Vector2(0f, 1f);
            armorRect.pivot = new Vector2(0f, 1f);
            armorRect.anchoredPosition = new Vector2(590f, -40f);
            armorRect.sizeDelta = new Vector2(150f, 36f);

            // Weapon 1 details
            var weaponTitle = CreateText("Оружие 1", statsTabPanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            var weaponTitleRect = weaponTitle.GetComponent<RectTransform>();
            weaponTitleRect.anchorMin = new Vector2(0f, 1f); weaponTitleRect.anchorMax = new Vector2(0f, 1f);
            weaponTitleRect.pivot = new Vector2(0f, 1f);
            weaponTitleRect.anchoredPosition = new Vector2(420f, -90f);
            weaponTitleRect.sizeDelta = new Vector2(320f, 24f);

            var weapon1NameLabel = CreateText("Weapon 1 Name Label", statsTabPanel.transform, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            var wp1NameRect = weapon1NameLabel.GetComponent<RectTransform>();
            wp1NameRect.anchorMin = new Vector2(0f, 1f); wp1NameRect.anchorMax = new Vector2(0f, 1f);
            wp1NameRect.pivot = new Vector2(0f, 1f);
            wp1NameRect.anchoredPosition = new Vector2(420f, -114f);
            wp1NameRect.sizeDelta = new Vector2(320f, 24f);
            weapon1NameLabel.text = "Название: Нет оружия 1";
            weapon1NameLabel.color = new Color(0.95f, 0.85f, 0.7f, 1f);

            var weapon1ScalingLabel = CreateText("Weapon 1 Scaling Label", statsTabPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            var wp1ScaleRect = weapon1ScalingLabel.GetComponent<RectTransform>();
            wp1ScaleRect.anchorMin = new Vector2(0f, 1f); wp1ScaleRect.anchorMax = new Vector2(0f, 1f);
            wp1ScaleRect.pivot = new Vector2(0f, 1f);
            wp1ScaleRect.anchoredPosition = new Vector2(420f, -138f);
            wp1ScaleRect.sizeDelta = new Vector2(320f, 24f);
            weapon1ScalingLabel.text = "Скейлинг: -";
            weapon1ScalingLabel.color = new Color(0.85f, 0.8f, 0.75f, 1f);

            var weapon1AttributesLabel = CreateText("Weapon 1 Attributes Label", statsTabPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            var wp1AttrRect = weapon1AttributesLabel.GetComponent<RectTransform>();
            wp1AttrRect.anchorMin = new Vector2(0f, 1f); wp1AttrRect.anchorMax = new Vector2(0f, 1f);
            wp1AttrRect.pivot = new Vector2(0f, 1f);
            wp1AttrRect.anchoredPosition = new Vector2(420f, -162f);
            wp1AttrRect.sizeDelta = new Vector2(320f, 48f);
            weapon1AttributesLabel.text = "Свойства: -";
            weapon1AttributesLabel.color = new Color(0.85f, 0.8f, 0.75f, 1f);

            // Weapon 2 details
            var weapon2Title = CreateText("Оружие 2", statsTabPanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            var weapon2TitleRect = weapon2Title.GetComponent<RectTransform>();
            weapon2TitleRect.anchorMin = new Vector2(0f, 1f); weapon2TitleRect.anchorMax = new Vector2(0f, 1f);
            weapon2TitleRect.pivot = new Vector2(0f, 1f);
            weapon2TitleRect.anchoredPosition = new Vector2(420f, -215f);
            weapon2TitleRect.sizeDelta = new Vector2(320f, 24f);

            var weapon2NameLabel = CreateText("Weapon 2 Name Label", statsTabPanel.transform, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            var wp2NameRect = weapon2NameLabel.GetComponent<RectTransform>();
            wp2NameRect.anchorMin = new Vector2(0f, 1f); wp2NameRect.anchorMax = new Vector2(0f, 1f);
            wp2NameRect.pivot = new Vector2(0f, 1f);
            wp2NameRect.anchoredPosition = new Vector2(420f, -239f);
            wp2NameRect.sizeDelta = new Vector2(320f, 24f);
            weapon2NameLabel.text = "Название: Нет оружия 2";
            weapon2NameLabel.color = new Color(0.95f, 0.85f, 0.7f, 1f);

            var weapon2ScalingLabel = CreateText("Weapon 2 Scaling Label", statsTabPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            var wp2ScaleRect = weapon2ScalingLabel.GetComponent<RectTransform>();
            wp2ScaleRect.anchorMin = new Vector2(0f, 1f); wp2ScaleRect.anchorMax = new Vector2(0f, 1f);
            wp2ScaleRect.pivot = new Vector2(0f, 1f);
            wp2ScaleRect.anchoredPosition = new Vector2(420f, -263f);
            wp2ScaleRect.sizeDelta = new Vector2(320f, 24f);
            weapon2ScalingLabel.text = "Скейлинг: -";
            weapon2ScalingLabel.color = new Color(0.85f, 0.8f, 0.75f, 1f);

            var weapon2AttributesLabel = CreateText("Weapon 2 Attributes Label", statsTabPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            var wp2AttrRect = weapon2AttributesLabel.GetComponent<RectTransform>();
            wp2AttrRect.anchorMin = new Vector2(0f, 1f); wp2AttrRect.anchorMax = new Vector2(0f, 1f);
            wp2AttrRect.pivot = new Vector2(0f, 1f);
            wp2AttrRect.anchoredPosition = new Vector2(420f, -287f);
            wp2AttrRect.sizeDelta = new Vector2(320f, 48f);
            weapon2AttributesLabel.text = "Свойства: -";
            weapon2AttributesLabel.color = new Color(0.85f, 0.8f, 0.75f, 1f);

            // D6 slots columns in Stats Tab (Column 3)
            var attackInputs = CreateSlotColumn(statsTabPanel.transform, "⚔1", 180f, slotSprite);
            var attack2Inputs = CreateSlotColumn(statsTabPanel.transform, "⚔2", 300f, slotSprite);
            var defenseInputs = CreateSlotColumn(statsTabPanel.transform, "Щит", 420f, slotSprite);

            // Bottom abilities table - CHILD OF statsTabPanel!
            var bottomPanel = CreatePanel("Abilities Panel", statsTabPanel.transform, new Color(0.075f, 0.068f, 0.058f, 0.98f));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.anchoredPosition = new Vector2(0f, 20f);
            bottomRect.sizeDelta = new Vector2(-80f, 210f); // 210f height fits exactly 3 rows of 64x64

            var abilitiesRoot = new GameObject("Abilities Content", typeof(RectTransform));
            abilitiesRoot.transform.SetParent(bottomPanel.transform, false);
            var abilitiesRect = abilitiesRoot.GetComponent<RectTransform>();
            abilitiesRect.anchorMin = Vector2.zero;
            abilitiesRect.anchorMax = Vector2.one;
            abilitiesRect.offsetMin = Vector2.zero;
            abilitiesRect.offsetMax = Vector2.zero;

            // 10x3 bank grid layout group
            var grid = abilitiesRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64f, 64f);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 3;

            // Initialize Character Editor Canvas controller with all elements
            var controller = canvasObject.AddComponent<CharacterEditorController>();
            controller.Initialize(
                nameInput, descriptionInput, portraitImage, tokenLabel, hpInput,
                attackInputs, attack2Inputs, defenseInputs, abilitiesRect,

                classInput, levelInput, xpInput, armorInput,
                strVal, agiVal, intVal, holVal,
                strPlus, strMinus, agiPlus, agiMinus, intPlus, intMinus, holPlus, holMinus,

                weapon1NameLabel, weapon1ScalingLabel, weapon1AttributesLabel,
                weapon2NameLabel, weapon2ScalingLabel, weapon2AttributesLabel,

                charTabBtn, statsTabBtn,
                charTabPanel, statsTabPanel,
                tokenPortraitImg, tokenFrameImg,

                eqHelmet, eqArmor, eqWeapon, eqWeapon2, eqShield,
                eqBoots, eqAmulet, eqRing, eqArtifact,
                eqBelt, backpackFields, null,
                frameSprite, dialogBgSprite
            );

            backButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Back);
            saveButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Save);
            openButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Open);
            portraitObject.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.ImportPortrait);
            selectTokenButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.SelectToken);
            createTokenButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.CreateToken);

            // Create Tooltip UI object inside Canvas
            var tooltipObj = new GameObject("Item Tooltip", typeof(RectTransform));
            tooltipObj.transform.SetParent(canvasObject.transform, false);
            var tooltipRt = tooltipObj.GetComponent<RectTransform>();
            tooltipRt.anchorMin = Vector2.zero;
            tooltipRt.anchorMax = Vector2.zero;
            tooltipRt.pivot = new Vector2(0f, 1f); // Top-left pivot
            tooltipRt.sizeDelta = new Vector2(240f, 120f);

            var tooltipBg = tooltipObj.AddComponent<Image>();
            tooltipBg.color = new Color(0.045f, 0.041f, 0.037f, 0.98f);
            tooltipBg.sprite = barSprite; // Styling border matches bar asset

            var tooltipTextObj = new GameObject("Text", typeof(RectTransform));
            tooltipTextObj.transform.SetParent(tooltipObj.transform, false);
            var textRt = tooltipTextObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 12f);
            textRt.offsetMax = new Vector2(-12f, -12f);

            var tooltipText = tooltipTextObj.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipText.fontSize = 13;
            tooltipText.color = Color.white;
            tooltipText.alignment = TextAnchor.UpperLeft;
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipText.supportRichText = true;

            tooltipObj.AddComponent<ItemTooltip>();

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreateStyledButton(string name, Transform parent, string label, Vector2 size, Sprite normalSprite)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = size;
            var image = root.AddComponent<Image>();
            image.sprite = normalSprite;
            image.color = Color.white;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Label", root.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.text = label;
            return root;
        }

        private static Text CreateText(string name, Transform parent, int size, FontStyle style, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static InputField CreateInput(string name, Transform parent, string placeholder, bool multiline)
        {
            var inputObject = new GameObject(name, typeof(RectTransform));
            inputObject.transform.SetParent(parent, false);
            inputObject.AddComponent<Image>().color = new Color(0.04f, 0.038f, 0.034f, 0.95f);
            var input = inputObject.AddComponent<InputField>();
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;

            var text = CreateText("Text", inputObject.transform, 20, FontStyle.Normal, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
            text.supportRichText = false;

            var placeholderText = CreateText("Placeholder", inputObject.transform, 20, FontStyle.Normal, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            placeholderText.text = placeholder;
            placeholderText.color = new Color(1f, 1f, 1f, 0.38f);
            var placeholderRect = placeholderText.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 8f);
            placeholderRect.offsetMax = new Vector2(-12f, -8f);

            input.textComponent = text;
            input.placeholder = placeholderText;
            return input;
        }

        private static InputField CreateItemSlot(string name, Transform parent, string placeholder, Sprite slotBg)
        {
            var slotObj = new GameObject(name, typeof(RectTransform));
            slotObj.transform.SetParent(parent, false);
            slotObj.GetComponent<RectTransform>().sizeDelta = new Vector2(96f, 96f);
            
            var image = slotObj.AddComponent<Image>();
            image.sprite = slotBg;
            image.color = Color.white;

            // Centered icon layer filling the slot
            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10f, 10f);
            iconRect.offsetMax = new Vector2(-10f, -10f);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.clear;
            iconImg.raycastTarget = false;
            
            var input = slotObj.AddComponent<InputField>();
            input.lineType = InputField.LineType.SingleLine;

            // Transparent text component
            var text = CreateText("Text", slotObj.transform, 11, FontStyle.Normal, TextAnchor.MiddleCenter);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.color = Color.clear; // Fully transparent!

            // Silhouette placeholder text in the center
            var placeholderText = CreateText("Placeholder", slotObj.transform, 13, FontStyle.Italic, TextAnchor.MiddleCenter);
            placeholderText.text = placeholder;
            placeholderText.color = new Color(1f, 1f, 1f, 0.25f);
            var placeholderRect = placeholderText.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            input.textComponent = text;
            input.placeholder = placeholderText;

            var dropSlot = slotObj.AddComponent<ItemDropSlot>();
            dropSlot.inputField = input;
            dropSlot.slotIcon = iconImg;

            // Hover Tooltip Trigger
            var tooltipTrigger = slotObj.AddComponent<ItemTooltipTrigger>();
            tooltipTrigger.boundInputField = input;

            return input;
        }

        private static void PositionTop(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void PositionCenter(RectTransform rect, float x, float y)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void Stretch(GameObject gameObject)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.052f, 0.047f);
        }

        private static AbilityDropSlot[] CreateSlotColumn(Transform parent, string title, float x, Sprite slotBg)
        {
            var root = new GameObject(title + " Column", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -20f);
            rect.sizeDelta = new Vector2(104f, 570f); // 104 width, 570 height
            root.AddComponent<Image>().color = new Color(0.055f, 0.05f, 0.044f, 0.65f);

            var titleText = CreateText(title, root.transform, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(0f, -48f);
            titleRect.offsetMax = Vector2.zero;

            var slots = new AbilityDropSlot[6];

            for (var i = 0; i < slots.Length; i++)
            {
                var slotObj = new GameObject($"Slot {i + 1}", typeof(RectTransform));
                slotObj.transform.SetParent(root.transform, false);
                var slotRect = slotObj.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 1f);
                slotRect.anchorMax = new Vector2(1f, 1f);
                slotRect.offsetMin = new Vector2(12f, -130f - i * 86f);
                slotRect.offsetMax = new Vector2(-12f, -50f - i * 86f);

                // Slot border/background frame
                var bg = slotObj.AddComponent<Image>();
                bg.sprite = slotBg;
                bg.color = Color.white;

                // Center icon overlay
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(slotObj.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(6f, 6f);
                iconRect.offsetMax = new Vector2(-6f, -6f);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.color = Color.clear;
                iconImg.raycastTarget = false;

                // Invisible InputField component for saving/loading bindings
                var input = slotObj.AddComponent<InputField>();
                input.lineType = InputField.LineType.SingleLine;

                // Transparent text component
                var labelObj = new GameObject("Text", typeof(RectTransform));
                labelObj.transform.SetParent(slotObj.transform, false);
                var labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var text = labelObj.AddComponent<Text>();
                text.text = "";
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.color = Color.clear; // Fully transparent!

                // Face number placeholder shown in faint grey when empty
                var placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
                placeholderObj.transform.SetParent(slotObj.transform, false);
                var placeholderRect = placeholderObj.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.offsetMin = Vector2.zero;
                placeholderRect.offsetMax = Vector2.zero;

                var placeholderText = placeholderObj.AddComponent<Text>();
                placeholderText.text = (i + 1).ToString();
                placeholderText.alignment = TextAnchor.MiddleCenter;
                placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                placeholderText.fontStyle = FontStyle.Bold;
                placeholderText.fontSize = 24;
                placeholderText.color = new Color(1f, 1f, 1f, 0.15f); // bld-faint placeholder

                input.textComponent = text;
                input.placeholder = placeholderText;

                var dropSlot = slotObj.AddComponent<AbilityDropSlot>();
                dropSlot.labelText = placeholderText; // Bind placeholderText so it controls face numbers
                dropSlot.slotImage = iconImg;
                dropSlot.abilityName = "";
                dropSlot.diceFaceNumber = (i + 1).ToString();

                // Bind tooltip trigger
                var trigger = slotObj.AddComponent<ItemTooltipTrigger>();
                trigger.boundAbilitySlot = dropSlot;

                slots[i] = dropSlot;
            }

            return slots;
        }

        private static (Text valueLabel, Button plus, Button minus) CreateStatField(Transform parent, string labelText, float y)
        {
            var row = new GameObject(labelText + " Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(40f, y);
            rowRt.sizeDelta = new Vector2(320f, 36f);

            var nameText = CreateText("Name", row.transform, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            var nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 0.5f);
            nameRt.anchoredPosition = new Vector2(0f, 0f);
            nameRt.sizeDelta = new Vector2(150f, 36f);
            nameText.text = labelText;

            // Load button sprite for stat modifiers
            var btnSprite = GetGuiSprite("button2.png");

            var minusObj = CreateStyledButton("Minus", row.transform, "-", new Vector2(30f, 30f), btnSprite);
            var minusRt = minusObj.GetComponent<RectTransform>();
            minusRt.anchorMin = new Vector2(0f, 0.5f); minusRt.anchorMax = new Vector2(0f, 0.5f);
            minusRt.pivot = new Vector2(0f, 0.5f);
            minusRt.anchoredPosition = new Vector2(160f, 0f);
            var minusBtn = minusObj.GetComponent<Button>();

            var valueText = CreateText("Value", row.transform, 17, FontStyle.Normal, TextAnchor.MiddleCenter);
            var valueRt = valueText.GetComponent<RectTransform>();
            valueRt.anchorMin = new Vector2(0f, 0f); valueRt.anchorMax = new Vector2(0f, 1f);
            valueRt.pivot = new Vector2(0f, 0.5f);
            valueRt.anchoredPosition = new Vector2(200f, 0f);
            valueRt.sizeDelta = new Vector2(40f, 36f);

            var plusObj = CreateStyledButton("Plus", row.transform, "+", new Vector2(30f, 30f), btnSprite);
            var plusRt = plusObj.GetComponent<RectTransform>();
            plusRt.anchorMin = new Vector2(0f, 0.5f); plusRt.anchorMax = new Vector2(0f, 0.5f);
            plusRt.pivot = new Vector2(0f, 0.5f);
            plusRt.anchoredPosition = new Vector2(250f, 0f);
            var plusBtn = plusObj.GetComponent<Button>();

            return (valueText, plusBtn, minusBtn);
        }

        private static Toggle CreateToggle(Transform parent, string label)
        {
            var root = new GameObject(label, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var toggle = root.AddComponent<Toggle>();

            var box = CreatePanel("Box", root.transform, new Color(0.12f, 0.105f, 0.085f, 1f));
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(0f, 0f);
            boxRect.sizeDelta = new Vector2(28f, 28f);
            toggle.targetGraphic = box.GetComponent<Image>();

            var check = CreatePanel("Checkmark", box.transform, new Color(1f, 0.78f, 0.22f, 1f));
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero; checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(6f, 6f); checkRect.offsetMax = new Vector2(-6f, -6f);
            toggle.graphic = check.GetComponent<Image>();

            var text = CreateText("Label", root.transform, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(38f, 0f);
            textRect.offsetMax = Vector2.zero;
            text.text = label;
            return toggle;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.48f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "CircleMaskSprite";
            return sprite;
        }
    }
}
