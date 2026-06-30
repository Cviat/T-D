using System.IO;
using RPGTable.Board;
using RPGTable.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var lightObject = new GameObject("Main Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
