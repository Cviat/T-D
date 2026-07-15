using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapEditorMapController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private MapEditorElementSpawner spawner;

        private string currentMapName;
        private bool placingExitPoint;
        private bool placingSpawnZone;

        public void Initialize(MapEditorElementSpawner elementSpawner)
        {
            spawner = elementSpawner;
        }

        public void OpenMapPresetTokens()
        {
            OpenMap(CampaignEditSession.EditingMapPath);
            LoadPresetTokensForEditing();
        }

        public void RequestSaveMap()
        {
            if (CampaignEditSession.IsEditingPresetTokens)
            {
                SavePresetTokens();
            }
            else
            {
                MapEditorMapDialog.ShowSave(currentMapName, SaveMap);
            }
        }

        public void RequestOpenMap()
        {
            MapEditorMapDialog.ShowOpen(UserMapStore.GetMapPaths(), OpenMap);
        }

        public void BeginAddExitPoint()
        {
            placingExitPoint = true;
            placingSpawnZone = false;
        }

        public void BeginAddSpawnZone()
        {
            placingSpawnZone = true;
            placingExitPoint = false;
        }

        public void BackToMainMenu()
        {
            if (CampaignEditSession.IsEditingPresetTokens)
            {
                CampaignEditSession.Clear();
                SceneManager.LoadScene("CampaignEditor");
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            Debug.LogWarning($"Scene '{mainMenuSceneName}' is not in Build Settings yet.");
        }

        private void SavePresetTokens()
        {
            var campaignPath = Path.Combine(UserCampaignStore.CampaignsFolder, $"{CampaignEditSession.ActiveCampaignName}.json");
            var campaignData = UserCampaignStore.LoadCampaign(campaignPath);
            if (campaignData == null) return;

            var placedTokens = FindObjectsByType<PlacedMapToken>(FindObjectsInactive.Exclude);
            var presetTokensList = new System.Collections.Generic.List<SavedCampaignTokenData>();

            foreach (var t in placedTokens)
            {
                presetTokensList.Add(new SavedCampaignTokenData
                {
                    displayName = t.displayName,
                    characterPath = t.characterPath,
                    tokenPath = t.tokenPath,
                    worldPosition = new Vector2(t.transform.position.x, t.transform.position.y),
                    team = t.team,
                    visibleToPlayers = t.visibleToPlayers
                });
            }

            if (campaignData.maps != null)
            {
                foreach (var mapNode in campaignData.maps)
                {
                    if (mapNode.id == CampaignEditSession.EditingNodeId)
                    {
                        mapNode.presetTokens = presetTokensList.ToArray();
                        break;
                    }
                }
            }

            UserCampaignStore.SaveCampaign(CampaignEditSession.ActiveCampaignName, campaignData);
            CampaignEditSession.Clear();
            SceneManager.LoadScene("CampaignEditor");
        }

        private void LoadPresetTokensForEditing()
        {
            foreach (var t in FindObjectsByType<PlacedMapToken>(FindObjectsInactive.Exclude))
            {
                Destroy(t.gameObject);
            }

            var campaignPath = Path.Combine(UserCampaignStore.CampaignsFolder, $"{CampaignEditSession.ActiveCampaignName}.json");
            var campaignData = UserCampaignStore.LoadCampaign(campaignPath);
            if (campaignData == null) return;

            SavedCampaignMapNodeData currentNode = null;
            if (campaignData.maps != null)
            {
                foreach (var mapNode in campaignData.maps)
                {
                    if (mapNode.id == CampaignEditSession.EditingNodeId)
                    {
                        currentNode = mapNode;
                        break;
                    }
                }
            }

            if (currentNode == null || currentNode.presetTokens == null) return;

            foreach (var preset in currentNode.presetTokens)
            {
                var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(preset.characterPath);
                if (charData == null) continue;

                var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(preset.tokenPath);
                int footprint = tokenData != null ? Mathf.Max(1, tokenData.footprintSize) : 1;

                var tokenGo = new GameObject(preset.displayName);
                var collider = tokenGo.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one * footprint;

                var pToken = tokenGo.AddComponent<PlacedMapToken>();
                pToken.Initialize(preset.displayName, preset.characterPath, preset.tokenPath, preset.worldPosition, preset.team, preset.visibleToPlayers, footprint);
                pToken.SetupVisuals(preset.tokenPath, footprint);
            }
        }

        private void Update()
        {
            if (!placingExitPoint && !placingSpawnZone)
            {
                return;
            }

            if (EscapePressed())
            {
                placingExitPoint = false;
                placingSpawnZone = false;
                return;
            }

            if (!PrimaryPressedThisFrame() || IsPointerOverUi())
            {
                return;
            }

            if (placingExitPoint)
            {
                CreateExitPoint(MouseWorld(), null, null, new Vector2(1.2f, 1.2f));
                placingExitPoint = false;
                return;
            }

            CreateSpawnZone(MouseWorld(), null, null, new Vector2(1.2f, 1.2f));
            placingSpawnZone = false;
        }

        private void SaveMap(string mapName)
        {
            var placedElements = FindObjectsByType<PlacedMapElement>(FindObjectsInactive.Exclude);
            var exitPoints = FindObjectsByType<MapExitPoint>(FindObjectsInactive.Exclude);
            var spawnZones = FindObjectsByType<MapSpawnZone>(FindObjectsInactive.Exclude);
            var saveableElements = new System.Collections.Generic.List<PlacedMapElement>();
            var saveableExitPoints = new System.Collections.Generic.List<MapExitPoint>();
            var saveableSpawnZones = new System.Collections.Generic.List<MapSpawnZone>();

            foreach (var element in placedElements)
            {
                if (!string.IsNullOrWhiteSpace(element.SourceImagePath))
                {
                    saveableElements.Add(element);
                }
            }

            foreach (var exitPoint in exitPoints)
            {
                saveableExitPoints.Add(exitPoint);
            }

            foreach (var spawnZone in spawnZones)
            {
                saveableSpawnZones.Add(spawnZone);
            }

            if (!string.IsNullOrWhiteSpace(UserMapStore.SaveMap(mapName, saveableElements, saveableExitPoints, saveableSpawnZones)))
            {
                currentMapName = mapName.Trim();
            }
        }

        private void OpenMap(string path)
        {
            var data = UserMapStore.LoadMap(path);

            if (data == null || spawner == null)
            {
                return;
            }

            spawner.ClearPlacedElements();
            ClearExitPoints();
            ClearSpawnZones();
            currentMapName = data.name;

            if (data.elements != null)
            {
                foreach (var element in data.elements)
                {
                    var sprite = UserElementAssetStore.LoadSprite(element.imagePath);

                    if (sprite != null)
                    {
                        spawner.CreatePlacedElement(element.imagePath, sprite, element.position, element.scale);
                    }
                }
            }

            if (data.exitPoints != null)
            {
                foreach (var exitPoint in data.exitPoints)
                {
                    CreateExitPoint(exitPoint.position, exitPoint.id, exitPoint.name, exitPoint.size);
                }
            }

            if (data.spawnZones == null)
            {
                return;
            }

            foreach (var spawnZone in data.spawnZones)
            {
                CreateSpawnZone(spawnZone.position, spawnZone.id, spawnZone.name, spawnZone.size);
            }
        }

        private static MapExitPoint CreateExitPoint(Vector3 position, string id, string displayName, Vector2 size)
        {
            var exitObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Exit Point" : displayName);
            position.z = -0.2f;
            exitObject.transform.position = position;
            var exitPoint = exitObject.AddComponent<MapExitPoint>();
            exitPoint.Initialize(
                string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
                string.IsNullOrWhiteSpace(displayName) ? "Exit" : displayName,
                size == Vector2.zero ? new Vector2(1.2f, 1.2f) : size);
            return exitPoint;
        }

        private static MapSpawnZone CreateSpawnZone(Vector3 position, string id, string displayName, Vector2 size)
        {
            var spawnObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Spawn Zone" : displayName);
            position.z = -0.18f;
            spawnObject.transform.position = position;
            var spawnZone = spawnObject.AddComponent<MapSpawnZone>();
            spawnZone.Initialize(
                string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
                string.IsNullOrWhiteSpace(displayName) ? "Spawn" : displayName,
                size == Vector2.zero ? new Vector2(1.2f, 1.2f) : size);
            return spawnZone;
        }

        private static void ClearExitPoints()
        {
            foreach (var exitPoint in FindObjectsByType<MapExitPoint>(FindObjectsInactive.Exclude))
            {
                Destroy(exitPoint.gameObject);
            }
        }

        private static void ClearSpawnZones()
        {
            foreach (var spawnZone in FindObjectsByType<MapSpawnZone>(FindObjectsInactive.Exclude))
            {
                Destroy(spawnZone.gameObject);
            }
        }

        private static Vector3 MouseWorld()
        {
            var camera = Camera.main;

            if (camera == null)
            {
                return Vector3.zero;
            }

            var mouse = MousePosition();
            mouse.z = Mathf.Abs(camera.transform.position.z);
            var world = camera.ScreenToWorldPoint(mouse);
            world.z = -0.2f;
            return world;
        }

        private static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector3.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static bool PrimaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
