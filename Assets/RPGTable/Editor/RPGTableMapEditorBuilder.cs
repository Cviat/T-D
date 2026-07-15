using System.IO;
using RPGTable.Board;
using RPGTable.Input;
using RPGTable.MapEditor;
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
    public static class RPGTableMapEditorBuilder
    {
        private const string ScenePath = "Assets/RPGTable/Scenes/MapEditor.unity";
        private const string CampaignScenePath = "Assets/RPGTable/Scenes/CampaignEditor.unity";

        [MenuItem("RPG Table/Build Map Editor Scene")]
        public static void BuildMapEditorScene()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MapEditor";

            var board = new GameObject("Map Editor Board");
            board.transform.position = new Vector3(-50f, -50f, 0f);

            var grid = board.AddComponent<BoardGrid>();
            grid.width = 100;
            grid.height = 100;
            grid.cellSize = 1f;
            board.AddComponent<BoardGridVisual>().Build();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<MouseCameraController>();
            var spawner = cameraObject.AddComponent<MapEditorElementSpawner>();

            var lightObject = new GameObject("Main Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            CreateEditorUi(spawner);
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("RPG Table/Build Campaign Editor Scene")]
        public static void BuildCampaignEditorScene()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CampaignEditor";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.backgroundColor = new Color(0.095f, 0.095f, 0.085f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            var canvasObject = new GameObject("Campaign Editor Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            var leftPanel = CreatePanel("Left Maps Panel", canvasObject.transform, new Color(0.06f, 0.055f, 0.05f, 0.94f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.sizeDelta = new Vector2(320f, 0f);
            leftRect.anchoredPosition = Vector2.zero;

            var board = CreatePanel("Campaign Board", canvasObject.transform, new Color(0.11f, 0.105f, 0.095f, 1f));
            var boardRect = board.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0f, 0f);
            boardRect.anchorMax = new Vector2(1f, 1f);
            boardRect.offsetMin = new Vector2(320f, 0f);
            boardRect.offsetMax = Vector2.zero;
            board.AddComponent<RectMask2D>();

            var boardContent = new GameObject("Campaign Board Content", typeof(RectTransform));
            boardContent.transform.SetParent(board.transform, false);
            var boardContentRect = boardContent.GetComponent<RectTransform>();
            boardContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardContentRect.pivot = new Vector2(0.5f, 0.5f);
            boardContentRect.sizeDelta = new Vector2(3200f, 2200f);
            boardContentRect.anchoredPosition = Vector2.zero;
            var boardContentImage = boardContent.AddComponent<Image>();
            boardContentImage.color = new Color(0.145f, 0.14f, 0.125f, 1f);

            var boardTitle = new GameObject("Board Hint", typeof(RectTransform));
            boardTitle.transform.SetParent(boardContent.transform, false);
            var boardTitleRect = boardTitle.GetComponent<RectTransform>();
            boardTitleRect.anchorMin = new Vector2(0.5f, 1f);
            boardTitleRect.anchorMax = new Vector2(0.5f, 1f);
            boardTitleRect.pivot = new Vector2(0.5f, 1f);
            boardTitleRect.anchoredPosition = new Vector2(0f, -28f);
            boardTitleRect.sizeDelta = new Vector2(700f, 48f);
            var boardTitleText = boardTitle.AddComponent<Text>();
            boardTitleText.text = "Campaign Board";
            boardTitleText.alignment = TextAnchor.MiddleCenter;
            boardTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boardTitleText.fontStyle = FontStyle.Bold;
            boardTitleText.fontSize = 28;
            boardTitleText.color = new Color(1f, 1f, 1f, 0.34f);
            boardTitleText.raycastTarget = false;

            var boardPanZoom = board.AddComponent<CampaignBoardPanZoom>();
            boardPanZoom.Initialize(boardContentRect);

            var backButton = CreateTextButton("Back Button", leftPanel.transform, "Back", new Vector2(280f, 48f));
            PositionTopButton(backButton.GetComponent<RectTransform>(), -18f);

            var saveButton = CreateTextButton("Save Campaign Button", leftPanel.transform, "Save Campaign", new Vector2(280f, 48f));
            PositionTopButton(saveButton.GetComponent<RectTransform>(), -76f);

            var openButton = CreateTextButton("Open Campaign Button", leftPanel.transform, "Import Campaign", new Vector2(280f, 48f));
            PositionTopButton(openButton.GetComponent<RectTransform>(), -134f);

            var coverButton = CreateTextButton("Import Campaign Cover Button", leftPanel.transform, "Import Cover", new Vector2(280f, 42f));
            PositionTopButton(coverButton.GetComponent<RectTransform>(), -192f);

            var coverPreview = new GameObject("Campaign Cover Preview", typeof(RectTransform));
            coverPreview.transform.SetParent(leftPanel.transform, false);
            var coverRect = coverPreview.GetComponent<RectTransform>();
            coverRect.anchorMin = new Vector2(0.5f, 1f);
            coverRect.anchorMax = new Vector2(0.5f, 1f);
            coverRect.pivot = new Vector2(0.5f, 1f);
            coverRect.anchoredPosition = new Vector2(0f, -244f);
            coverRect.sizeDelta = new Vector2(280f, 112f);
            var coverImage = coverPreview.AddComponent<Image>();
            coverImage.color = new Color(0.15f, 0.14f, 0.12f, 1f);
            coverImage.preserveAspect = true;

            var descriptionInput = CreateMultilineInput("Campaign Description Input", leftPanel.transform, "Campaign description");
            var descriptionRect = descriptionInput.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.5f, 1f);
            descriptionRect.anchorMax = new Vector2(0.5f, 1f);
            descriptionRect.pivot = new Vector2(0.5f, 1f);
            descriptionRect.anchoredPosition = new Vector2(0f, -370f);
            descriptionRect.sizeDelta = new Vector2(280f, 112f);

            var listRect = CreateScrollView("Map List Scroll View", leftPanel.transform, new Vector2(20f, 24f), new Vector2(-20f, -506f));

            var layout = listRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = listRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var controller = canvasObject.AddComponent<CampaignEditorController>();
            controller.Initialize(listRect, boardContentRect, descriptionInput, coverImage);

            var backBinder = backButton.AddComponent<CampaignEditorButton>();
            backBinder.Initialize(controller, CampaignEditorButtonAction.Back);

            var saveBinder = saveButton.AddComponent<CampaignEditorButton>();
            saveBinder.Initialize(controller, CampaignEditorButtonAction.Save);

            var openBinder = openButton.AddComponent<CampaignEditorButton>();
            openBinder.Initialize(controller, CampaignEditorButtonAction.Open);

            var coverBinder = coverButton.AddComponent<CampaignEditorButton>();
            coverBinder.Initialize(controller, CampaignEditorButtonAction.ImportCover);

            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, CampaignScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateEditorUi(MapEditorElementSpawner spawner)
        {
            var canvasObject = new GameObject("Map Editor Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            var leftPanel = CreatePanel("Left Elements Panel", canvasObject.transform, new Color(0.06f, 0.055f, 0.05f, 0.92f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.sizeDelta = new Vector2(280f, 0f);
            leftRect.anchoredPosition = Vector2.zero;

            var importButton = CreateTextButton("Import Image Button", leftPanel.transform, "Импорт изображений", new Vector2(240f, 54f));
            var backButton = CreateTextButton("Back Button", leftPanel.transform, "Back", new Vector2(240f, 48f));
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 1f);
            backRect.anchorMax = new Vector2(0.5f, 1f);
            backRect.pivot = new Vector2(0.5f, 1f);
            backRect.anchoredPosition = new Vector2(0f, -18f);

            var saveMapButton = CreateTextButton("Save Map Button", leftPanel.transform, "Save Map", new Vector2(240f, 48f));
            var saveMapRect = saveMapButton.GetComponent<RectTransform>();
            saveMapRect.anchorMin = new Vector2(0.5f, 1f);
            saveMapRect.anchorMax = new Vector2(0.5f, 1f);
            saveMapRect.pivot = new Vector2(0.5f, 1f);
            saveMapRect.anchoredPosition = new Vector2(0f, -76f);

            var openMapButton = CreateTextButton("Open Map Button", leftPanel.transform, "Import Map", new Vector2(240f, 48f));
            var openMapRect = openMapButton.GetComponent<RectTransform>();
            openMapRect.anchorMin = new Vector2(0.5f, 1f);
            openMapRect.anchorMax = new Vector2(0.5f, 1f);
            openMapRect.pivot = new Vector2(0.5f, 1f);
            openMapRect.anchoredPosition = new Vector2(0f, -134f);

            var addExitButton = CreateTextButton("Add Exit Button", leftPanel.transform, "Add Exit", new Vector2(240f, 48f));
            var addExitRect = addExitButton.GetComponent<RectTransform>();
            addExitRect.anchorMin = new Vector2(0.5f, 1f);
            addExitRect.anchorMax = new Vector2(0.5f, 1f);
            addExitRect.pivot = new Vector2(0.5f, 1f);
            addExitRect.anchoredPosition = new Vector2(0f, -192f);

            var addSpawnButton = CreateTextButton("Add Spawn Button", leftPanel.transform, "Add Spawn", new Vector2(240f, 48f));
            var addSpawnRect = addSpawnButton.GetComponent<RectTransform>();
            addSpawnRect.anchorMin = new Vector2(0.5f, 1f);
            addSpawnRect.anchorMax = new Vector2(0.5f, 1f);
            addSpawnRect.pivot = new Vector2(0.5f, 1f);
            addSpawnRect.anchoredPosition = new Vector2(0f, -250f);

            var importRect = importButton.GetComponent<RectTransform>();
            importRect.anchorMin = new Vector2(0.5f, 1f);
            importRect.anchorMax = new Vector2(0.5f, 1f);
            importRect.pivot = new Vector2(0.5f, 1f);
            importRect.anchoredPosition = new Vector2(0f, -308f);

            var contentRect = CreateScrollView("Element Scroll View", leftPanel.transform, new Vector2(18f, 90f), new Vector2(-18f, -374f));

            var grid = contentRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96f, 96f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var palette = leftPanel.AddComponent<MapEditorElementPalette>();
            var serializedPalette = new SerializedObject(palette);
            serializedPalette.FindProperty("contentRoot").objectReferenceValue = contentRect;
            serializedPalette.FindProperty("spawner").objectReferenceValue = spawner;
            serializedPalette.ApplyModifiedPropertiesWithoutUndo();

            var importBinder = importButton.AddComponent<MapEditorImportButton>();
            var serializedImportBinder = new SerializedObject(importBinder);
            serializedImportBinder.FindProperty("palette").objectReferenceValue = palette;
            serializedImportBinder.ApplyModifiedPropertiesWithoutUndo();

            var mapController = leftPanel.AddComponent<MapEditorMapController>();
            mapController.Initialize(spawner);

            var backBinder = backButton.AddComponent<MapEditorMapButton>();
            backBinder.Initialize(mapController, MapEditorMapButtonAction.Back);

            var saveMapBinder = saveMapButton.AddComponent<MapEditorMapButton>();
            saveMapBinder.Initialize(mapController, MapEditorMapButtonAction.Save);

            var openMapBinder = openMapButton.AddComponent<MapEditorMapButton>();
            openMapBinder.Initialize(mapController, MapEditorMapButtonAction.Open);

            var addExitBinder = addExitButton.AddComponent<MapEditorMapButton>();
            addExitBinder.Initialize(mapController, MapEditorMapButtonAction.AddExit);

            var addSpawnBinder = addSpawnButton.AddComponent<MapEditorMapButton>();
            addSpawnBinder.Initialize(mapController, MapEditorMapButtonAction.AddSpawn);

            var bottomPanel = CreatePanel("Bottom Tool Panel", canvasObject.transform, new Color(0.045f, 0.04f, 0.035f, 0.9f));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(0f, 72f);
            bottomRect.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static void PositionTopButton(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static GameObject CreateTextButton(string name, Transform parent, string label, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<RectTransform>().sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            return buttonObject;
        }

        private static InputField CreateMultilineInput(string name, Transform parent, string placeholder)
        {
            var inputObject = new GameObject(name, typeof(RectTransform));
            inputObject.transform.SetParent(parent, false);

            var image = inputObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.075f, 0.065f, 0.96f);

            var input = inputObject.AddComponent<InputField>();
            input.lineType = InputField.LineType.MultiLineNewline;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(inputObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform));
            placeholderObject.transform.SetParent(inputObject.transform, false);
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 8f);
            placeholderRect.offsetMax = new Vector2(-10f, -8f);

            var placeholderText = placeholderObject.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 16;
            placeholderText.color = new Color(1f, 1f, 1f, 0.38f);
            placeholderText.alignment = TextAnchor.UpperLeft;
            placeholderText.raycastTarget = false;

            input.textComponent = text;
            input.placeholder = placeholderText;
            return input;
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

        private static RectTransform CreateScrollView(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollObject = new GameObject(name, typeof(RectTransform));
            scrollObject.transform.SetParent(parent, false);
            var scrollRt = scrollObject.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = offsetMin;
            scrollRt.offsetMax = offsetMax;

            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewRt = viewport.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            contentRt.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewRt;
            scrollRect.content = contentRt;

            return contentRt;
        }
    }
}
