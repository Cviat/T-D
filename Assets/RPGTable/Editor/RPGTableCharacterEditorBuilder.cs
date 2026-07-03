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

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.055f, 0.052f, 0.047f, 1f));
            Stretch(root);

            var leftPanel = CreatePanel("Left Panel", root.transform, new Color(0.075f, 0.066f, 0.055f, 0.98f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.anchoredPosition = Vector2.zero;
            leftRect.sizeDelta = new Vector2(330f, 0f);

            var backButton = CreateTextButton("Back Button", leftPanel.transform, "Back", new Vector2(280f, 48f));
            PositionTop(backButton.GetComponent<RectTransform>(), -20f);
            var saveButton = CreateTextButton("Save Character Button", leftPanel.transform, "Save Character", new Vector2(280f, 48f));
            PositionTop(saveButton.GetComponent<RectTransform>(), -82f);
            var openButton = CreateTextButton("Open Character Button", leftPanel.transform, "Import Character", new Vector2(280f, 48f));
            PositionTop(openButton.GetComponent<RectTransform>(), -144f);
            var selectTokenButton = CreateTextButton("Select Token Button", leftPanel.transform, "Select Token", new Vector2(280f, 48f));
            PositionTop(selectTokenButton.GetComponent<RectTransform>(), -226f);
            var createTokenButton = CreateTextButton("Create Token Button", leftPanel.transform, "Create Token", new Vector2(280f, 48f));
            PositionTop(createTokenButton.GetComponent<RectTransform>(), -288f);

            var title = CreateText("Title", root.transform, 36, FontStyle.Bold, TextAnchor.MiddleLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(370f, -92f);
            titleRect.offsetMax = new Vector2(-40f, -28f);
            title.text = "Character Editor";

            var portraitPanel = CreatePanel("Portrait Panel", root.transform, new Color(0.08f, 0.074f, 0.064f, 0.98f));
            var portraitPanelRect = portraitPanel.GetComponent<RectTransform>();
            portraitPanelRect.anchorMin = new Vector2(0f, 0.5f);
            portraitPanelRect.anchorMax = new Vector2(0f, 0.5f);
            portraitPanelRect.pivot = new Vector2(0f, 0.5f);
            portraitPanelRect.anchoredPosition = new Vector2(370f, 40f);
            portraitPanelRect.sizeDelta = new Vector2(360f, 460f);

            var portraitObject = new GameObject("Portrait", typeof(RectTransform));
            portraitObject.transform.SetParent(portraitPanel.transform, false);
            var portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.anchoredPosition = new Vector2(0f, -28f);
            portraitRect.sizeDelta = new Vector2(300f, 300f);
            var portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = new Color(0.12f, 0.11f, 0.1f, 1f);
            portraitImage.preserveAspect = true;

            var portraitButton = CreateTextButton("Import Portrait Button", portraitPanel.transform, "Import Portrait", new Vector2(260f, 48f));
            var portraitButtonRect = portraitButton.GetComponent<RectTransform>();
            portraitButtonRect.anchorMin = new Vector2(0.5f, 0f);
            portraitButtonRect.anchorMax = new Vector2(0.5f, 0f);
            portraitButtonRect.pivot = new Vector2(0.5f, 0f);
            portraitButtonRect.anchoredPosition = new Vector2(0f, 26f);

            var formPanel = CreatePanel("Form Panel", root.transform, new Color(0.068f, 0.062f, 0.054f, 1f));
            var formRect = formPanel.GetComponent<RectTransform>();
            formRect.anchorMin = new Vector2(0f, 0f);
            formRect.anchorMax = new Vector2(1f, 1f);
            formRect.offsetMin = new Vector2(770f, 120f);
            formRect.offsetMax = new Vector2(-40f, -130f);

            var nameInput = CreateInput("Name Input", formPanel.transform, "Character name", false);
            var nameRect = nameInput.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(28f, -88f);
            nameRect.offsetMax = new Vector2(-28f, -28f);

            var descriptionInput = CreateInput("Description Input", formPanel.transform, "Description", true);
            var descriptionRect = descriptionInput.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(28f, 130f);
            descriptionRect.offsetMax = new Vector2(-28f, -118f);

            var tokenLabel = CreateText("Token Label", formPanel.transform, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            var tokenLabelRect = tokenLabel.GetComponent<RectTransform>();
            tokenLabelRect.anchorMin = new Vector2(0f, 0f);
            tokenLabelRect.anchorMax = new Vector2(1f, 0f);
            tokenLabelRect.offsetMin = new Vector2(28f, 38f);
            tokenLabelRect.offsetMax = new Vector2(-28f, 92f);

            var controller = canvasObject.AddComponent<CharacterEditorController>();
            controller.Initialize(nameInput, descriptionInput, portraitImage, tokenLabel);
            backButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Back);
            saveButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Save);
            openButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.Open);
            portraitButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.ImportPortrait);
            selectTokenButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.SelectToken);
            createTokenButton.AddComponent<CharacterEditorButton>().Initialize(controller, CharacterEditorButtonAction.CreateToken);

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

        private static GameObject CreateTextButton(string name, Transform parent, string label, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = size;
            var image = root.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Label", root.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
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

        private static void PositionTop(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
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
    }
}
