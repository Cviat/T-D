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
            var importRect = importButton.GetComponent<RectTransform>();
            importRect.anchorMin = new Vector2(0.5f, 1f);
            importRect.anchorMax = new Vector2(0.5f, 1f);
            importRect.pivot = new Vector2(0.5f, 1f);
            importRect.anchoredPosition = new Vector2(0f, -18f);

            var content = new GameObject("Element Content", typeof(RectTransform));
            content.transform.SetParent(leftPanel.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(18f, 90f);
            contentRect.offsetMax = new Vector2(-18f, -90f);

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96f, 96f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.UpperLeft;

            var palette = leftPanel.AddComponent<MapEditorElementPalette>();
            var serializedPalette = new SerializedObject(palette);
            serializedPalette.FindProperty("contentRoot").objectReferenceValue = contentRect;
            serializedPalette.FindProperty("spawner").objectReferenceValue = spawner;
            serializedPalette.ApplyModifiedPropertiesWithoutUndo();

            var importBinder = importButton.AddComponent<MapEditorImportButton>();
            var serializedImportBinder = new SerializedObject(importBinder);
            serializedImportBinder.FindProperty("palette").objectReferenceValue = palette;
            serializedImportBinder.ApplyModifiedPropertiesWithoutUndo();

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
