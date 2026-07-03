using System.IO;
using RPGTable.TokenEditor;
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
    public static class RPGTableTokenEditorBuilder
    {
        public const string ScenePath = "Assets/RPGTable/Scenes/TokenEditor.unity";
        private static readonly string[] FramePaths =
        {
            "Assets/RPGTable/Art/TokenFrames/Frame_01.png",
            "Assets/RPGTable/Art/TokenFrames/Frame_02.png",
            "Assets/RPGTable/Art/TokenFrames/Frame_03.png",
            "Assets/RPGTable/Art/TokenFrames/Frame_04.png",
            "Assets/RPGTable/Art/TokenFrames/Frame_05.png",
            "Assets/RPGTable/Art/TokenFrames/Frame_06.png"
        };

        [MenuItem("RPG Table/Build Token Editor Scene")]
        public static void BuildTokenEditorScene()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");
            var preservedMaskLayout = CaptureRectLayout("Portrait Mask");
            ConfigureFrames();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "TokenEditor";

            CreateCamera();
            var canvasObject = new GameObject("Token Editor Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.055f, 0.052f, 0.047f, 1f));
            Stretch(root);

            var leftPanel = CreatePanel("Controls Panel", root.transform, new Color(0.07f, 0.064f, 0.055f, 0.98f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.sizeDelta = new Vector2(300f, 0f);
            leftRect.pivot = new Vector2(0f, 0.5f);

            var backButton = CreateTextButton("Back Button", leftPanel.transform, "Back", new Vector2(260f, 48f));
            PositionTop(backButton.GetComponent<RectTransform>(), -18f);
            var saveButton = CreateTextButton("Save Token Button", leftPanel.transform, "Save Token", new Vector2(260f, 48f));
            PositionTop(saveButton.GetComponent<RectTransform>(), -76f);
            var openButton = CreateTextButton("Open Token Button", leftPanel.transform, "Import Token", new Vector2(260f, 48f));
            PositionTop(openButton.GetComponent<RectTransform>(), -134f);
            var portraitButton = CreateTextButton("Import Portrait Button", leftPanel.transform, "Import Portrait", new Vector2(260f, 48f));
            PositionTop(portraitButton.GetComponent<RectTransform>(), -192f);

            var frameGrid = new GameObject("Frame Options", typeof(RectTransform));
            frameGrid.transform.SetParent(leftPanel.transform, false);
            var frameGridRect = frameGrid.GetComponent<RectTransform>();
            frameGridRect.anchorMin = new Vector2(0.5f, 1f);
            frameGridRect.anchorMax = new Vector2(0.5f, 1f);
            frameGridRect.pivot = new Vector2(0.5f, 1f);
            frameGridRect.anchoredPosition = new Vector2(0f, -260f);
            frameGridRect.sizeDelta = new Vector2(260f, 260f);
            var grid = frameGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(116f, 116f);
            grid.spacing = new Vector2(12f, 12f);

            var centerPanel = CreatePanel("Portrait Panel", root.transform, new Color(0.06f, 0.056f, 0.05f, 1f));
            var centerRect = centerPanel.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0f, 0f);
            centerRect.anchorMax = new Vector2(1f, 1f);
            centerRect.offsetMin = new Vector2(320f, 190f);
            centerRect.offsetMax = new Vector2(-610f, -30f);

            var footprintButtonsRoot = new GameObject("Footprint Size Buttons", typeof(RectTransform));
            footprintButtonsRoot.transform.SetParent(centerPanel.transform, false);
            var footprintButtonsRect = footprintButtonsRoot.GetComponent<RectTransform>();
            footprintButtonsRect.anchorMin = new Vector2(0.5f, 1f);
            footprintButtonsRect.anchorMax = new Vector2(0.5f, 1f);
            footprintButtonsRect.pivot = new Vector2(0.5f, 1f);
            footprintButtonsRect.anchoredPosition = new Vector2(0f, -22f);
            footprintButtonsRect.sizeDelta = new Vector2(500f, 48f);
            var footprintLayout = footprintButtonsRoot.AddComponent<HorizontalLayoutGroup>();
            footprintLayout.spacing = 10f;
            footprintLayout.childControlWidth = false;
            footprintLayout.childControlHeight = false;
            footprintLayout.childAlignment = TextAnchor.MiddleCenter;

            var footprintLabel = CreateText("Footprint Label", centerPanel.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            var footprintLabelRect = footprintLabel.GetComponent<RectTransform>();
            footprintLabelRect.anchorMin = new Vector2(0.5f, 1f);
            footprintLabelRect.anchorMax = new Vector2(0.5f, 1f);
            footprintLabelRect.pivot = new Vector2(0.5f, 1f);
            footprintLabelRect.anchoredPosition = new Vector2(0f, -78f);
            footprintLabelRect.sizeDelta = new Vector2(220f, 34f);

            var portraitRoot = new GameObject("Token Portrait Root", typeof(RectTransform));
            portraitRoot.transform.SetParent(centerPanel.transform, false);
            var portraitRootRect = portraitRoot.GetComponent<RectTransform>();
            portraitRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRootRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRootRect.anchoredPosition = new Vector2(0f, -24f);
            portraitRootRect.sizeDelta = new Vector2(560f, 560f);

            var maskObject = new GameObject("Portrait Mask", typeof(RectTransform));
            maskObject.transform.SetParent(portraitRoot.transform, false);
            var maskRect = maskObject.GetComponent<RectTransform>();
            ApplyRectLayout(
                maskRect,
                preservedMaskLayout,
                new RectLayout
                {
                    hasValue = true,
                    anchorMin = new Vector2(0.5f, 0.5f),
                    anchorMax = new Vector2(0.5f, 0.5f),
                    pivot = new Vector2(0.5f, 0.5f),
                    anchoredPosition = new Vector2(0f, 44f),
                    sizeDelta = new Vector2(360f, 360f)
                });
            var maskImage = maskObject.AddComponent<Image>();
            maskImage.sprite = CreateCircleSprite();
            maskImage.color = Color.white;
            var mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var portraitObject = new GameObject("Portrait", typeof(RectTransform));
            portraitObject.transform.SetParent(maskObject.transform, false);
            Stretch(portraitObject);
            var portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = new Color(0.12f, 0.11f, 0.1f, 1f);
            portraitImage.preserveAspect = true;

            var plusObject = new GameObject("Portrait Plus", typeof(RectTransform));
            plusObject.transform.SetParent(maskObject.transform, false);
            Stretch(plusObject);
            var plusText = plusObject.AddComponent<Text>();
            plusText.text = "+";
            plusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            plusText.fontStyle = FontStyle.Bold;
            plusText.fontSize = 118;
            plusText.alignment = TextAnchor.MiddleCenter;
            plusText.color = new Color(1f, 1f, 1f, 0.8f);

            var plusButton = maskObject.AddComponent<Button>();
            plusButton.targetGraphic = maskImage;

            var frameObject = new GameObject("Frame", typeof(RectTransform));
            frameObject.transform.SetParent(portraitRoot.transform, false);
            Stretch(frameObject);
            var frameImage = frameObject.AddComponent<Image>();
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;

            var rightPanel = CreatePanel("Stats Panel", root.transform, new Color(0.07f, 0.064f, 0.055f, 0.98f));
            var rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.pivot = new Vector2(1f, 0.5f);
            rightRect.sizeDelta = new Vector2(580f, 0f);

            var nameInput = CreateInput("Name Input", rightPanel.transform, "Token name", 22);
            PositionTop(nameInput.GetComponent<RectTransform>(), -18f, new Vector2(520f, 48f));
            var descriptionInput = CreateInput("Description Input", rightPanel.transform, "Description", 18, true);
            PositionTop(descriptionInput.GetComponent<RectTransform>(), -78f, new Vector2(520f, 126f));

            var togglesRoot = new GameObject("Type Toggles", typeof(RectTransform));
            togglesRoot.transform.SetParent(rightPanel.transform, false);
            var togglesRect = togglesRoot.GetComponent<RectTransform>();
            PositionTop(togglesRect, -224f, new Vector2(520f, 86f));
            var toggleLayout = togglesRoot.AddComponent<GridLayoutGroup>();
            toggleLayout.cellSize = new Vector2(250f, 36f);
            toggleLayout.spacing = new Vector2(12f, 8f);
            var meleeToggle = CreateToggle(togglesRoot.transform, "Рукопашная");
            var magicToggle = CreateToggle(togglesRoot.transform, "Магия");
            var rangedToggle = CreateToggle(togglesRoot.transform, "Дальний бой");
            var doubleToggle = CreateToggle(togglesRoot.transform, "Двойной урон");

            var attackInputs = CreateSlotColumn(rightPanel.transform, "⚔", -330f, true);
            var defenseInputs = CreateSlotColumn(rightPanel.transform, "Щит", -330f, false);

            var bottomPanel = CreatePanel("Abilities Panel", root.transform, new Color(0.075f, 0.068f, 0.058f, 0.98f));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.offsetMin = new Vector2(320f, 20f);
            bottomRect.offsetMax = new Vector2(-20f, 170f);

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

            var addAbilityButton = CreateTextButton("Add Ability Button", bottomPanel.transform, "+", new Vector2(108f, 108f));
            var addAbilityRect = addAbilityButton.GetComponent<RectTransform>();
            addAbilityRect.anchorMin = new Vector2(1f, 0.5f);
            addAbilityRect.anchorMax = new Vector2(1f, 0.5f);
            addAbilityRect.pivot = new Vector2(1f, 0.5f);
            addAbilityRect.anchoredPosition = new Vector2(-20f, 0f);

            var controller = canvasObject.AddComponent<TokenEditorController>();
            controller.Initialize(frameImage, portraitImage, plusText, footprintLabel, nameInput, descriptionInput, attackInputs, defenseInputs, meleeToggle, magicToggle, rangedToggle, doubleToggle, abilitiesRect);

            backButton.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.Back);
            saveButton.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.Save);
            openButton.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.Open);
            portraitButton.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.ImportPortrait);
            plusButton.gameObject.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.ImportPortrait);
            addAbilityButton.AddComponent<TokenEditorButton>().Initialize(controller, TokenEditorButtonAction.AddAbility);

            CreateFootprintButtons(footprintButtonsRoot.transform, controller);
            CreateFrameOptions(frameGrid.transform, controller, frameImage);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateFrameOptions(Transform parent, TokenEditorController controller, Image targetFrame)
        {
            for (var i = 0; i < FramePaths.Length; i++)
            {
                var path = FramePaths[i];
                var option = CreatePanel($"Frame {i + 1}", parent, new Color(0.12f, 0.1f, 0.085f, 1f));
                var button = option.AddComponent<Button>();
                var preview = option.GetComponent<Image>();
                preview.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                preview.color = Color.white;
                preview.preserveAspect = true;
                button.targetGraphic = preview;
                option.AddComponent<TokenFrameOptionButton>().Initialize(controller, path, preview);

                if (i == 0)
                {
                    controller.SetFrame(path, preview.sprite);
                }
            }
        }

        private static void CreateFootprintButtons(Transform parent, TokenEditorController controller)
        {
            for (var size = 1; size <= 5; size++)
            {
                var buttonObject = CreateTextButton($"{size}x{size} Button", parent, $"{size}x{size}", new Vector2(86f, 42f));
                buttonObject.AddComponent<TokenFootprintButton>().Initialize(controller, size);
            }
        }

        private static InputField[] CreateSlotColumn(Transform parent, string title, float y, bool left)
        {
            var root = new GameObject(left ? "Attack Column" : "Defense Column", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(left ? -138f : 138f, y);
            rect.sizeDelta = new Vector2(250f, 430f);
            root.AddComponent<Image>().color = new Color(0.055f, 0.05f, 0.044f, 0.65f);

            var titleText = CreateText(title, root.transform, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(0f, -48f);
            titleRect.offsetMax = Vector2.zero;

            var inputs = new InputField[6];

            for (var i = 0; i < inputs.Length; i++)
            {
                var input = CreateInput($"Slot {i + 1}", root.transform, $"{i + 1}", 16);
                var inputRect = input.GetComponent<RectTransform>();
                inputRect.anchorMin = new Vector2(0f, 1f);
                inputRect.anchorMax = new Vector2(1f, 1f);
                inputRect.offsetMin = new Vector2(12f, -96f - i * 54f);
                inputRect.offsetMax = new Vector2(-12f, -54f - i * 54f);
                inputs[i] = input;
            }

            return inputs;
        }

        private static InputField CreateInput(string name, Transform parent, string placeholder, int fontSize, bool multiline = false)
        {
            var inputObject = CreatePanel(name, parent, new Color(0.09f, 0.082f, 0.07f, 0.98f));
            var input = inputObject.AddComponent<InputField>();
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(inputObject.transform, false);
            Stretch(textObject, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform));
            placeholderObject.transform.SetParent(inputObject.transform, false);
            Stretch(placeholderObject, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            var placeholderText = placeholderObject.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = fontSize;
            placeholderText.color = new Color(1f, 1f, 1f, 0.38f);
            placeholderText.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            placeholderText.raycastTarget = false;

            input.textComponent = text;
            input.placeholder = placeholderText;
            return input;
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
            Stretch(check, new Vector2(6f, 6f), new Vector2(-6f, -6f));
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
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private struct RectLayout
        {
            public bool hasValue;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
        }

        private static RectLayout CaptureRectLayout(string objectName)
        {
            var gameObject = GameObject.Find(objectName);

            if (gameObject == null)
            {
                return default;
            }

            var rect = gameObject.GetComponent<RectTransform>();

            if (rect == null)
            {
                return default;
            }

            return new RectLayout
            {
                hasValue = true,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta
            };
        }

        private static void ApplyRectLayout(RectTransform rect, RectLayout preserved, RectLayout fallback)
        {
            var layout = preserved.hasValue ? preserved : fallback;
            rect.anchorMin = layout.anchorMin;
            rect.anchorMax = layout.anchorMax;
            rect.pivot = layout.pivot;
            rect.anchoredPosition = layout.anchoredPosition;
            rect.sizeDelta = layout.sizeDelta;
        }

        private static void ConfigureFrames()
        {
            foreach (var path in FramePaths)
            {
                ConfigureSpriteTexture(path);
            }
        }

        private static void ConfigureSpriteTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreateTextButton(string name, Transform parent, string label, Vector2 size)
        {
            var buttonObject = CreatePanel(name, parent, new Color(0.16f, 0.105f, 0.055f, 0.96f));
            buttonObject.GetComponent<RectTransform>().sizeDelta = size;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var text = CreateText("Label", buttonObject.transform, label == "+" ? 52 : 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.gameObject);
            return buttonObject;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
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

        private static void Stretch(GameObject gameObject)
        {
            Stretch(gameObject, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(GameObject gameObject, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void PositionTop(RectTransform rect, float y)
        {
            PositionTop(rect, y, rect.sizeDelta);
        }

        private static void PositionTop(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }
    }
}
