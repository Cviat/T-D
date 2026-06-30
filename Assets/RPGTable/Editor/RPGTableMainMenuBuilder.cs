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
        private const string PrototypeScenePath = "Assets/RPGTable/Scenes/RPGTablePrototype.unity";

        [MenuItem("RPG Table/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            EnsureFolders();
            ConfigureBackgroundImport();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            var controller = CreateController();
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateLeftShade(canvas.transform);
            CreateLogo(canvas.transform);
            CreateButtonStack(canvas.transform, controller);
            CreateFooter(canvas.transform);
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
            Directory.CreateDirectory("Assets/RPGTable/Art/MainMenu");
        }

        private static void ConfigureBackgroundImport()
        {
            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning($"Main menu background was not found: {BackgroundPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
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
            canvas.sortingOrder = 0;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateBackground(Transform parent)
        {
            var background = CreateUiObject("Background", parent);
            Stretch(background);

            var image = background.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
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

        private static void CreateLogo(Transform parent)
        {
            var panel = CreateUiObject("Logo panel", parent);
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

            CreateLabel("RPG", inner.transform, 118, FontStyle.Bold, new Color(1f, 0.77f, 0.28f), new Vector2(0f, 22f));
            CreateLabel("TABLE", inner.transform, 50, FontStyle.Bold, new Color(1f, 0.84f, 0.45f), new Vector2(0f, -74f));
        }

        private static void CreateButtonStack(Transform parent, MainMenuController controller)
        {
            var stack = CreateUiObject("Main menu buttons", parent);

            var rect = stack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(134f, -138f);
            rect.sizeDelta = new Vector2(540f, 430f);

            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateMenuButton(stack.transform, controller, MainMenuAction.StartGame, "Начать игру", "X", new Color(0.03f, 0.26f, 0.43f), new Color(0.04f, 0.38f, 0.62f));
            CreateMenuButton(stack.transform, controller, MainMenuAction.ContinueGame, "Продолжить", "S", new Color(0.08f, 0.28f, 0.07f), new Color(0.13f, 0.42f, 0.11f));
            CreateMenuButton(stack.transform, controller, MainMenuAction.AddContent, "Добавить контент", "C", new Color(0.18f, 0.09f, 0.31f), new Color(0.27f, 0.13f, 0.45f));
            CreateMenuButton(stack.transform, controller, MainMenuAction.Quit, "Выйти", "D", new Color(0.36f, 0.12f, 0.055f), new Color(0.52f, 0.17f, 0.07f));
        }

        private static void CreateMenuButton(
            Transform parent,
            MainMenuController controller,
            MainMenuAction action,
            string label,
            string iconText,
            Color normalColor,
            Color hoverColor)
        {
            var root = CreateUiObject($"{label} button", parent);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 94f);

            var image = root.AddComponent<Image>();
            image.color = normalColor;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var menuButton = root.AddComponent<MainMenuButton>();
            var serializedButton = new SerializedObject(menuButton);
            serializedButton.FindProperty("action").enumValueIndex = (int)action;
            serializedButton.FindProperty("controller").objectReferenceValue = controller;
            serializedButton.FindProperty("targetGraphic").objectReferenceValue = image;
            serializedButton.FindProperty("normalColor").colorValue = normalColor;
            serializedButton.FindProperty("hoverColor").colorValue = hoverColor;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();

            var border = CreateUiObject("Border", root.transform);
            Stretch(border, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            border.AddComponent<Image>().color = new Color(0.88f, 0.68f, 0.38f, 0.34f);

            var inner = CreateUiObject("Inner shade", root.transform);
            Stretch(inner, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            inner.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.16f);

            var icon = CreateUiObject("Icon", root.transform);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(72f, 0f);
            iconRect.sizeDelta = new Vector2(58f, 58f);

            var iconTextComponent = icon.AddComponent<Text>();
            iconTextComponent.text = iconText;
            iconTextComponent.alignment = TextAnchor.MiddleCenter;
            iconTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconTextComponent.fontStyle = FontStyle.Bold;
            iconTextComponent.fontSize = 36;
            iconTextComponent.color = new Color(1f, 0.77f, 0.35f);
            iconTextComponent.raycastTarget = false;

            var text = CreateUiObject("Label", root.transform);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(132f, 0f);
            textRect.offsetMax = new Vector2(-24f, 0f);

            var textComponent = text.AddComponent<Text>();
            textComponent.text = label;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontStyle = FontStyle.Bold;
            textComponent.fontSize = 32;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
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

        private static void CreateLabel(string text, Transform parent, int fontSize, FontStyle style, Color color, Vector2 position)
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
            labelText.fontStyle = style;
            labelText.fontSize = fontSize;
            labelText.color = color;
            labelText.raycastTarget = false;
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(PrototypeScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
        }
    }
}
