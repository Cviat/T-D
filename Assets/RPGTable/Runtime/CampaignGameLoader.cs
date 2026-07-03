using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.Input;
using RPGTable.MapEditor;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.Runtime
{
    public sealed class CampaignGameLoader : MonoBehaviour
    {
        private const float CellSize = 1f;
        private const string DeadTokenAssetPath = "Assets/RPGTable/Art/Tokens/DeadToken.png";
        private const int PlayerViewLayer = 31;
        private const int PlayerViewLayerMask = 1 << PlayerViewLayer;
        private const float PlayerViewOrthographicSize = 8.5f;
        private const float PlayerViewCameraSmoothTime = 0.55f;
        private const float PlayerViewMinZoom = 4f;
        private const float PlayerViewMaxZoom = 18f;

        [SerializeField] private Camera worldCamera;

        private readonly Dictionary<string, SavedMapData> loadedMaps = new Dictionary<string, SavedMapData>();
        private readonly Dictionary<string, SavedCampaignMapNodeData> mapNodes = new Dictionary<string, SavedCampaignMapNodeData>();
        private readonly Dictionary<string, List<RuntimeMapTokenState>> mapTokenStates = new Dictionary<string, List<RuntimeMapTokenState>>();
        private readonly Dictionary<string, Vector2Int> occupiedCells = new Dictionary<string, Vector2Int>();
        private readonly Dictionary<string, string> pendingSpawnExitIds = new Dictionary<string, string>();
        private readonly Dictionary<string, Vector3> playerViewTokenPositions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Transform> playerViewTokenTransforms = new Dictionary<string, Transform>();
        private readonly HashSet<string> ignoredTransitionKeys = new HashSet<string>();

        private SavedCampaignData campaign;
        private SavedCampaignMapNodeData currentMapNode;
        private BoardGrid grid;
        private Transform mapRoot;
        private Transform tokenRoot;
        private RectTransform playerPanelRoot;
        private RectTransform mapPanelRoot;
        private RectTransform playerViewRosterRoot;
        private Text promptText;
        private GameObject promptPanel;
        private CampaignPlayerData pendingTransitionPlayer;
        private SavedCampaignLinkData pendingTransitionLink;
        private string selectedBankTokenPath;
        private string selectedPlayerId;
        private string playerViewStateKey;
        private string playerViewMapId;
        private GameObject playerViewControlButton;
        private int nextTokenSortingOrder = 100;
        private Sprite deadTokenSprite;
        private Camera playerViewCamera;
        private Transform playerViewRoot;
        private Vector3 playerViewGridOrigin;
        private Vector3 playerViewFollowTarget;
        private Vector3 playerViewManualOffset;
        private Vector3 playerViewCameraVelocity;
        private Vector3 lastPlayerViewMouseWorld;
        private Vector3 lastPlayerViewMouseScreen;
        private float playerViewTargetZoom = PlayerViewOrthographicSize;
        private bool playerViewCameraHasTarget;
        private bool playerViewDragging;

        public static bool PlayerViewCameraControlActive { get; private set; }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        private sealed class RuntimeMapTokenState
        {
            public string runtimeId;
            public string displayName;
            public string tokenPath;
            public TokenTeam team;
            public bool visibleToPlayers;
            public Vector2Int gridPosition;
            public bool isDead;
        }

        private void Start()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera != null)
            {
                worldCamera.cullingMask &= ~PlayerViewLayerMask;
            }

            BuildUi();
            BuildPlayerView();
            LoadSelectedCampaign();
        }

        private void Update()
        {
            HandleBankPlacement();
            SyncRuntimePlayerPositions();
            CheckPlayerTransitions();
            RefreshPlayerViewIfNeeded();
            HandlePlayerViewCameraPan();
            HandlePlayerViewCameraZoom();
            UpdatePlayerViewCamera();
        }

        private void LoadSelectedCampaign()
        {
            campaign = UserCampaignStore.LoadCampaign(CampaignGameSession.SelectedCampaignPath);

            if (campaign == null || campaign.maps == null || campaign.maps.Length == 0)
            {
                return;
            }

            mapNodes.Clear();

            foreach (var node in campaign.maps)
            {
                if (!string.IsNullOrWhiteSpace(node.id))
                {
                    mapNodes[node.id] = node;
                }
            }

            currentMapNode = FindStartMap();
            SelectDefaultPlayer();
            LoadMap(currentMapNode);
            RefreshMapPanel();
            RefreshPlayerPanel();
            RefreshPlayerView(true);
        }

        private SavedCampaignMapNodeData FindStartMap()
        {
            if (!string.IsNullOrWhiteSpace(campaign.startMapId) && mapNodes.TryGetValue(campaign.startMapId, out var startNode))
            {
                return startNode;
            }

            return campaign.maps[0];
        }

        private void LoadMap(SavedCampaignMapNodeData node)
        {
            if (node == null)
            {
                return;
            }

            SaveCurrentMapTokenState();
            currentMapNode = node;
            ClearWorld();

            var data = GetMap(node.mapPath);

            if (data == null)
            {
                return;
            }

            mapRoot = new GameObject("Loaded Map").transform;
            tokenRoot = new GameObject("Board Tokens").transform;

            var bounds = BuildMapElements(data);
            BuildGrid(bounds, data);
            BuildExitZones(data);
            SpawnPlayersForCurrentMap(data);
            SpawnStoredTokensForCurrentMap();
            FocusCamera(bounds);
            RefreshMapPanel();
            RefreshPlayerPanel();
            RefreshPlayerView(true);
        }

        private SavedMapData GetMap(string mapPath)
        {
            if (string.IsNullOrWhiteSpace(mapPath))
            {
                return null;
            }

            if (!loadedMaps.TryGetValue(mapPath, out var data))
            {
                data = UserMapStore.LoadMap(mapPath);
                loadedMaps[mapPath] = data;
            }

            return data;
        }

        private Bounds BuildMapElements(SavedMapData data)
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, new Vector3(12f, 8f, 0f));

            if (data.elements == null)
            {
                return bounds;
            }

            foreach (var element in data.elements)
            {
                var sprite = UserElementAssetStore.LoadSprite(element.imagePath);

                if (sprite == null)
                {
                    continue;
                }

                var objectName = string.IsNullOrWhiteSpace(sprite.name) ? "Map Element" : sprite.name;
                var gameObject = new GameObject(objectName);
                gameObject.transform.SetParent(mapRoot, false);
                gameObject.transform.position = element.position;
                gameObject.transform.localScale = element.scale;

                var renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -15;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private void BuildGrid(Bounds mapBounds, SavedMapData data)
        {
            mapBounds = ExpandBoundsForGrid(mapBounds, data);

            var gridObject = new GameObject("Board Grid");
            gridObject.transform.position = new Vector3(
                Mathf.Floor(mapBounds.min.x),
                Mathf.Floor(mapBounds.min.y),
                0f);

            grid = gridObject.AddComponent<BoardGrid>();
            grid.cellSize = CellSize;
            grid.width = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.x / CellSize));
            grid.height = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.y / CellSize));
            gridObject.AddComponent<BoardGridVisual>();
        }

        private void BuildExitZones(SavedMapData data)
        {
            if (data.exitPoints != null)
            {
                foreach (var exit in data.exitPoints)
                {
                    var zone = new GameObject(string.IsNullOrWhiteSpace(exit.name) ? "Exit Zone" : exit.name);
                    zone.transform.SetParent(mapRoot, false);
                    zone.transform.position = new Vector3(exit.position.x, exit.position.y, -0.05f);
                    zone.transform.localScale = new Vector3(Mathf.Max(0.1f, exit.size.x), Mathf.Max(0.1f, exit.size.y), 1f);

                    var renderer = zone.AddComponent<SpriteRenderer>();
                    renderer.sprite = RuntimeSpriteFactory.Square;
                    renderer.color = new Color(0.2f, 0.75f, 1f, 0.22f);
                    renderer.sortingOrder = -4;
                }
            }

            if (data.spawnZones == null)
            {
                return;
            }

            foreach (var spawn in data.spawnZones)
            {
                var zone = new GameObject(string.IsNullOrWhiteSpace(spawn.name) ? "Spawn Zone" : spawn.name);
                zone.transform.SetParent(mapRoot, false);
                zone.transform.position = new Vector3(spawn.position.x, spawn.position.y, -0.045f);
                zone.transform.localScale = new Vector3(Mathf.Max(0.1f, spawn.size.x), Mathf.Max(0.1f, spawn.size.y), 1f);

                var renderer = zone.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSpriteFactory.Square;
                renderer.color = new Color(0.25f, 1f, 0.35f, 0.18f);
                renderer.sortingOrder = -3;
            }
        }

        private void SpawnPlayersForCurrentMap(SavedMapData data)
        {
            occupiedCells.Clear();

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(player.tokenPath);
                var footprint = GetFootprint(tokenData);

                if (pendingSpawnExitIds.TryGetValue(player.id, out var spawnExitId))
                {
                    player.currentMapId = currentMapNode.id;
                    var spawn = FindSpawnCell(data, footprint, spawnExitId);
                    player.gridX = spawn.x;
                    player.gridY = spawn.y;
                    pendingSpawnExitIds.Remove(player.id);
                }
                else if (string.IsNullOrWhiteSpace(player.currentMapId))
                {
                    player.currentMapId = currentMapNode.id;
                    var spawn = FindSpawnCell(data, footprint, null);
                    player.gridX = spawn.x;
                    player.gridY = spawn.y;
                }

                if (player.currentMapId != currentMapNode.id)
                {
                    continue;
                }

                var cell = FindFreeCell(new Vector2Int(player.gridX, player.gridY), footprint);
                player.gridX = cell.x;
                player.gridY = cell.y;
                var runtimeToken = CreateTokenObject(player.name, player.tokenPath, cell, TokenTeam.Player, true, player.id);
                runtimeToken.IsDead = player.isDead;

                if (runtimeToken.IsDead)
                {
                    ApplyDeadVisual(runtimeToken);
                }

                ReserveCells(player.id, cell, footprint);
            }
        }

        private void SaveCurrentMapTokenState()
        {
            if (currentMapNode == null || tokenRoot == null || grid == null)
            {
                return;
            }

            var states = new List<RuntimeMapTokenState>();

            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (boardToken == null)
                {
                    continue;
                }

                boardToken.SnapToGrid(grid);

                if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                {
                    var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);

                    if (player != null)
                    {
                        player.currentMapId = currentMapNode.id;
                        player.gridX = boardToken.gridPosition.x;
                        player.gridY = boardToken.gridPosition.y;
                        player.isDead = runtimeToken.IsDead;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(runtimeToken.TokenPath))
                {
                    continue;
                }

                states.Add(new RuntimeMapTokenState
                {
                    runtimeId = EnsureRuntimeTokenId(runtimeToken),
                    displayName = runtimeToken.DisplayName,
                    tokenPath = runtimeToken.TokenPath,
                    team = runtimeToken.Team,
                    visibleToPlayers = runtimeToken.VisibleToPlayers,
                    gridPosition = boardToken.gridPosition,
                    isDead = runtimeToken.IsDead
                });
            }

            mapTokenStates[currentMapNode.id] = states;
        }

        private void SpawnStoredTokensForCurrentMap()
        {
            if (currentMapNode == null || !mapTokenStates.TryGetValue(currentMapNode.id, out var states))
            {
                return;
            }

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var tokenData = UserTokenStore.LoadToken(state.tokenPath);
                var footprint = GetFootprint(tokenData);
                var cell = FindFreeCell(state.gridPosition, footprint);
                var runtimeToken = CreateTokenObject(state.displayName, state.tokenPath, cell, state.team, state.visibleToPlayers, null);
                runtimeToken.RuntimeId = string.IsNullOrWhiteSpace(state.runtimeId) ? NewRuntimeTokenId() : state.runtimeId;
                runtimeToken.IsDead = state.isDead;

                if (runtimeToken.IsDead)
                {
                    ApplyDeadVisual(runtimeToken);
                }

                ReserveCells($"stored_{currentMapNode.id}_{i}", cell, footprint);
            }
        }

        private Vector2Int FindSpawnCell(SavedMapData data, int footprint, string entryExitId)
        {
            if (!string.IsNullOrWhiteSpace(entryExitId) && data.exitPoints != null)
            {
                foreach (var exit in data.exitPoints)
                {
                    if (exit.id == entryExitId)
                    {
                        if (TryFindFreeCellNearExit(exit, footprint, out var entryCell))
                        {
                            return entryCell;
                        }

                        return FindFreeCell(grid.WorldToCell(exit.position) + Vector2Int.right, footprint);
                    }
                }
            }

            if (data.spawnZones != null && data.spawnZones.Length > 0 && TryFindFreeCellInZone(data.spawnZones[0], footprint, out var cell))
            {
                return cell;
            }

            if (data.exitPoints != null && data.exitPoints.Length > 0)
            {
                return FindFreeCell(grid.WorldToCell(data.exitPoints[0].position), footprint);
            }

            return FindFreeCell(new Vector2Int(grid.width / 2, grid.height / 2), footprint);
        }

        private bool TryFindFreeCellNearExit(SavedMapExitPointData exit, int footprint, out Vector2Int cell)
        {
            var half = exit.size * 0.5f;
            var first = grid.WorldToCell(new Vector3(exit.position.x - half.x, exit.position.y - half.y, 0f));
            var second = grid.WorldToCell(new Vector3(exit.position.x + half.x, exit.position.y + half.y, 0f));
            var min = new Vector2Int(Mathf.Min(first.x, second.x), Mathf.Min(first.y, second.y));
            var max = new Vector2Int(Mathf.Max(first.x, second.x), Mathf.Max(first.y, second.y));
            var center = grid.WorldToCell(exit.position);
            var directions = new[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            for (var distance = 1; distance <= Mathf.Max(grid.width, grid.height); distance++)
            {
                foreach (var direction in directions)
                {
                    var candidate = direction.x != 0
                        ? new Vector2Int(direction.x > 0 ? max.x + distance : min.x - footprint - distance + 1, center.y)
                        : new Vector2Int(center.x, direction.y > 0 ? max.y + distance : min.y - footprint - distance + 1);

                    if (CanPlace(candidate, footprint) && !OverlapsExit(candidate, footprint, min, max))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            cell = default;
            return false;
        }

        private static bool OverlapsExit(Vector2Int origin, int footprint, Vector2Int min, Vector2Int max)
        {
            var tokenMax = origin + new Vector2Int(footprint - 1, footprint - 1);
            return origin.x <= max.x
                && tokenMax.x >= min.x
                && origin.y <= max.y
                && tokenMax.y >= min.y;
        }

        private bool TryFindFreeCellInZone(SavedMapSpawnZoneData spawnZone, int footprint, out Vector2Int cell)
        {
            var half = spawnZone.size * 0.5f;
            var first = grid.WorldToCell(new Vector3(spawnZone.position.x - half.x, spawnZone.position.y - half.y, 0f));
            var second = grid.WorldToCell(new Vector3(spawnZone.position.x + half.x, spawnZone.position.y + half.y, 0f));
            var preferred = grid.WorldToCell(spawnZone.position);

            var min = new Vector2Int(Mathf.Min(first.x, second.x), Mathf.Min(first.y, second.y));
            var max = new Vector2Int(Mathf.Max(first.x, second.x), Mathf.Max(first.y, second.y));

            preferred = new Vector2Int(
                Mathf.Clamp(preferred.x, min.x, max.x),
                Mathf.Clamp(preferred.y, min.y, max.y));

            if (CanPlaceInZone(preferred, footprint, min, max))
            {
                cell = preferred;
                return true;
            }

            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var candidate = new Vector2Int(x, y);

                    if (CanPlaceInZone(candidate, footprint, min, max))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            cell = default;
            return false;
        }

        private bool CanPlaceInZone(Vector2Int origin, int footprint, Vector2Int min, Vector2Int max)
        {
            for (var y = 0; y < footprint; y++)
            {
                for (var x = 0; x < footprint; x++)
                {
                    var cell = origin + new Vector2Int(x, y);

                    if (cell.x < min.x || cell.x > max.x || cell.y < min.y || cell.y > max.y)
                    {
                        return false;
                    }
                }
            }

            return CanPlace(origin, footprint);
        }

        private Vector2Int FindFreeCell(Vector2Int preferred, int footprint)
        {
            if (grid == null)
            {
                return Vector2Int.zero;
            }

            footprint = Mathf.Clamp(footprint, 1, Mathf.Max(1, Mathf.Min(grid.width, grid.height)));
            preferred = new Vector2Int(
                Mathf.Clamp(preferred.x, 0, Mathf.Max(0, grid.width - footprint)),
                Mathf.Clamp(preferred.y, 0, Mathf.Max(0, grid.height - footprint)));

            if (CanPlace(preferred, footprint))
            {
                return preferred;
            }

            for (var radius = 1; radius < Mathf.Max(grid.width, grid.height); radius++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        var cell = preferred + new Vector2Int(x, y);

                        if (CanPlace(cell, footprint))
                        {
                            return cell;
                        }
                    }
                }
            }

            return preferred;
        }

        private bool CanPlace(Vector2Int origin, int footprint)
        {
            for (var y = 0; y < footprint; y++)
            {
                for (var x = 0; x < footprint; x++)
                {
                    var cell = origin + new Vector2Int(x, y);

                    if (!grid.Contains(cell) || IsCellOccupied(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsCellOccupied(Vector2Int cell)
        {
            return occupiedCells.ContainsValue(cell);
        }

        private void ReserveCells(string ownerId, Vector2Int origin, int footprint)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                ownerId = $"token_{occupiedCells.Count}";
            }

            for (var y = 0; y < footprint; y++)
            {
                for (var x = 0; x < footprint; x++)
                {
                    occupiedCells[$"{ownerId}_{x}_{y}"] = origin + new Vector2Int(x, y);
                }
            }
        }

        private CampaignRuntimeToken CreateTokenObject(string displayName, string tokenPath, Vector2Int cell, TokenTeam team, bool visibleToPlayers, string playerId)
        {
            var tokenData = UserTokenStore.LoadToken(tokenPath);
            var footprint = GetFootprint(tokenData);
            var sortingBase = nextTokenSortingOrder;
            nextTokenSortingOrder += 10;
            var tokenObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Token" : displayName);
            tokenObject.transform.SetParent(tokenRoot, false);
            tokenObject.transform.position = TokenWorldPosition(cell, footprint);

            var renderer = tokenObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.Square;
            renderer.sortingOrder = sortingBase;
            renderer.color = new Color(1f, 1f, 1f, 0f);

            var collider = tokenObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * footprint;

            var token = tokenObject.AddComponent<BoardToken>();
            token.displayName = displayName;
            token.team = team;
            token.visibleToPlayers = visibleToPlayers;
            token.gridPosition = cell;
            token.footprintSize = footprint;

            CreateTokenVisual(tokenObject.transform, tokenData, footprint, team, sortingBase);

            var runtime = tokenObject.AddComponent<CampaignRuntimeToken>();
            runtime.PlayerId = playerId;
            runtime.RuntimeId = string.IsNullOrWhiteSpace(playerId) ? NewRuntimeTokenId() : playerId;
            runtime.TokenPath = tokenPath;
            runtime.DisplayName = displayName;
            runtime.Team = team;
            runtime.VisibleToPlayers = visibleToPlayers;
            tokenObject.AddComponent<TokenDragController>();
            tokenObject.AddComponent<CampaignTokenContextClick>().Initialize(this, runtime);
            return runtime;
        }

        private static string EnsureRuntimeTokenId(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return NewRuntimeTokenId();
            }

            if (string.IsNullOrWhiteSpace(runtimeToken.RuntimeId))
            {
                runtimeToken.RuntimeId = string.IsNullOrWhiteSpace(runtimeToken.PlayerId)
                    ? NewRuntimeTokenId()
                    : runtimeToken.PlayerId;
            }

            return runtimeToken.RuntimeId;
        }

        private static string NewRuntimeTokenId()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        public void DeleteRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return;
            }

            RebuildOccupiedCells(runtimeToken);
            Destroy(runtimeToken.gameObject);
        }

        public void KillRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return;
            }

            runtimeToken.IsDead = true;

            if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
            {
                var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);

                if (player != null)
                {
                    player.isDead = true;
                }
            }

            ApplyDeadVisual(runtimeToken);
        }

        private void RebuildOccupiedCells(CampaignRuntimeToken ignoredToken = null)
        {
            occupiedCells.Clear();

            if (tokenRoot == null || grid == null)
            {
                return;
            }

            var runtimeIndex = 0;

            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (runtimeToken == null || runtimeToken == ignoredToken)
                {
                    continue;
                }

                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (boardToken == null)
                {
                    continue;
                }

                boardToken.SnapToGrid(grid);
                var ownerId = !string.IsNullOrWhiteSpace(runtimeToken.PlayerId)
                    ? runtimeToken.PlayerId
                    : $"runtime_{runtimeIndex++}";
                ReserveCells(ownerId, boardToken.gridPosition, Mathf.Max(1, boardToken.footprintSize));
            }
        }

        private void ApplyDeadVisual(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return;
            }

            for (var i = runtimeToken.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(runtimeToken.transform.GetChild(i).gameObject);
            }

            var boardToken = runtimeToken.GetComponent<BoardToken>();
            var footprint = Mathf.Max(1, boardToken == null ? 1 : boardToken.footprintSize);
            var targetSize = Mathf.Max(0.1f, footprint * CellSize);
            var rootRenderer = runtimeToken.GetComponent<SpriteRenderer>();
            var sortingOrder = rootRenderer == null ? nextTokenSortingOrder : rootRenderer.sortingOrder + 5;
            var deadSprite = LoadDeadTokenSprite();
            var layer = CreateSpriteLayer("Dead Token", runtimeToken.transform, deadSprite, sortingOrder, Color.white);
            FitSprite(layer.transform, deadSprite, targetSize);
        }

        private Sprite LoadDeadTokenSprite()
        {
            if (deadTokenSprite != null)
            {
                return deadTokenSprite;
            }

            var path = Path.Combine(Application.dataPath, DeadTokenAssetPath.Substring("Assets/".Length));
            deadTokenSprite = UserTokenStore.LoadSprite(path);

            if (deadTokenSprite == null)
            {
                deadTokenSprite = RuntimeSpriteFactory.Circle;
            }

            return deadTokenSprite;
        }

        private Vector3 TokenWorldPosition(Vector2Int cell, int footprint)
        {
            return grid.CellToWorld(cell) + new Vector3((footprint - 1) * grid.cellSize * 0.5f, (footprint - 1) * grid.cellSize * 0.5f, 0f);
        }

        private static int GetFootprint(SavedTokenData tokenData)
        {
            return Mathf.Clamp(tokenData == null ? 1 : tokenData.footprintSize, 1, 5);
        }

        private void CreateTokenVisual(Transform parent, SavedTokenData tokenData, int footprint, TokenTeam team, int sortingBase)
        {
            var targetSize = Mathf.Max(0.1f, footprint * CellSize);
            var maskLayout = ResolveMaskLayout(tokenData, targetSize);
            var portraitSprite = UserTokenStore.LoadSprite(tokenData?.portraitPath);
            var frameSprite = UserTokenStore.LoadSprite(tokenData?.framePath);

            if (portraitSprite == null && frameSprite == null)
            {
                var fallback = CreateSpriteLayer("Fallback", parent, RuntimeSpriteFactory.Circle, sortingBase + 2, team == TokenTeam.Player ? Color.white : new Color(1f, 0.82f, 0.65f, 1f));
                FitSprite(fallback.transform, RuntimeSpriteFactory.Circle, targetSize);
                return;
            }

            if (portraitSprite != null)
            {
                CreatePortraitMask(parent, maskLayout.position, maskLayout.size, sortingBase + 1, sortingBase + 3);
                var portrait = CreateSpriteLayer("Portrait", parent, portraitSprite, sortingBase + 2, Color.white);
                portrait.transform.localPosition = maskLayout.position;
                portrait.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                FitSpriteCover(portrait.transform, portraitSprite, maskLayout.size);
            }

            if (frameSprite != null)
            {
                var frame = CreateSpriteLayer("Frame", parent, frameSprite, sortingBase + 4, Color.white);
                FitSprite(frame.transform, frameSprite, targetSize);
            }
        }

        private static (Vector3 position, Vector2 size) ResolveMaskLayout(SavedTokenData tokenData, float targetSize)
        {
            var positionRatio = new Vector2(0f, 44f / 560f);
            var sizeRatio = new Vector2(360f / 560f, 360f / 560f);

            if (tokenData != null && tokenData.hasPortraitMaskLayout)
            {
                positionRatio = tokenData.portraitMaskPositionRatio;
                sizeRatio = tokenData.portraitMaskSizeRatio;
            }

            return (
                new Vector3(positionRatio.x * targetSize, positionRatio.y * targetSize, 0f),
                new Vector2(Mathf.Max(0.05f, sizeRatio.x * targetSize), Mathf.Max(0.05f, sizeRatio.y * targetSize)));
        }

        private static void CreatePortraitMask(Transform parent, Vector3 position, Vector2 size, int backSortingOrder, int frontSortingOrder)
        {
            var maskObject = new GameObject("Portrait Mask");
            maskObject.transform.SetParent(parent, false);
            maskObject.transform.localPosition = position;
            FitSprite(maskObject.transform, RuntimeSpriteFactory.Circle, size);

            var mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = RuntimeSpriteFactory.Circle;
            mask.isCustomRangeActive = true;
            mask.backSortingOrder = backSortingOrder;
            mask.frontSortingOrder = frontSortingOrder;
        }

        private static GameObject CreateSpriteLayer(string name, Transform parent, Sprite sprite, int sortingOrder, Color color)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = Vector3.zero;
            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            return layer;
        }

        private static void FitSprite(Transform transform, Sprite sprite, float targetSize)
        {
            if (sprite == null)
            {
                return;
            }

            var spriteSize = sprite.bounds.size;
            var maxSide = Mathf.Max(0.01f, Mathf.Max(spriteSize.x, spriteSize.y));
            var scale = targetSize / maxSide;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void FitSprite(Transform transform, Sprite sprite, Vector2 targetSize)
        {
            if (sprite == null)
            {
                return;
            }

            var spriteSize = sprite.bounds.size;
            transform.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.01f, spriteSize.x),
                targetSize.y / Mathf.Max(0.01f, spriteSize.y),
                1f);
        }

        private static void FitSpriteCover(Transform transform, Sprite sprite, float targetSize)
        {
            if (sprite == null)
            {
                return;
            }

            var spriteSize = sprite.bounds.size;
            var minSide = Mathf.Max(0.01f, Mathf.Min(spriteSize.x, spriteSize.y));
            var scale = targetSize / minSide;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void FitSpriteCover(Transform transform, Sprite sprite, Vector2 targetSize)
        {
            if (sprite == null)
            {
                return;
            }

            var spriteSize = sprite.bounds.size;
            var scale = Mathf.Max(
                targetSize.x / Mathf.Max(0.01f, spriteSize.x),
                targetSize.y / Mathf.Max(0.01f, spriteSize.y));
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void HandleBankPlacement()
        {
            if (string.IsNullOrWhiteSpace(selectedBankTokenPath) || grid == null || worldCamera == null)
            {
                return;
            }

            if (PlayerViewCameraControlActive || !MouseOnDisplay(worldCamera.targetDisplay) || !PrimaryMousePressed() || IsPointerOverUi())
            {
                return;
            }

            var mouse = MousePosition();
            mouse.z = Mathf.Abs(worldCamera.transform.position.z);
            var world = worldCamera.ScreenToWorldPoint(mouse);
            var tokenData = UserTokenStore.LoadToken(selectedBankTokenPath);
            var footprint = GetFootprint(tokenData);
            var cell = FindFreeCell(grid.WorldToCell(world), footprint);
            CreateTokenObject(UserTokenStore.GetDisplayName(selectedBankTokenPath), selectedBankTokenPath, cell, TokenTeam.Enemy, true, null);
            ReserveCells(null, cell, footprint);
            selectedBankTokenPath = null;
        }

        private void CheckPlayerTransitions()
        {
            if (promptPanel != null && promptPanel.activeSelf)
            {
                return;
            }

            if (campaign?.links == null || currentMapNode == null)
            {
                return;
            }

            var data = GetMap(currentMapNode.mapPath);

            if (data?.exitPoints == null)
            {
                return;
            }

            var activeTransitionKeys = new HashSet<string>();

            if (tokenRoot == null)
            {
                return;
            }

            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                {
                    continue;
                }

                var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);

                if (player == null)
                {
                    continue;
                }

                if (player.currentMapId != currentMapNode.id)
                {
                    continue;
                }

                var boardToken = runtimeToken.GetComponent<BoardToken>();
                var tokenCell = boardToken == null ? grid.WorldToCell(runtimeToken.transform.position) : boardToken.gridPosition;
                player.gridX = tokenCell.x;
                player.gridY = tokenCell.y;

                foreach (var exit in data.exitPoints)
                {
                    if (!Contains(exit, runtimeToken.transform.position))
                    {
                        continue;
                    }

                    var link = FindLinkFrom(exit.id);
                    var transitionKey = TransitionKey(player.id, currentMapNode.id, exit.id);
                    activeTransitionKeys.Add(transitionKey);

                    if (link != null && !ignoredTransitionKeys.Contains(transitionKey))
                    {
                        ShowTransitionPrompt(player, link);
                        return;
                    }
                }
            }

            RemoveInactiveIgnoredTransitions(activeTransitionKeys);
        }

        private void SyncRuntimePlayerPositions()
        {
            if (currentMapNode == null || tokenRoot == null || grid == null)
            {
                return;
            }

            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                {
                    continue;
                }

                var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);
                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (player == null || boardToken == null)
                {
                    continue;
                }

                player.currentMapId = currentMapNode.id;
                player.gridX = boardToken.gridPosition.x;
                player.gridY = boardToken.gridPosition.y;
                player.isDead = runtimeToken.IsDead;
            }
        }

        private SavedCampaignLinkData FindLinkFrom(string exitId)
        {
            foreach (var link in campaign.links)
            {
                if (link.fromMapId == currentMapNode.id && link.fromExitId == exitId)
                {
                    return link;
                }
            }

            foreach (var link in campaign.links)
            {
                if (link.toMapId == currentMapNode.id && link.toExitId == exitId)
                {
                    return new SavedCampaignLinkData
                    {
                        fromMapId = currentMapNode.id,
                        fromExitId = exitId,
                        toMapId = link.fromMapId,
                        toExitId = link.fromExitId
                    };
                }
            }

            return null;
        }

        private static bool Contains(SavedMapExitPointData exit, Vector3 world)
        {
            var half = exit.size * 0.5f;
            return world.x >= exit.position.x - half.x
                && world.x <= exit.position.x + half.x
                && world.y >= exit.position.y - half.y
                && world.y <= exit.position.y + half.y;
        }

        private void ShowTransitionPrompt(CampaignPlayerData player, SavedCampaignLinkData link)
        {
            pendingTransitionPlayer = player;
            pendingTransitionLink = link;

            if (promptText != null)
            {
                promptText.text = $"Перейти на другую карту: {player.name}?";
            }

            if (promptText != null)
            {
                var targetName = mapNodes.TryGetValue(link.toMapId, out var targetNode)
                    ? GetMap(targetNode.mapPath)?.name
                    : null;
                promptText.text = string.IsNullOrWhiteSpace(targetName)
                    ? $"Перейти на другую карту: {player.name}?"
                    : $"Перейти на карту \"{targetName}\": {player.name}?";
            }

            if (promptPanel != null)
            {
                promptPanel.SetActive(true);
            }
        }

        private void ConfirmTransition()
        {
            if (pendingTransitionPlayer == null || pendingTransitionLink == null || !mapNodes.TryGetValue(pendingTransitionLink.toMapId, out var targetNode))
            {
                HideTransitionPrompt();
                return;
            }

            pendingTransitionPlayer.currentMapId = targetNode.id;
            pendingSpawnExitIds[pendingTransitionPlayer.id] = pendingTransitionLink.toExitId;
            ignoredTransitionKeys.Add(TransitionKey(pendingTransitionPlayer.id, targetNode.id, pendingTransitionLink.toExitId));
            HideTransitionPrompt();
            LoadMap(targetNode);
        }

        private void HideTransitionPrompt()
        {
            if (pendingTransitionPlayer != null && pendingTransitionLink != null)
            {
                ignoredTransitionKeys.Add(TransitionKey(pendingTransitionPlayer.id, pendingTransitionLink.fromMapId, pendingTransitionLink.fromExitId));
            }

            pendingTransitionPlayer = null;
            pendingTransitionLink = null;

            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }
        }

        private static string TransitionKey(string playerId, string mapId, string exitId)
        {
            return $"{playerId}|{mapId}|{exitId}";
        }

        private void RemoveInactiveIgnoredTransitions(HashSet<string> activeTransitionKeys)
        {
            var toRemove = new List<string>();

            foreach (var key in ignoredTransitionKeys)
            {
                if (!activeTransitionKeys.Contains(key))
                {
                    toRemove.Add(key);
                }
            }

            foreach (var key in toRemove)
            {
                ignoredTransitionKeys.Remove(key);
            }
        }

        private void SwitchMap(string mapId)
        {
            if (mapNodes.TryGetValue(mapId, out var node))
            {
                LoadMap(node);
            }
        }

        private void ClearWorld()
        {
            DestroyIfExists(mapRoot);
            DestroyIfExists(tokenRoot);

            if (grid != null)
            {
                Destroy(grid.gameObject);
                grid = null;
            }
        }

        private static void DestroyIfExists(Transform root)
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }

        private void FocusCamera(Bounds bounds)
        {
            if (worldCamera == null)
            {
                return;
            }

            var center = bounds.center;
            worldCamera.transform.position = new Vector3(center.x, center.y, -10f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = Mathf.Max(6f, Mathf.Max(bounds.extents.y, bounds.extents.x / worldCamera.aspect) * 1.25f);
        }

        private void BuildPlayerView()
        {
            if (Display.displays.Length > 1)
            {
                Display.displays[1].Activate();
            }

            var cameraObject = new GameObject("Player View Camera");
            playerViewCamera = cameraObject.AddComponent<Camera>();
            playerViewCamera.clearFlags = CameraClearFlags.SolidColor;
            playerViewCamera.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            playerViewCamera.orthographic = true;
            playerViewCamera.orthographicSize = playerViewTargetZoom;
            playerViewCamera.depth = -10f;
            playerViewCamera.targetDisplay = 1;
            playerViewCamera.cullingMask = PlayerViewLayerMask;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var canvasObject = new GameObject("Player View Interface", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = playerViewCamera.targetDisplay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var rosterPanel = CreatePanel("Player View Roster", canvasObject.transform, new Color(0.035f, 0.033f, 0.030f, 0.86f));
            var rosterRect = rosterPanel.GetComponent<RectTransform>();
            rosterRect.anchorMin = new Vector2(0f, 0f);
            rosterRect.anchorMax = new Vector2(0f, 1f);
            rosterRect.pivot = new Vector2(0f, 0.5f);
            rosterRect.sizeDelta = new Vector2(112f, 0f);
            rosterRect.anchoredPosition = Vector2.zero;

            playerViewRosterRoot = CreateVerticalList(
                "Player View Icons",
                rosterPanel.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(10f, 10f),
                new Vector2(-10f, -10f),
                8f);
        }

        private void SelectDefaultPlayer()
        {
            if (!string.IsNullOrWhiteSpace(selectedPlayerId) && CampaignGameSession.FindPlayer(selectedPlayerId) != null)
            {
                return;
            }

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (!string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    selectedPlayerId = player.id;
                    return;
                }
            }

            selectedPlayerId = CampaignGameSession.CurrentPlayers.Count > 0 ? CampaignGameSession.CurrentPlayers[0].id : null;
        }

        private void SelectPlayer(CampaignPlayerData player)
        {
            if (player == null)
            {
                return;
            }

            selectedPlayerId = player.id;
            playerViewManualOffset = Vector3.zero;
            playerViewCameraVelocity = Vector3.zero;
            RefreshPlayerPanel();
            RefreshPlayerView(true);
        }

        private void RefreshPlayerViewIfNeeded()
        {
            var stateKey = BuildPlayerViewStateKey();

            if (stateKey == playerViewStateKey)
            {
                return;
            }

            var selectedPlayer = CampaignGameSession.FindPlayer(selectedPlayerId);

            if (selectedPlayer != null && TrySyncPlayerViewInPlace(selectedPlayer.currentMapId))
            {
                playerViewStateKey = stateKey;
                return;
            }

            RefreshPlayerView(true);
        }

        private string BuildPlayerViewStateKey()
        {
            var key = selectedPlayerId ?? string.Empty;

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                key += $"|{player.id}:{player.currentMapId}:{player.gridX}:{player.gridY}:{player.tokenPath}:{player.isDead}";
            }

            var selectedPlayer = CampaignGameSession.FindPlayer(selectedPlayerId);

            if (selectedPlayer != null && selectedPlayer.currentMapId == currentMapNode?.id && tokenRoot != null)
            {
                foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
                {
                    if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                    {
                        continue;
                    }

                    var boardToken = runtimeToken.GetComponent<BoardToken>();
                    key += $"|token:{EnsureRuntimeTokenId(runtimeToken)}:{runtimeToken.TokenPath}:{runtimeToken.VisibleToPlayers}:{runtimeToken.IsDead}:{boardToken?.gridPosition.x}:{boardToken?.gridPosition.y}";
                }
            }

            return key;
        }

        private void RefreshPlayerView(bool force)
        {
            if (playerViewCamera == null)
            {
                return;
            }

            SelectDefaultPlayer();
            var selectedPlayer = CampaignGameSession.FindPlayer(selectedPlayerId);

            if (selectedPlayer == null || string.IsNullOrWhiteSpace(selectedPlayer.currentMapId))
            {
                RefreshPlayerViewRoster(null);
                return;
            }

            if (!mapNodes.TryGetValue(selectedPlayer.currentMapId, out var selectedNode))
            {
                RefreshPlayerViewRoster(null);
                return;
            }

            var data = GetMap(selectedNode.mapPath);

            if (data == null)
            {
                RefreshPlayerViewRoster(null);
                return;
            }

            if (playerViewMapId != selectedPlayer.currentMapId)
            {
                playerViewTokenPositions.Clear();
            }

            DestroyPlayerViewRoot();
            playerViewRoot = new GameObject("Player View World").transform;
            playerViewMapId = selectedPlayer.currentMapId;

            var bounds = BuildPlayerViewMap(data, playerViewRoot);
            BuildPlayerViewGrid(bounds, data, playerViewRoot);
            var activeTokenKeys = SpawnPlayerViewTokens(selectedPlayer.currentMapId, playerViewRoot);
            PrunePlayerViewTokenPositions(activeTokenKeys);
            SetLayerRecursively(playerViewRoot.gameObject, PlayerViewLayer);
            SetPlayerViewCameraTarget(selectedPlayer, bounds);
            RefreshPlayerViewRoster(selectedPlayer.currentMapId);
            playerViewStateKey = BuildPlayerViewStateKey();
        }

        private void DestroyPlayerViewRoot()
        {
            if (playerViewRoot == null)
            {
                return;
            }

            playerViewRoot.gameObject.SetActive(false);
            Destroy(playerViewRoot.gameObject);
            playerViewRoot = null;
            playerViewTokenTransforms.Clear();
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.layer = layer;

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                SetLayerRecursively(gameObject.transform.GetChild(i).gameObject, layer);
            }
        }

        private Bounds BuildPlayerViewMap(SavedMapData data, Transform root)
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, new Vector3(12f, 8f, 0f));

            if (data.elements == null)
            {
                return bounds;
            }

            foreach (var element in data.elements)
            {
                var sprite = UserElementAssetStore.LoadSprite(element.imagePath);

                if (sprite == null)
                {
                    continue;
                }

                var gameObject = new GameObject(string.IsNullOrWhiteSpace(sprite.name) ? "Player View Map Element" : sprite.name);
                gameObject.transform.SetParent(root, false);
                gameObject.transform.position = element.position;
                gameObject.transform.localScale = element.scale;

                var renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -15;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private void BuildPlayerViewGrid(Bounds mapBounds, SavedMapData data, Transform root)
        {
            mapBounds = ExpandBoundsForGrid(mapBounds, data);

            var gridObject = new GameObject("Player View Grid");
            gridObject.transform.SetParent(root, false);
            playerViewGridOrigin = new Vector3(Mathf.Floor(mapBounds.min.x), Mathf.Floor(mapBounds.min.y), 0f);
            gridObject.transform.position = playerViewGridOrigin;

            var playerGrid = gridObject.AddComponent<BoardGrid>();
            playerGrid.cellSize = CellSize;
            playerGrid.width = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.x / CellSize));
            playerGrid.height = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.y / CellSize));
            gridObject.AddComponent<BoardGridVisual>();
        }

        private static Bounds ExpandBoundsForGrid(Bounds mapBounds, SavedMapData data)
        {
            if (data.exitPoints != null)
            {
                foreach (var exit in data.exitPoints)
                {
                    mapBounds.Encapsulate(new Bounds(exit.position, new Vector3(exit.size.x, exit.size.y, 0f)));
                }
            }

            if (data.spawnZones != null)
            {
                foreach (var spawn in data.spawnZones)
                {
                    mapBounds.Encapsulate(new Bounds(spawn.position, new Vector3(spawn.size.x, spawn.size.y, 0f)));
                }
            }

            mapBounds.Expand(new Vector3(2f, 2f, 0f));
            return mapBounds;
        }

        private bool TrySyncPlayerViewInPlace(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId) || playerViewRoot == null || mapId != playerViewMapId)
            {
                return false;
            }

            var activeTokenKeys = new HashSet<string>();

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (player.currentMapId != mapId || string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(player.tokenPath);
                var footprint = GetFootprint(tokenData);
                var targetPosition = PlayerViewTokenWorldPosition(new Vector2Int(player.gridX, player.gridY), footprint);
                var key = PlayerViewTokenKey("player", mapId, player.id);

                if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                {
                    return false;
                }
            }

            if (mapId == currentMapNode?.id && tokenRoot != null)
            {
                if (!SyncRuntimePlayerViewTokens(mapId, activeTokenKeys))
                {
                    return false;
                }
            }
            else if (mapTokenStates.TryGetValue(mapId, out var states))
            {
                for (var i = 0; i < states.Count; i++)
                {
                    var state = states[i];

                    if (!state.visibleToPlayers || string.IsNullOrWhiteSpace(state.tokenPath))
                    {
                        continue;
                    }

                    var tokenData = UserTokenStore.LoadToken(state.tokenPath);
                    var footprint = GetFootprint(tokenData);
                    var targetPosition = PlayerViewTokenWorldPosition(state.gridPosition, footprint);
                var key = PlayerViewTokenKey("stored", mapId, string.IsNullOrWhiteSpace(state.runtimeId) ? i.ToString() : state.runtimeId);

                    if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                    {
                        return false;
                    }
                }
            }

            foreach (var key in playerViewTokenTransforms.Keys)
            {
                if (!activeTokenKeys.Contains(key))
                {
                    return false;
                }
            }

            SetPlayerViewCameraTarget(CampaignGameSession.FindPlayer(selectedPlayerId), new Bounds(playerViewFollowTarget, Vector3.zero));
            RefreshPlayerViewRoster(mapId);
            return true;
        }

        private bool SyncRuntimePlayerViewTokens(string mapId, HashSet<string> activeTokenKeys)
        {
            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId) || !runtimeToken.VisibleToPlayers || string.IsNullOrWhiteSpace(runtimeToken.TokenPath))
                {
                    continue;
                }

                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (boardToken == null)
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(runtimeToken.TokenPath);
                var footprint = GetFootprint(tokenData);
                var targetPosition = PlayerViewTokenWorldPosition(boardToken.gridPosition, footprint);
                var key = PlayerViewTokenKey("runtime", mapId, EnsureRuntimeTokenId(runtimeToken));

                if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MoveExistingPlayerViewToken(string key, Vector3 targetPosition, HashSet<string> activeTokenKeys)
        {
            activeTokenKeys.Add(key);

            if (!playerViewTokenTransforms.TryGetValue(key, out var tokenTransform) || tokenTransform == null)
            {
                return false;
            }

            playerViewTokenPositions[key] = targetPosition;

            if ((tokenTransform.position - targetPosition).sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            var mover = tokenTransform.GetComponent<PlayerViewTokenMover>();

            if (mover == null)
            {
                mover = tokenTransform.gameObject.AddComponent<PlayerViewTokenMover>();
            }

            mover.Initialize(targetPosition);
            return true;
        }

        private HashSet<string> SpawnPlayerViewTokens(string mapId, Transform root)
        {
            var activeTokenKeys = new HashSet<string>();

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (player.currentMapId != mapId || string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(player.tokenPath);
                var footprint = GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(player.name) ? "Player Token" : player.name);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(new Vector2Int(player.gridX, player.gridY), footprint);
                PlacePlayerViewToken(tokenObject.transform, PlayerViewTokenKey("player", mapId, player.id), targetPosition, activeTokenKeys);

                CreateTokenVisual(tokenObject.transform, tokenData, footprint, TokenTeam.Player, nextTokenSortingOrder);
                nextTokenSortingOrder += 10;

                if (player.isDead)
                {
                    var runtimeToken = tokenObject.AddComponent<CampaignRuntimeToken>();
                    ApplyDeadVisual(runtimeToken);
                }
            }

            if (mapId == currentMapNode?.id && tokenRoot != null)
            {
                SpawnPlayerViewRuntimeTokens(mapId, root, activeTokenKeys);
                return activeTokenKeys;
            }

            if (!mapTokenStates.TryGetValue(mapId, out var states))
            {
                return activeTokenKeys;
            }

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];

                if (!state.visibleToPlayers || string.IsNullOrWhiteSpace(state.tokenPath))
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(state.tokenPath);
                var footprint = GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(state.displayName) ? "Visible Token" : state.displayName);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(state.gridPosition, footprint);
                PlacePlayerViewToken(tokenObject.transform, PlayerViewTokenKey("stored", mapId, string.IsNullOrWhiteSpace(state.runtimeId) ? i.ToString() : state.runtimeId), targetPosition, activeTokenKeys);

                CreateTokenVisual(tokenObject.transform, tokenData, footprint, state.team, nextTokenSortingOrder);
                nextTokenSortingOrder += 10;

                if (state.isDead)
                {
                    var runtimeToken = tokenObject.AddComponent<CampaignRuntimeToken>();
                    ApplyDeadVisual(runtimeToken);
                }
            }

            return activeTokenKeys;
        }

        private void SpawnPlayerViewRuntimeTokens(string mapId, Transform root, HashSet<string> activeTokenKeys)
        {
            foreach (var runtimeToken in tokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId) || !runtimeToken.VisibleToPlayers || string.IsNullOrWhiteSpace(runtimeToken.TokenPath))
                {
                    continue;
                }

                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (boardToken == null)
                {
                    continue;
                }

                var tokenData = UserTokenStore.LoadToken(runtimeToken.TokenPath);
                var footprint = GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(runtimeToken.DisplayName) ? "Visible Token" : runtimeToken.DisplayName);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(boardToken.gridPosition, footprint);
                PlacePlayerViewToken(tokenObject.transform, PlayerViewTokenKey("runtime", mapId, EnsureRuntimeTokenId(runtimeToken)), targetPosition, activeTokenKeys);

                CreateTokenVisual(tokenObject.transform, tokenData, footprint, runtimeToken.Team, nextTokenSortingOrder);
                nextTokenSortingOrder += 10;

                if (runtimeToken.IsDead)
                {
                    var playerViewRuntimeToken = tokenObject.AddComponent<CampaignRuntimeToken>();
                    ApplyDeadVisual(playerViewRuntimeToken);
                }
            }
        }

        private void PlacePlayerViewToken(Transform tokenTransform, string key, Vector3 targetPosition, HashSet<string> activeTokenKeys)
        {
            activeTokenKeys.Add(key);

            if (playerViewTokenPositions.TryGetValue(key, out var previousPosition))
            {
                tokenTransform.position = previousPosition;
                tokenTransform.gameObject.AddComponent<PlayerViewTokenMover>().Initialize(targetPosition);
            }
            else
            {
                tokenTransform.position = targetPosition;
            }

            playerViewTokenPositions[key] = targetPosition;
            playerViewTokenTransforms[key] = tokenTransform;
        }

        private Vector3 PlayerViewTokenWorldPosition(Vector2Int cell, int footprint)
        {
            return playerViewGridOrigin + new Vector3(
                (cell.x + 0.5f) * CellSize + (footprint - 1) * CellSize * 0.5f,
                (cell.y + 0.5f) * CellSize + (footprint - 1) * CellSize * 0.5f,
                0f);
        }

        private void PrunePlayerViewTokenPositions(HashSet<string> activeTokenKeys)
        {
            var staleKeys = new List<string>();

            foreach (var key in playerViewTokenPositions.Keys)
            {
                if (!activeTokenKeys.Contains(key))
                {
                    staleKeys.Add(key);
                }
            }

            foreach (var key in staleKeys)
            {
                playerViewTokenPositions.Remove(key);
                playerViewTokenTransforms.Remove(key);
            }
        }

        private static string PlayerViewTokenKey(string type, string mapId, string id)
        {
            return $"{type}:{mapId}:{id}";
        }

        private void SetPlayerViewCameraTarget(CampaignPlayerData selectedPlayer, Bounds fallbackBounds)
        {
            if (playerViewCamera == null)
            {
                return;
            }

            var center = fallbackBounds.center;

            if (selectedPlayer != null)
            {
                var tokenData = string.IsNullOrWhiteSpace(selectedPlayer.tokenPath) ? null : UserTokenStore.LoadToken(selectedPlayer.tokenPath);
                center = PlayerViewTokenWorldPosition(new Vector2Int(selectedPlayer.gridX, selectedPlayer.gridY), GetFootprint(tokenData));
            }

            playerViewFollowTarget = new Vector3(center.x, center.y, 0f);

            if (!playerViewCameraHasTarget)
            {
                playerViewCamera.transform.position = new Vector3(
                    playerViewFollowTarget.x + playerViewManualOffset.x,
                    playerViewFollowTarget.y + playerViewManualOffset.y,
                    -10f);
                playerViewCamera.orthographic = true;
                playerViewCamera.orthographicSize = playerViewTargetZoom;
                playerViewCameraHasTarget = true;
            }
        }

        private void UpdatePlayerViewCamera()
        {
            if (playerViewCamera == null || !playerViewCameraHasTarget)
            {
                return;
            }

            var desired = playerViewFollowTarget + playerViewManualOffset;
            desired.z = -10f;
            playerViewCamera.transform.position = Vector3.SmoothDamp(
                playerViewCamera.transform.position,
                desired,
                ref playerViewCameraVelocity,
                PlayerViewCameraSmoothTime);
            playerViewCamera.orthographic = true;
            playerViewCamera.orthographicSize = Mathf.Lerp(
                playerViewCamera.orthographicSize,
                playerViewTargetZoom,
                Time.deltaTime * 5f);
        }

        private void HandlePlayerViewCameraZoom()
        {
            if (!PlayerViewCameraControlActive || playerViewCamera == null)
            {
                return;
            }

            var scroll = ScrollDelta();

            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            playerViewTargetZoom = Mathf.Clamp(playerViewTargetZoom - scroll * 1.4f, PlayerViewMinZoom, PlayerViewMaxZoom);
        }

        private void HandlePlayerViewCameraPan()
        {
            if (playerViewCamera == null || (!PlayerViewCameraControlActive && !MouseOnPlayerViewDisplay()))
            {
                playerViewDragging = false;
                return;
            }

            if (PlayerViewDragStarted())
            {
                playerViewDragging = true;
                lastPlayerViewMouseWorld = PlayerViewMouseWorldPosition();
                lastPlayerViewMouseScreen = PlayerViewControlMousePosition();
            }

            if (PlayerViewDragEnded())
            {
                playerViewDragging = false;
            }

            if (!playerViewDragging)
            {
                return;
            }

            if (PlayerViewCameraControlActive)
            {
                var currentMouseScreen = PlayerViewControlMousePosition();
                var screenDelta = lastPlayerViewMouseScreen - currentMouseScreen;
                var worldDelta = PlayerViewScreenDeltaToWorld(screenDelta);
                playerViewManualOffset += worldDelta;
                lastPlayerViewMouseScreen = currentMouseScreen;
                return;
            }

            var currentMouseWorld = PlayerViewMouseWorldPosition();
            var delta = lastPlayerViewMouseWorld - currentMouseWorld;
            playerViewManualOffset += new Vector3(delta.x, delta.y, 0f);
            lastPlayerViewMouseWorld = PlayerViewMouseWorldPosition();
        }

        private Vector3 PlayerViewControlMousePosition()
        {
            return PlayerViewCameraControlActive ? MousePositionForDisplay(0) : PlayerViewMousePosition();
        }

        private Vector3 PlayerViewScreenDeltaToWorld(Vector3 screenDelta)
        {
            var displayWidth = Mathf.Max(1f, PrimaryMonitorWidth());
            var displayHeight = Mathf.Max(1f, PrimaryMonitorHeight());
            var verticalWorld = playerViewCamera.orthographicSize * 2f;
            var horizontalWorld = verticalWorld * playerViewCamera.aspect;
            return new Vector3(
                screenDelta.x / displayWidth * horizontalWorld,
                screenDelta.y / displayHeight * verticalWorld,
                0f);
        }

        private Vector3 PlayerViewMouseWorldPosition()
        {
            var mouse = PlayerViewMousePosition();
            mouse.z = Mathf.Abs(playerViewCamera.transform.position.z);
            var world = playerViewCamera.ScreenToWorldPoint(mouse);
            world.z = 0f;
            return world;
        }

        private bool MouseOnPlayerViewDisplay()
        {
            return MouseOnDisplay(playerViewCamera.targetDisplay);
        }

        private static bool MouseOnDisplay(int displayIndex)
        {
            return MouseDisplayIndex() == displayIndex;
        }

        private static int MouseDisplayIndex()
        {
            var mouse = MousePosition();
            var relative = Display.RelativeMouseAt(mouse);

            if (relative != Vector3.zero || Mathf.RoundToInt(relative.z) > 0)
            {
                return Mathf.RoundToInt(relative.z);
            }

            if (TryNativeMouseDisplayIndex(out var nativeDisplayIndex))
            {
                return nativeDisplayIndex;
            }

            if (Display.displays.Length > 1 && mouse.x >= Display.displays[0].systemWidth)
            {
                return 1;
            }

            return 0;
        }

        private static Vector3 PlayerViewMousePosition()
        {
            return MousePositionForDisplay(1);
        }

        private static Vector3 MousePositionForDisplay(int displayIndex)
        {
            var mouse = MousePosition();
            var relative = Display.RelativeMouseAt(mouse);

            if (relative != Vector3.zero || Mathf.RoundToInt(relative.z) > 0)
            {
                return new Vector3(relative.x, relative.y, 0f);
            }

            if (TryNativeMousePositionForDisplay(displayIndex, out var nativeMouse))
            {
                return nativeMouse;
            }

            if (displayIndex == 1 && Display.displays.Length > 1 && mouse.x >= Display.displays[0].systemWidth)
            {
                return new Vector3(mouse.x - Display.displays[0].systemWidth, mouse.y, 0f);
            }

            return mouse;
        }

        private static bool TryNativeMouseDisplayIndex(out int displayIndex)
        {
            displayIndex = 0;

            if (!GetCursorPos(out var point))
            {
                return false;
            }

            var firstWidth = PrimaryMonitorWidth();

            if (point.x >= firstWidth)
            {
                displayIndex = 1;
                return true;
            }

            if (point.x >= 0 && point.x < firstWidth)
            {
                displayIndex = 0;
                return true;
            }

            return false;
        }

        private static bool TryNativeMousePositionForDisplay(int displayIndex, out Vector3 position)
        {
            position = Vector3.zero;

            if (!GetCursorPos(out var point))
            {
                return false;
            }

            if (displayIndex == 1)
            {
                var x = point.x - PrimaryMonitorWidth();
                var y = PrimaryMonitorHeight() - point.y;
                position = new Vector3(x, y, 0f);
                return x >= 0f;
            }

            position = new Vector3(point.x, PrimaryMonitorHeight() - point.y, 0f);
            return displayIndex == 0;
        }

        private static int PrimaryMonitorWidth()
        {
            var width = GetSystemMetrics(0);
            return width > 0 ? width : Display.displays[0].systemWidth;
        }

        private static int PrimaryMonitorHeight()
        {
            var height = GetSystemMetrics(1);
            return height > 0 ? height : Display.displays[0].systemHeight;
        }

        private static bool PlayerViewDragStarted()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame);
