using System.IO;
using RPGTable.UI;
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
    public static class RPGTableMainMenuBuilder
    {
        private const string ScenePath = "Assets/RPGTable/Scenes/MainMenu.unity";
        private const string BackgroundPath = "Assets/RPGTable/Art/MainMenu/MainMenuBackground.png";
        private const string ButtonStartPath = "Assets/RPGTable/Art/MainMenu/Buttons/ButtonStartGame.png";
        private const string ButtonContinuePath = "Assets/RPGTable/Art/MainMenu/Buttons/ButtonContinue.png";
        private const string ButtonAddContentPath = "Assets/RPGTable/Art/MainMenu/Buttons/ButtonAddContent.png";
        private const string ButtonQuitPath = "Assets/RPGTable/Art/MainMenu/Buttons/ButtonQuit.png";
        private const string ContentBackgroundPath = "Assets/RPGTable/Art/MainMenu/Editor/ContentMenuBackground.png";
        private const string PrototypeScenePath = "Assets/RPGTable/Scenes/RPGTablePrototype.unity";
        private const string MapEditorScenePath = "Assets/RPGTable/Scenes/MapEditor.unity";

        [MenuItem("RPG Table/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            EnsureFolders();
            ConfigureSpriteImport(BackgroundPath, false);
            ConfigureSpriteImport(ButtonStartPath, true);
            ConfigureSpriteImport(ButtonContinuePath, true);
            ConfigureSpriteImport(ButtonAddContentPath, true);
            ConfigureSpriteImport(ButtonQuitPath, true);
            ConfigureSpriteImport(ContentBackgroundPath, false);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            var controller = CreateController();
            var canvas = CreateCanvas();
            var mainPanel = CreateMainMenuPanel(canvas.transform, controller);
            var contentPanel = CreateContentMenuPanel(canvas.transform, controller);
            contentPanel.SetActive(false);
            WireControllerPanels(controller, mainPanel, contentPanel);
            CreateEventSystem();
            CreateCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RPG Table main menu scene built: {ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");
            Directory.CreateDirectory("Assets/RPGTable/Art/MainMenu/Buttons");
            Directory.CreateDirectory("Assets/RPGTable/Art/MainMenu/Editor");
        }

        private static void ConfigureSpriteImport(string path, bool alphaIsTransparency)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning($"Main menu sprite was not found: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static MainMenuController CreateController()
        {
            var controllerObject = new GameObject("Main Menu Controller");
            return controllerObject.AddComponent<MainMenuController>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Main Menu Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateMainMenuPanel(Transform parent, MainMenuController controller)
        {
            var panel = CreateUiObject("Main Menu Panel", parent);
            Stretch(panel);
            CreateBackground(panel.transform, BackgroundPath);
            CreateLeftShade(panel.transform);
            CreateLogoPlaceholder(panel.transform);
            CreateButtonStack(panel.transform, controller);
            CreateFooter(panel.transform);
            return panel;
        }

        private static GameObject CreateContentMenuPanel(Transform parent, MainMenuController controller)
        {
            var panel = CreateUiObject("Content Menu Panel", parent);
            Stretch(panel);
            CreateBackground(panel.transform, ContentBackgroundPath);
            CreateLeftShade(panel.transform);
            CreateContentButtonStack(panel.transform, controller);
            return panel;
        }

        private static void WireControllerPanels(MainMenuController controller, GameObject mainPanel, GameObject contentPanel)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("contentMenuPanel").objectReferenceValue = contentPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBackground(Transform parent, string spritePath)
        {
            var background = CreateUiObject("Background", parent);
            Stretch(background);

            var image = background.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void CreateLeftShade(Transform parent)
        {
            var shade = CreateUiObject("Left readability shade", parent);
            var rect = shade.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.48f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = shade.AddComponent<Image>();
            image.color = new Color(0.01f, 0.012f, 0.014f, 0.42f);
            image.raycastTarget = false;
        }

        private static void CreateLogoPlaceholder(Transform parent)
        {
            var panel = CreateUiObject("Logo placeholder", parent);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(118f, -76f);
            rect.sizeDelta = new Vector2(560f, 246f);

            var shadow = panel.AddComponent<Image>();
            shadow.color = new Color(0.035f, 0.022f, 0.014f, 0.92f);

            var border = CreateUiObject("Logo border", panel.transform);
            Stretch(border, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            border.AddComponent<Image>().color = new Color(0.72f, 0.58f, 0.36f, 0.65f);

            var inner = CreateUiObject("Logo inner", panel.transform);
            Stretch(inner, new Vector2(20f, 20f), new Vector2(-20f, -20f));
            inner.AddComponent<Image>().color = new Color(0.12f, 0.07f, 0.035f, 0.96f);

            CreateLabel("RPG", inner.transform, 118, new Color(1f, 0.77f, 0.28f), new Vector2(0f, 22f));
            CreateLabel("TABLE", inner.transform, 50, new Color(1f, 0.84f, 0.45f), new Vector2(0f, -74f));
        }

        private static void CreateButtonStack(Transform parent, MainMenuController controller)
        {
            var stack = CreateUiObject("Main menu buttons", parent);
            var rect = stack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(90f, -154f);
            rect.sizeDelta = new Vector2(660f, 590f);

            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateMenuButton(stack.transform, controller, MainMenuAction.StartGame, "Start Game", ButtonStartPath);
            CreateMenuButton(stack.transform, controller, MainMenuAction.ContinueGame, "Continue", ButtonContinuePath);
            CreateMenuButton(stack.transform, controller, MainMenuAction.AddContent, "Add Content", ButtonAddContentPath);
            CreateMenuButton(stack.transform, controller, MainMenuAction.Quit, "Quit", ButtonQuitPath);
        }

        private static void CreateContentButtonStack(Transform parent, MainMenuController controller)
        {
            var stack = CreateUiObject("Content menu buttons", parent);
            var rect = stack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(110f, -40f);
            rect.sizeDelta = new Vector2(460f, 300f);

            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTextMenuButton(stack.transform, controller, MainMenuAction.CreateMap, "Создать карту");
            CreateTextMenuButton(stack.transform, controller, MainMenuAction.CreateToken, "Создать фишку");
            CreateTextMenuButton(stack.transform, controller, MainMenuAction.BackToMain, "Назад");
        }

        private static void CreateMenuButton(
            Transform parent,
            MainMenuController controller,
            MainMenuAction action,
            string name,
            string spritePath)
        {
            var root = CreateUiObject($"{name} button", parent);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 155f);

            var image = root.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.color = Color.white;
            image.preserveAspect = true;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var menuButton = root.AddComponent<MainMenuButton>();
            var serializedButton = new SerializedObject(menuButton);
            serializedButton.FindProperty("action").enumValueIndex = (int)action;
            serializedButton.FindProperty("controller").objectReferenceValue = controller;
            serializedButton.FindProperty("targetGraphic").objectReferenceValue = image;
            serializedButton.FindProperty("normalColor").colorValue = Color.white;
            serializedButton.FindProperty("hoverColor").colorValue = new Color(1f, 1f, 1f, 0.86f);
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTextMenuButton(
            Transform parent,
            MainMenuController controller,
            MainMenuAction action,
            string label)
        {
            var root = CreateUiObject($"{label} button", parent);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(390f, 72f);

            var image = root.AddComponent<Image>();
            image.color = new Color(0.12f, 0.075f, 0.035f, 0.9f);

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var menuButton = root.AddComponent<MainMenuButton>();
            var serializedButton = new SerializedObject(menuButton);
            serializedButton.FindProperty("action").enumValueIndex = (int)action;
            serializedButton.FindProperty("controller").objectReferenceValue = controller;
            serializedButton.FindProperty("targetGraphic").objectReferenceValue = image;
            serializedButton.FindProperty("normalColor").colorValue = image.color;
            serializedButton.FindProperty("hoverColor").colorValue = new Color(0.22f, 0.14f, 0.06f, 0.95f);
            serializedButton.ApplyModifiedPropertiesWithoutUndo();

            var textObject = CreateUiObject("Label", root.transform);
            Stretch(textObject, new Vector2(22f, 0f), new Vector2(-22f, 0f));

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static void CreateFooter(Transform parent)
        {
            var footer = CreateUiObject("Footer", parent);
            var rect = footer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-38f, 26f);
            rect.sizeDelta = new Vector2(520f, 44f);

            var text = footer.AddComponent<Text>();
            text.text = "RPG Table prototype";
            text.alignment = TextAnchor.MiddleRight;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = new Color(1f, 1f, 1f, 0.74f);
            text.raycastTarget = false;
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
            camera.backgroundColor = Color.black;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
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

        private static void CreateLabel(string text, Transform parent, int fontSize, Color color, Vector2 position)
        {
            var label = CreateUiObject(text, parent);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(460f, 120f);

            var labelText = label.AddComponent<Text>();
            labelText.text = text;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontStyle = FontStyle.Bold;
            labelText.fontSize = fontSize;
            labelText.color = color;
            labelText.raycastTarget = false;
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(PrototypeScenePath, true),
                new EditorBuildSettingsScene(MapEditorScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
        }
    }
}
