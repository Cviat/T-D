using System.IO;
using RPGTable.Input;
using RPGTable.Runtime;
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
    public static class RPGTableCampaignFlowBuilder
    {
        public const string CampaignSelectionScenePath = "Assets/RPGTable/Scenes/CampaignSelection.unity";
        public const string CampaignGameScenePath = "Assets/RPGTable/Scenes/CampaignGame.unity";
        private const string DefaultPlayerPath = "Assets/RPGTable/Resources/DefaultPlayer.png";

        [MenuItem("RPG Table/Build Campaign Flow Scenes")]
        public static void BuildCampaignFlowScenes()
        {
            BuildCampaignSelectionScene();
            BuildCampaignGameScene();
        }

        [MenuItem("RPG Table/Build Campaign Selection Scene")]
        public static void BuildCampaignSelectionScene()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");
            ConfigureSprite(DefaultPlayerPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CampaignSelection";

            CreateCamera(new Color(0.055f, 0.052f, 0.047f));
            var canvasObject = new GameObject("Campaign Selection Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.055f, 0.052f, 0.047f, 1f));
            Stretch(root);

            var left = CreatePanel("Campaign List Panel", root.transform, new Color(0.075f, 0.066f, 0.055f, 0.98f));
            var leftRect = left.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.sizeDelta = new Vector2(360f, 0f);
            leftRect.pivot = new Vector2(0f, 0.5f);

            var backButton = CreateTextButton("Back Button", left.transform, "Back", new Vector2(320f, 48f));
            PositionTop(backButton.GetComponent<RectTransform>(), -20f);

            var listRoot = new GameObject("Campaign List Content", typeof(RectTransform));
            listRoot.transform.SetParent(left.transform, false);
            var listRect = listRoot.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = new Vector2(20f, 24f);
            listRect.offsetMax = new Vector2(-20f, -92f);
            var listLayout = listRoot.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var playersPanel = CreatePanel("Players Panel", root.transform, new Color(0.08f, 0.074f, 0.064f, 0.98f));
            var playersRect = playersPanel.GetComponent<RectTransform>();
            playersRect.anchorMin = new Vector2(0f, 1f);
            playersRect.anchorMax = new Vector2(1f, 1f);
            playersRect.offsetMin = new Vector2(380f, -180f);
            playersRect.offsetMax = new Vector2(-20f, -20f);

            var playersRoot = new GameObject("Player Cards Content", typeof(RectTransform));
            playersRoot.transform.SetParent(playersPanel.transform, false);
            var playerCardsRect = playersRoot.GetComponent<RectTransform>();
            playerCardsRect.anchorMin = new Vector2(0f, 0f);
            playerCardsRect.anchorMax = new Vector2(1f, 1f);
            playerCardsRect.offsetMin = new Vector2(18f, 12f);
            playerCardsRect.offsetMax = new Vector2(-190f, -12f);
            var playerLayout = playersRoot.AddComponent<HorizontalLayoutGroup>();
            playerLayout.spacing = 12f;
            playerLayout.childControlWidth = false;
            playerLayout.childControlHeight = false;
            playerLayout.childForceExpandWidth = false;
            playerLayout.childForceExpandHeight = false;

            var addPlayerButton = CreateTextButton("Add Player Button", playersPanel.transform, "+", new Vector2(132f, 132f));
            var addPlayerRect = addPlayerButton.GetComponent<RectTransform>();
            addPlayerRect.anchorMin = new Vector2(1f, 0.5f);
            addPlayerRect.anchorMax = new Vector2(1f, 0.5f);
            addPlayerRect.pivot = new Vector2(1f, 0.5f);
            addPlayerRect.anchoredPosition = new Vector2(-24f, 0f);

            var detail = CreatePanel("Campaign Detail Panel", root.transform, new Color(0.068f, 0.062f, 0.054f, 1f));
            var detailRect = detail.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.offsetMin = new Vector2(380f, 120f);
            detailRect.offsetMax = new Vector2(-20f, -210f);

            var cover = new GameObject("Campaign Cover", typeof(RectTransform));
            cover.transform.SetParent(detail.transform, false);
            var coverRect = cover.GetComponent<RectTransform>();
            coverRect.anchorMin = new Vector2(0f, 0f);
            coverRect.anchorMax = new Vector2(0.55f, 1f);
            coverRect.offsetMin = new Vector2(24f, 24f);
            coverRect.offsetMax = new Vector2(-18f, -24f);
            var coverImage = cover.AddComponent<Image>();
            coverImage.color = new Color(0.15f, 0.14f, 0.12f, 1f);
            coverImage.preserveAspect = true;

            var title = CreateText("Campaign Title", detail.transform, 36, FontStyle.Bold, TextAnchor.UpperLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.55f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(24f, -90f);
            titleRect.offsetMax = new Vector2(-24f, -24f);

            var description = CreateText("Campaign Description", detail.transform, 22, FontStyle.Normal, TextAnchor.UpperLeft);
            var descriptionRect = description.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.55f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(24f, 24f);
            descriptionRect.offsetMax = new Vector2(-24f, -108f);

            var startButton = CreateTextButton("Start Campaign Button", root.transform, "Начать игру", new Vector2(320f, 64f));
            var startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0f);
            startRect.anchorMax = new Vector2(0.5f, 0f);
            startRect.pivot = new Vector2(0.5f, 0f);
            startRect.anchoredPosition = new Vector2(180f, 30f);

            var warning = CreateText("Start Warning", root.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            var warningRect = warning.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.5f, 0f);
            warningRect.anchorMax = new Vector2(0.5f, 0f);
            warningRect.pivot = new Vector2(0.5f, 0f);
            warningRect.anchoredPosition = new Vector2(180f, 100f);
            warningRect.sizeDelta = new Vector2(640f, 32f);
            warning.color = new Color(1f, 0.62f, 0.25f, 1f);
            warning.gameObject.SetActive(false);

            var controller = canvasObject.AddComponent<CampaignSelectionController>();
            controller.Initialize(listRect, playerCardsRect, coverImage, title, description, warning);
            backButton.AddComponent<CampaignSelectionButton>().Initialize(controller, CampaignSelectionButtonAction.Back);
            addPlayerButton.AddComponent<CampaignSelectionButton>().Initialize(controller, CampaignSelectionButtonAction.AddPlayer);
            startButton.AddComponent<CampaignSelectionButton>().Initialize(controller, CampaignSelectionButtonAction.StartGame);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, CampaignSelectionScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("RPG Table/Build Campaign Game Scene")]
        public static void BuildCampaignGameScene()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CampaignGame";

            var cameraObject = CreateCamera(new Color(0.12f, 0.12f, 0.12f));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            cameraObject.AddComponent<MouseCameraController>();
            var loader = cameraObject.AddComponent<CampaignGameLoader>();
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("worldCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var lightObject = new GameObject("Main Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            EditorSceneManager.SaveScene(scene, CampaignGameScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureSprite(string path)
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

        private static GameObject CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return cameraObject;
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
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<RectTransform>().sizeDelta = size;
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.105f, 0.055f, 0.96f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Label", buttonObject.transform, label == "+" ? 58 : 22, FontStyle.Bold, TextAnchor.MiddleCenter);
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

        private static void Stretch(GameObject gameObject)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PositionTop(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
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
    }
}
