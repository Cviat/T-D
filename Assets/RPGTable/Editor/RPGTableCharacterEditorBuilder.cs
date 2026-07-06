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

            var eqWeapon = CreateItemSlot("Slot Weapon", charTabPanel.transform, "Оружие", slotSprite);
            eqWeapon.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Weapon;
            PositionCenter(eqWeapon.GetComponent<RectTransform>(), 120f, -280f);

            var eqArtifact = CreateItemSlot("Slot Artifact", charTabPanel.transform, "Артефакт", slotSprite);
            eqArtifact.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Artifact;
            PositionCenter(eqArtifact.GetComponent<RectTransform>(), 120f, -410f);

            var eqRing = CreateItemSlot("Slot Ring", charTabPanel.transform, "Кольцо", slotSprite);
            eqRing.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Ring;
            PositionCenter(eqRing.GetComponent<RectTransform>(), 560f, -150f);

            var eqShield = CreateItemSlot("Slot Shield", charTabPanel.transform, "Щит", slotSprite);
            eqShield.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Shield;
            PositionCenter(eqShield.GetComponent<RectTransform>(), 560f, -280f);

            var eqBelt = CreateItemSlot("Slot Belt", charTabPanel.transform, "Пояс", slotSprite);
            eqBelt.GetComponent<ItemDropSlot>().slotType = RPGTable.Core.ItemType.Belt;
            PositionCenter(eqBelt.GetComponent<RectTransform>(), 560f, -410f);

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

            var statTogglesRoot = new GameObject("Type Toggles", typeof(RectTransform));
            statTogglesRoot.transform.SetParent(statsTabPanel.transform, false);
            var togglesRect = statTogglesRoot.GetComponent<RectTransform>();
            togglesRect.anchorMin = new Vector2(0f, 1f); togglesRect.anchorMax = new Vector2(0f, 1f);
            togglesRect.pivot = new Vector2(0f, 1f);
            togglesRect.anchoredPosition = new Vector2(40f, -310f);
            togglesRect.sizeDelta = new Vector2(320f, 120f);
            var toggleLayout = statTogglesRoot.AddComponent<GridLayoutGroup>();
            toggleLayout.cellSize = new Vector2(150f, 36f);
            toggleLayout.spacing = new Vector2(12f, 8f);
            var meleeToggle = CreateToggle(statTogglesRoot.transform, "Рукопашная");
            var magicToggle = CreateToggle(statTogglesRoot.transform, "Магия");
            var rangedToggle = CreateToggle(statTogglesRoot.transform, "Дальний бой");
            var doubleToggle = CreateToggle(statTogglesRoot.transform, "Двойной урон");

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

            var weaponTitle = CreateText("Оружие", statsTabPanel.transform, 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            var weaponTitleRect = weaponTitle.GetComponent<RectTransform>();
            weaponTitleRect.anchorMin = new Vector2(0f, 1f); weaponTitleRect.anchorMax = new Vector2(0f, 1f);
            weaponTitleRect.pivot = new Vector2(0f, 1f);
            weaponTitleRect.anchoredPosition = new Vector2(420f, -90f);
            weaponTitleRect.sizeDelta = new Vector2(320f, 30f);

            var weaponNameInput = CreateInput("Weapon Name Input", statsTabPanel.transform, "Название оружия", false);
            var wpNameRect = weaponNameInput.GetComponent<RectTransform>();
            wpNameRect.anchorMin = new Vector2(0f, 1f); wpNameRect.anchorMax = new Vector2(0f, 1f);
            wpNameRect.pivot = new Vector2(0f, 1f);
            wpNameRect.anchoredPosition = new Vector2(420f, -130f);
            wpNameRect.sizeDelta = new Vector2(320f, 36f);

            var scaleStat1Obj = CreateStyledButton("Scale Stat 1 Button", statsTabPanel.transform, "None", new Vector2(150f, 36f), btnSprite);
            var scale1Rt = scaleStat1Obj.GetComponent<RectTransform>();
            scale1Rt.anchorMin = new Vector2(0f, 1f); scale1Rt.anchorMax = new Vector2(0f, 1f);
            scale1Rt.pivot = new Vector2(0f, 1f);
            scale1Rt.anchoredPosition = new Vector2(420f, -180f);
            var scaleStat1Text = scaleStat1Obj.GetComponentInChildren<Text>();
            var scaleStat1Btn = scaleStat1Obj.GetComponent<Button>();

            var weaponCoef1Input = CreateInput("Weapon Coef 1 Input", statsTabPanel.transform, "0.6", false);
            var coef1Rt = weaponCoef1Input.GetComponent<RectTransform>();
            coef1Rt.anchorMin = new Vector2(0f, 1f); coef1Rt.anchorMax = new Vector2(0f, 1f);
            coef1Rt.pivot = new Vector2(0f, 1f);
            coef1Rt.anchoredPosition = new Vector2(590f, -180f);
            coef1Rt.sizeDelta = new Vector2(150f, 36f);

            var scaleStat2Obj = CreateStyledButton("Scale Stat 2 Button", statsTabPanel.transform, "None", new Vector2(150f, 36f), btnSprite);
            var scale2Rt = scaleStat2Obj.GetComponent<RectTransform>();
            scale2Rt.anchorMin = new Vector2(0f, 1f); scale2Rt.anchorMax = new Vector2(0f, 1f);
            scale2Rt.pivot = new Vector2(0f, 1f);
            scale2Rt.anchoredPosition = new Vector2(420f, -230f);
            var scaleStat2Text = scaleStat2Obj.GetComponentInChildren<Text>();
            var scaleStat2Btn = scaleStat2Obj.GetComponent<Button>();

            var weaponCoef2Input = CreateInput("Weapon Coef 2 Input", statsTabPanel.transform, "0.0", false);
            var coef2Rt = weaponCoef2Input.GetComponent<RectTransform>();
            coef2Rt.anchorMin = new Vector2(0f, 1f); coef2Rt.anchorMax = new Vector2(0f, 1f);
            coef2Rt.pivot = new Vector2(0f, 1f);
            coef2Rt.anchoredPosition = new Vector2(590f, -230f);
            coef2Rt.sizeDelta = new Vector2(150f, 36f);

            var weaponAttributeInput = CreateInput("Weapon Attribute Input", statsTabPanel.transform, "Атрибут оружия", false);
            var wpAttrRt = weaponAttributeInput.GetComponent<RectTransform>();
            wpAttrRt.anchorMin = new Vector2(0f, 1f); wpAttrRt.anchorMax = new Vector2(0f, 1f);
            wpAttrRt.pivot = new Vector2(0f, 1f);
            wpAttrRt.anchoredPosition = new Vector2(420f, -280f);
            wpAttrRt.sizeDelta = new Vector2(320f, 36f);

            // D6 slots columns in Stats Tab (Column 3)
            var attackInputs = CreateSlotColumn(statsTabPanel.transform, "⚔", 180f);
            var defenseInputs = CreateSlotColumn(statsTabPanel.transform, "Щит", 480f);

            // Bottom abilities table - CHILD OF statsTabPanel!
            var bottomPanel = CreatePanel("Abilities Panel", statsTabPanel.transform, new Color(0.075f, 0.068f, 0.058f, 0.98f));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.offsetMin = new Vector2(40f, 20f);
            bottomRect.offsetMax = new Vector2(-40f, 170f);

            var abilitiesRoot = new GameObject("Abilities Content", typeof(RectTransform));
            abilitiesRoot.transform.SetParent(bottomPanel.transform, false);
            var abilitiesRect = abilitiesRoot.GetComponent<RectTransform>();
            abilitiesRect.anchorMin = new Vector2(0f, 0f);
            abilitiesRect.anchorMax = new Vector2(1f, 1f);
            abilitiesRect.offsetMin = new Vector2(18f, 18f);
            abilitiesRect.offsetMax = new Vector2(-150f, -18f);
            var abilitiesLayout = abilitiesRoot.AddComponent<HorizontalLayoutGroup>();
            abilitiesLayout.spacing = 12f;
            abilitiesLayout.childControlWidth = false;
            abilitiesLayout.childControlHeight = false;

            var addAbilityButton = CreateStyledButton("Add Ability Button", bottomPanel.transform, "+", new Vector2(108f, 108f), btnSprite);
            var addAbilityRect = addAbilityButton.GetComponent<RectTransform>();
            addAbilityRect.anchorMin = new Vector2(1f, 0.5f);
            addAbilityRect.anchorMax = new Vector2(1f, 0.5f);
            addAbilityRect.pivot = new Vector2(1f, 0.5f);
            addAbilityRect.anchoredPosition = new Vector2(-20f, 0f);

            // Initialize Character Editor Canvas controller with all elements
            var controller = canvasObject.AddComponent<CharacterEditorController>();
            controller.Initialize(
                nameInput, descriptionInput, portraitImage, tokenLabel, hpInput,
                attackInputs, defenseInputs, meleeToggle, magicToggle, rangedToggle, doubleToggle, abilitiesRect,

                classInput, levelInput, xpInput, armorInput,
                strVal, agiVal, intVal, holVal,
                strPlus, strMinus, agiPlus, agiMinus, intPlus, intMinus, holPlus, holMinus,

                weaponNameInput, weaponCoef1Input, weaponCoef2Input, weaponAttributeInput,
                scaleStat1Text, scaleStat1Btn,
                scaleStat2Text, scaleStat2Btn,

                charTabBtn, statsTabBtn,
                charTabPanel, statsTabPanel,
                tokenPortraitImg, tokenFrameImg,

                eqHelmet, eqArmor, eqWeapon, eqShield,
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
            addAbilityButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.AddAbility);

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

        private static AbilityDropSlot[] CreateSlotColumn(Transform parent, string title, float x)
        {
            var root = new GameObject(title + " Column", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -40f);
            rect.sizeDelta = new Vector2(250f, 430f);
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
                slotRect.offsetMin = new Vector2(12f, -96f - i * 54f);
                slotRect.offsetMax = new Vector2(-12f, -54f - i * 54f);

                var bg = slotObj.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.105f, 0.085f, 1f);

                var labelObj = new GameObject("Label", typeof(RectTransform));
                labelObj.transform.SetParent(slotObj.transform, false);
                var labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(5f, 5f);
                labelRect.offsetMax = new Vector2(-5f, -5f);

                var text = labelObj.AddComponent<Text>();
                text.text = $"{i + 1}. Пусто";
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontStyle = FontStyle.Normal;
                text.fontSize = 14;
                text.color = Color.white;

                var dropSlot = slotObj.AddComponent<AbilityDropSlot>();
                dropSlot.labelText = text;
                dropSlot.slotImage = bg;
                dropSlot.abilityName = "";
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
