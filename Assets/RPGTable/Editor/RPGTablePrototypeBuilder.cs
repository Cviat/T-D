using System.IO;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.GameMaster;
using RPGTable.Input;
using RPGTable.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPGTable.Editor
{
    public static class RPGTablePrototypeBuilder
    {
        private const string ScenePath = "Assets/RPGTable/Scenes/RPGTablePrototype.unity";
        private const string CardsPath = "Assets/RPGTable/ScriptableObjects/AbilityCards";

        [MenuItem("RPG Table/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RPGTablePrototype";

            var grid = CreateBoard();
            var fog = CreateFog(grid);
            CreateCamera(grid);
            CreateLight();
            CreateToken("Aria", TokenTeam.Player, true, 18, new Vector2Int(2, 2), new Color(0.15f, 0.55f, 1f));
            CreateToken("Bran", TokenTeam.Player, true, 13, new Vector2Int(3, 2), new Color(0.25f, 0.85f, 0.45f));
            CreateToken("Goblin Scout", TokenTeam.Enemy, true, 15, new Vector2Int(7, 5), new Color(0.95f, 0.25f, 0.2f));
            CreateToken("Hidden Shade", TokenTeam.Enemy, false, 9, new Vector2Int(9, 3), new Color(0.65f, 0.25f, 0.95f));
            CreateGameMaster(fog);
            CreateHud();
            CreateSampleAbilityCards();

            fog.Reveal(new Vector2Int(1, 1));
            fog.Reveal(new Vector2Int(2, 1));
            fog.Reveal(new Vector2Int(3, 1));
            fog.Reveal(new Vector2Int(1, 2));
            fog.Reveal(new Vector2Int(2, 2));
            fog.Reveal(new Vector2Int(3, 2));
            fog.Reveal(new Vector2Int(4, 2));
            fog.Reveal(new Vector2Int(2, 3));
            fog.Reveal(new Vector2Int(3, 3));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RPG Table prototype scene built: {ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/RPGTable/Scenes");
            Directory.CreateDirectory(CardsPath);
        }

        private static BoardGrid CreateBoard()
        {
            var board = new GameObject("Board");
            board.transform.position = new Vector3(-6f, -4f, 0f);

            var grid = board.AddComponent<BoardGrid>();
            grid.width = 12;
            grid.height = 8;
            grid.cellSize = 1f;
            board.AddComponent<BoardGridVisual>().Build();

            var background = new GameObject("Board Background");
            background.transform.SetParent(board.transform, false);
            background.transform.localPosition = new Vector3(6f, 4f, 0.2f);
            background.transform.localScale = new Vector3(12f, 8f, 1f);

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.Square;
            renderer.color = new Color(0.11f, 0.14f, 0.15f);
            renderer.sortingOrder = -10;

            return grid;
        }

        private static FogOfWarController CreateFog(BoardGrid grid)
        {
            var fogObject = new GameObject("Fog of War");
            var fog = fogObject.AddComponent<FogOfWarController>();
            fog.Build(grid);
            fog.SetPlayerView(false);
            return fog;
        }

        private static void CreateCamera(BoardGrid grid)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            var center = grid.CellToWorld(new Vector2Int(grid.width / 2, grid.height / 2));
            cameraObject.transform.position = new Vector3(center.x - 0.5f, center.y - 0.5f, -10f);
            cameraObject.AddComponent<MouseCameraController>();
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Main Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static void CreateToken(
            string displayName,
            TokenTeam team,
            bool visibleToPlayers,
            int initiative,
            Vector2Int cell,
            Color tint)
        {
            var tokenObject = new GameObject(displayName);
            tokenObject.transform.position = new Vector3(cell.x, cell.y, -0.5f);

            var renderer = tokenObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.Circle;
            renderer.color = tint;
            renderer.sortingOrder = 10;

            var collider = tokenObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            var token = tokenObject.AddComponent<BoardToken>();
            token.displayName = displayName;
            token.team = team;
            token.visibleToPlayers = visibleToPlayers;
            token.initiative = initiative;

            tokenObject.AddComponent<RPGTable.Input.TokenDragController>();

            var grid = Object.FindAnyObjectByType<BoardGrid>();
            tokenObject.transform.position = grid.CellToWorld(cell) + Vector3.back * 0.5f;
            token.SnapToGrid(grid);
        }

        private static void CreateGameMaster(FogOfWarController fog)
        {
            var gameMaster = new GameObject("Game Master");
            var viewMode = gameMaster.AddComponent<ViewModeController>();
            var serialized = new SerializedObject(viewMode);
            serialized.FindProperty("fogOfWar").objectReferenceValue = fog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud()
        {
            var hud = new GameObject("Prototype HUD");
            hud.AddComponent<PrototypeHud>();
        }

        private static void CreateSampleAbilityCards()
        {
            CreateAbilityCard(
                "Strike",
                "Deal 2 damage to one visible enemy in melee range.",
                0,
                1,
                AbilityEffectType.Damage);

            CreateAbilityCard(
                "Patch Wounds",
                "Restore 2 health to yourself or an adjacent ally.",
                1,
                1,
                AbilityEffectType.Heal);

            CreateAbilityCard(
                "Scout Ahead",
                "Reveal a small nearby area without moving into it.",
                1,
                4,
                AbilityEffectType.Reveal);
        }

        private static void CreateAbilityCard(
            string title,
            string description,
            int cost,
            int range,
            AbilityEffectType effectType)
        {
            var path = $"{CardsPath}/{title.Replace(" ", string.Empty)}.asset";
            var card = AssetDatabase.LoadAssetAtPath<AbilityCard>(path);

            if (card == null)
            {
                card = ScriptableObject.CreateInstance<AbilityCard>();
                AssetDatabase.CreateAsset(card, path);
            }

            card.title = title;
            card.description = description;
            card.cost = cost;
            card.range = range;
            card.effectType = effectType;
            EditorUtility.SetDirty(card);
        }
    }
}