#else
            return UnityEngine.Input.GetMouseButtonDown(0) ||
                   UnityEngine.Input.GetMouseButtonDown(1) ||
                   UnityEngine.Input.GetMouseButtonDown(2);
#endif
        }

        private static bool PlayerViewDragEnded()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ||
                   Mouse.current.leftButton.wasReleasedThisFrame ||
                   Mouse.current.middleButton.wasReleasedThisFrame ||
                   Mouse.current.rightButton.wasReleasedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonUp(0) ||
                   UnityEngine.Input.GetMouseButtonUp(1) ||
                   UnityEngine.Input.GetMouseButtonUp(2);
#endif
        }

        private void RefreshPlayerViewRoster(string activeMapId)
        {
            if (playerViewRosterRoot == null)
            {
                return;
            }

            ClearChildren(playerViewRosterRoot);

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                var isSelected = player.id == selectedPlayerId;
                var isOnActiveMap = !string.IsNullOrWhiteSpace(activeMapId) && player.currentMapId == activeMapId;
                var color = isOnActiveMap ? new Color(0.17f, 0.12f, 0.075f, 0.98f) : new Color(0.07f, 0.07f, 0.07f, 0.78f);

                if (isSelected)
                {
                    color = new Color(0.25f, 0.16f, 0.075f, 1f);
                }

                var card = CreateButton(player.name, playerViewRosterRoot, color);
                card.GetComponent<LayoutElement>().preferredHeight = 54f;
                card.GetComponent<Button>().interactable = false;
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("GM Interface", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var leftPanel = CreatePanel("Token Bank", canvasObject.transform, new Color(0.045f, 0.041f, 0.037f, 0.94f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.sizeDelta = new Vector2(188f, 0f);
            leftRect.anchoredPosition = Vector2.zero;

            CreateLabel("Фишки", leftPanel.transform, 20, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -42f), new Vector2(-12f, -10f));
            var tokenList = CreateVerticalList("Token List", leftPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, -54f), 6f);
            BuildTokenBank(tokenList);

            var topPanel = CreatePanel("Players Panel", canvasObject.transform, new Color(0.045f, 0.041f, 0.037f, 0.86f));
            var topRect = topPanel.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.offsetMin = new Vector2(198f, -72f);
            topRect.offsetMax = new Vector2(-12f, -8f);
            playerPanelRoot = CreateHorizontalList("Player Icons", topPanel.transform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f), 6f);

            var mapPanel = CreatePanel("Campaign Maps Panel", canvasObject.transform, new Color(0.045f, 0.041f, 0.037f, 0.90f));
            var mapRect = mapPanel.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0f, 0f);
            mapRect.anchorMax = new Vector2(0f, 0f);
            mapRect.pivot = new Vector2(0f, 0f);
            mapRect.anchoredPosition = new Vector2(198f, 14f);
            mapRect.sizeDelta = new Vector2(360f, 96f);
            mapPanelRoot = CreateHorizontalList("Map Cards", mapPanel.transform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f), 6f);

            playerViewControlButton = CreateButton("Камера игроков", canvasObject.transform, new Color(0.10f, 0.08f, 0.06f, 0.94f));
            var playerViewControlRect = playerViewControlButton.GetComponent<RectTransform>();
            playerViewControlRect.anchorMin = new Vector2(1f, 0f);
            playerViewControlRect.anchorMax = new Vector2(1f, 0f);
            playerViewControlRect.pivot = new Vector2(1f, 0f);
            playerViewControlRect.anchoredPosition = new Vector2(-14f, 14f);
            playerViewControlRect.sizeDelta = new Vector2(178f, 46f);
            playerViewControlButton.GetComponent<Button>().onClick.AddListener(TogglePlayerViewCameraControl);
            UpdatePlayerViewControlButton();

            BuildPrompt(canvasObject.transform);
        }

        private void TogglePlayerViewCameraControl()
        {
            PlayerViewCameraControlActive = !PlayerViewCameraControlActive;
            playerViewDragging = false;
            UpdatePlayerViewControlButton();
        }

        private void UpdatePlayerViewControlButton()
        {
            if (playerViewControlButton == null)
            {
                return;
            }

            playerViewControlButton.GetComponent<Image>().color = PlayerViewCameraControlActive
                ? new Color(0.24f, 0.14f, 0.045f, 1f)
                : new Color(0.10f, 0.08f, 0.06f, 0.94f);
        }

        private void BuildTokenBank(RectTransform tokenList)
        {
            foreach (var tokenPath in UserTokenStore.GetTokenPaths())
            {
                var path = tokenPath;
                var buttonObject = CreateButton(UserTokenStore.GetDisplayName(path), tokenList, new Color(0.12f, 0.09f, 0.065f, 0.98f));
                buttonObject.GetComponent<LayoutElement>().preferredHeight = 46f;
                buttonObject.GetComponent<Button>().onClick.AddListener(() => selectedBankTokenPath = path);
            }
        }

        private void RefreshPlayerPanel()
        {
            if (playerPanelRoot == null)
            {
                return;
            }

            ClearChildren(playerPanelRoot);

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                var activeHere = player.currentMapId == currentMapNode?.id;
                var selected = player.id == selectedPlayerId;
                var color = activeHere ? new Color(0.13f, 0.095f, 0.065f, 0.98f) : new Color(0.08f, 0.08f, 0.08f, 0.72f);

                if (selected)
                {
                    color = new Color(0.24f, 0.15f, 0.065f, 1f);
                }

                var capturedPlayer = player;
                var card = CreateButton(player.name, playerPanelRoot, color);
                card.GetComponent<LayoutElement>().preferredWidth = 120f;
                card.GetComponent<Button>().onClick.AddListener(() => SelectPlayer(capturedPlayer));
            }
        }

        private void RefreshMapPanel()
        {
            if (mapPanelRoot == null || campaign?.maps == null)
            {
                return;
            }

            ClearChildren(mapPanelRoot);

            foreach (var node in campaign.maps)
            {
                var map = GetMap(node.mapPath);
                var title = map == null ? "Карта" : map.name;
                var buttonObject = CreateButton(title, mapPanelRoot, node.id == currentMapNode?.id ? new Color(0.16f, 0.11f, 0.065f, 1f) : new Color(0.09f, 0.075f, 0.06f, 0.94f));
                buttonObject.GetComponent<LayoutElement>().preferredWidth = 104f;
                var id = node.id;
                buttonObject.GetComponent<Button>().onClick.AddListener(() => SwitchMap(id));
            }
        }

        private void BuildPrompt(Transform parent)
        {
            promptPanel = CreatePanel("Transition Prompt", parent, new Color(0.045f, 0.041f, 0.037f, 0.97f));
            var rect = promptPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420f, 150f);

            promptText = CreateLabel("Перейти на другую карту?", promptPanel.transform, 20, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -70f), new Vector2(-18f, -18f));

            var yes = CreateButton("Да", promptPanel.transform, new Color(0.16f, 0.11f, 0.06f, 1f));
            var yesRect = yes.GetComponent<RectTransform>();
            yesRect.anchorMin = new Vector2(0.5f, 0f);
            yesRect.anchorMax = new Vector2(0.5f, 0f);
            yesRect.anchoredPosition = new Vector2(-70f, 28f);
            yesRect.sizeDelta = new Vector2(110f, 42f);
            yes.GetComponent<Button>().onClick.AddListener(ConfirmTransition);

            var no = CreateButton("Нет", promptPanel.transform, new Color(0.11f, 0.10f, 0.09f, 1f));
            var noRect = no.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.5f, 0f);
            noRect.anchorMax = new Vector2(0.5f, 0f);
            noRect.anchoredPosition = new Vector2(70f, 28f);
            noRect.sizeDelta = new Vector2(110f, 42f);
            no.GetComponent<Button>().onClick.AddListener(HideTransitionPrompt);
            promptPanel.SetActive(false);
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreateButton(string label, Transform parent, Color color)
        {
            var buttonObject = CreatePanel(label, parent, color);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            buttonObject.AddComponent<LayoutElement>();
            CreateLabel(label, buttonObject.transform, 16, FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return buttonObject;
        }

        private static Text CreateLabel(string label, Transform parent, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateVerticalList(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float spacing)
        {
            var rect = CreateListRoot(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static RectTransform CreateHorizontalList(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float spacing)
        {
            var rect = CreateListRoot(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            return rect;
        }

        private static RectTransform CreateListRoot(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var listObject = new GameObject(name, typeof(RectTransform));
            listObject.transform.SetParent(parent, false);
            var rect = listObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool PrimaryMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        private static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector3.zero : Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return 0f;
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
#else
            return UnityEngine.Input.mouseScrollDelta.y;
#endif
        }

        private static void EnsureEventSystem()
        {
            var eventSystemObject = EventSystem.current == null ? null : EventSystem.current.gameObject;

            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            foreach (var legacyModule in eventSystemObject.GetComponents<StandaloneInputModule>())
            {
                Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }

    public sealed class PlayerViewTokenMover : MonoBehaviour
    {
        private const float MoveSpeed = 8f;
        private const float StopDistance = 0.01f;

        private Vector3 targetPosition;
        private bool moving;

        public void Initialize(Vector3 target)
        {
            targetPosition = target;
            moving = true;
        }

        private void Update()
        {
            if (!moving)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-MoveSpeed * Time.deltaTime));

            if ((transform.position - targetPosition).sqrMagnitude > StopDistance * StopDistance)
            {
                return;
            }

            transform.position = targetPosition;
            moving = false;
            Destroy(this);
        }
    }
}
