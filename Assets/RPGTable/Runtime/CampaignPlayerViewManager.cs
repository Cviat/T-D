using System.Collections.Generic;
using System.Runtime.InteropServices;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.MapEditor;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.Runtime
{
    /// <summary>
    /// Manages the Player View: second-display camera, duplicated map world on layer 31,
    /// token clones with smooth movement, roster panel, and camera pan/zoom controls.
    /// </summary>
    internal sealed class CampaignPlayerViewManager
    {
        private const float CellSize = 1f;
        private const int PlayerViewLayer = 31;
        private const int PlayerViewLayerMask = 1 << PlayerViewLayer;
        private const float PlayerViewOrthographicSize = 8.5f;
        private const float PlayerViewCameraSmoothTime = 0.55f;
        private const float PlayerViewMinZoom = 4f;
        private const float PlayerViewMaxZoom = 18f;

        private readonly CampaignGameContext context;
        private readonly CampaignMapLoader mapLoader;
        private readonly CampaignTokenSpawner tokenSpawner;
        private readonly Dictionary<string, Vector3> playerViewTokenPositions =
            new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Transform> playerViewTokenTransforms =
            new Dictionary<string, Transform>();

        private Camera playerViewCamera;
        private Transform playerViewRoot;
        private RectTransform playerViewRosterRoot;
        private Vector3 playerViewGridOrigin;
        private Vector3 playerViewFollowTarget;
        private Vector3 playerViewManualOffset;
        private Vector3 playerViewCameraVelocity;
        private Vector3 lastPlayerViewMouseWorld;
        private Vector3 lastPlayerViewMouseScreen;
        private float playerViewTargetZoom = PlayerViewOrthographicSize;
        private bool playerViewCameraHasTarget;
        private bool playerViewDragging;
        private string playerViewStateKey;
        private string playerViewMapId;

        public static bool PlayerViewCameraControlActive { get; private set; }
        public string PlayerViewMapId => playerViewMapId;
        private GameObject playerViewInterface;
        public GameObject PlayerViewInterface => playerViewInterface;

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

        public static CampaignPlayerViewManager Instance { get; private set; }

        internal CampaignPlayerViewManager(
            CampaignGameContext context,
            CampaignMapLoader mapLoader,
            CampaignTokenSpawner tokenSpawner)
        {
            Instance = this;
            this.context = context;
            this.mapLoader = mapLoader;
            this.tokenSpawner = tokenSpawner;
        }

        public int PlayerViewLayerMaskValue => PlayerViewLayerMask;

        // в”Ђв”Ђ Setup в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void BuildPlayerView()
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
            playerViewInterface = canvasObject;
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = playerViewCamera.targetDisplay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var rosterPanel = CampaignGameUI.CreatePanel("Player View Roster", canvasObject.transform, Color.clear);
            var rosterRect = rosterPanel.GetComponent<RectTransform>();
            rosterRect.anchorMin = new Vector2(0f, 0f);
            rosterRect.anchorMax = new Vector2(0f, 1f);
            rosterRect.pivot = new Vector2(0f, 0.5f);
            rosterRect.sizeDelta = new Vector2(260f, 0f);
            rosterRect.anchoredPosition = Vector2.zero;

            playerViewRosterRoot = CampaignGameUI.CreateVerticalList(
                "Player View Icons",
                rosterPanel.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(10f, 10f),
                new Vector2(-10f, -10f),
                8f);
        }

        // в”Ђв”Ђ Toggle / Control в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void TogglePlayerViewCameraControl()
        {
            PlayerViewCameraControlActive = !PlayerViewCameraControlActive;
            playerViewDragging = false;
        }

        public void ResetPlayerViewOffset()
        {
            playerViewManualOffset = Vector3.zero;
            playerViewCameraVelocity = Vector3.zero;
        }

        // в”Ђв”Ђ Player selection helpers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void SelectDefaultPlayer()
        {
            if (!string.IsNullOrWhiteSpace(context.SelectedPlayerId)
                && CampaignGameSession.FindPlayer(context.SelectedPlayerId) != null)
            {
                return;
            }

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (!string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    context.SelectedPlayerId = player.id;
                    return;
                }
            }

            context.SelectedPlayerId = CampaignGameSession.CurrentPlayers.Count > 0
                ? CampaignGameSession.CurrentPlayers[0].id
                : null;
        }

        // в”Ђв”Ђ Per-frame updates в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void RefreshPlayerViewIfNeeded()
        {
            var stateKey = BuildPlayerViewStateKey();

            if (stateKey == playerViewStateKey)
            {
                return;
            }

            var selectedPlayer = CampaignGameSession.FindPlayer(context.SelectedPlayerId);

            if (selectedPlayer != null && TrySyncPlayerViewInPlace(selectedPlayer.currentMapId))
            {
                playerViewStateKey = stateKey;
                return;
            }

            RefreshPlayerView(true);
        }

        public void HandlePlayerViewCameraPan()
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

        public void HandlePlayerViewCameraZoom()
        {
            if (!PlayerViewCameraControlActive || playerViewCamera == null)
            {
                return;
            }

            var scroll = CampaignGameUI.ScrollDelta();

            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            playerViewTargetZoom = Mathf.Clamp(playerViewTargetZoom - scroll * 1.4f, PlayerViewMinZoom, PlayerViewMaxZoom);
        }

        public void UpdatePlayerViewCamera()
        {
            if (playerViewCamera == null || !playerViewCameraHasTarget)
            {
                return;
            }

            UpdateCombatCameraTarget();

            var desired = CampaignGameSession.IsCombatActive
                ? playerViewFollowTarget
                : playerViewFollowTarget + playerViewManualOffset;
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
        private void UpdateCombatCameraTarget()
        {
            if (!CampaignGameSession.IsCombatActive)
            {
                return;
            }

            var activeToken = CombatManager.Instance.ActiveToken;
            if (activeToken == null || activeToken.IsDead)
            {
                return;
            }

            var playerViewToken = GetPlayerViewTokenTransform(activeToken);
            if (playerViewToken == null)
            {
                return;
            }

            playerViewFollowTarget = new Vector3(playerViewToken.position.x, playerViewToken.position.y, 0f);
        }


        // в”Ђв”Ђ Full refresh в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void RefreshPlayerView(bool force)
        {
            if (playerViewCamera == null)
            {
                return;
            }

            SelectDefaultPlayer();
            var selectedPlayer = CampaignGameSession.FindPlayer(context.SelectedPlayerId);

            if (selectedPlayer == null || string.IsNullOrWhiteSpace(selectedPlayer.currentMapId))
            {
                DestroyPlayerViewRoot();
                RefreshPlayerViewRoster(null);
                playerViewStateKey = BuildPlayerViewStateKey();
                return;
            }

            if (!context.MapNodes.TryGetValue(selectedPlayer.currentMapId, out var selectedNode))
            {
                DestroyPlayerViewRoot();
                RefreshPlayerViewRoster(null);
                playerViewStateKey = BuildPlayerViewStateKey();
                return;
            }

            var data = mapLoader.GetMap(selectedNode.mapPath);

            if (data == null)
            {
                DestroyPlayerViewRoot();
                RefreshPlayerViewRoster(null);
                playerViewStateKey = BuildPlayerViewStateKey();
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

        // в”Ђв”Ђ Player View world building в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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
            mapBounds = CampaignMapLoader.ExpandBoundsForGrid(mapBounds, data);

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

        // в”Ђв”Ђ Token spawning for PV в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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
                var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(player.name) ? "Player Token" : player.name);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(new Vector2Int(player.gridX, player.gridY), footprint);
                PlacePlayerViewToken(tokenObject.transform, PlayerViewTokenKey("player", mapId, player.id), targetPosition, activeTokenKeys);

                tokenSpawner.CreateTokenVisual(tokenObject.transform, tokenData, footprint, TokenTeam.Player, tokenSpawner.AllocateSortingOrder());
                var runtimeToken = ConfigurePlayerViewRuntimeToken(
                    tokenObject,
                    player.name,
                    player.characterPath,
                    player.tokenPath,
                    TokenTeam.Player,
                    true,
                    player.id,
                    player.id,
                    new Vector2Int(player.gridX, player.gridY),
                    footprint,
                    FindRuntimeTokenByPlayerId(player.id),
                    player.isDead,
                    player.currentHp);

                if (player.isDead)
                {
                    tokenSpawner.ApplyDeadVisual(runtimeToken, footprint);
                }
            }

            if (mapId == context.CurrentMapNode?.id && context.TokenRoot != null)
            {
                SpawnPlayerViewRuntimeTokens(mapId, root, activeTokenKeys);
                return activeTokenKeys;
            }

            if (!mapTokenStatesContains(mapId, out var states))
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
                var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(state.displayName) ? "Visible Token" : state.displayName);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(state.gridPosition, footprint);
                PlacePlayerViewToken(tokenObject.transform,
                    PlayerViewTokenKey("stored", mapId, string.IsNullOrWhiteSpace(state.runtimeId) ? i.ToString() : state.runtimeId),
                    targetPosition, activeTokenKeys);

                tokenSpawner.CreateTokenVisual(tokenObject.transform, tokenData, footprint, state.team, tokenSpawner.AllocateSortingOrder());
                var runtimeToken = ConfigurePlayerViewRuntimeToken(
                    tokenObject,
                    state.displayName,
                    state.characterPath,
                    state.tokenPath,
                    state.team,
                    state.visibleToPlayers,
                    null,
                    state.runtimeId,
                    state.gridPosition,
                    footprint,
                    null,
                    state.isDead,
                    state.currentHp);

                if (state.isDead)
                {
                    tokenSpawner.ApplyDeadVisual(runtimeToken, footprint);
                }
            }

            return activeTokenKeys;
        }

        private void SpawnPlayerViewRuntimeTokens(string mapId, Transform root, HashSet<string> activeTokenKeys)
        {
            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
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
                var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                var tokenObject = new GameObject(string.IsNullOrWhiteSpace(runtimeToken.DisplayName) ? "Visible Token" : runtimeToken.DisplayName);
                tokenObject.transform.SetParent(root, false);
                var targetPosition = PlayerViewTokenWorldPosition(boardToken.gridPosition, footprint);
                PlacePlayerViewToken(tokenObject.transform,
                    PlayerViewTokenKey("runtime", mapId, CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken)),
                    targetPosition, activeTokenKeys);

                tokenSpawner.CreateTokenVisual(tokenObject.transform, tokenData, footprint, runtimeToken.Team, tokenSpawner.AllocateSortingOrder());
                var playerViewRuntimeToken = ConfigurePlayerViewRuntimeToken(
                    tokenObject,
                    runtimeToken.DisplayName,
                    runtimeToken.CharacterPath,
                    runtimeToken.TokenPath,
                    runtimeToken.Team,
                    runtimeToken.VisibleToPlayers,
                    runtimeToken.PlayerId,
                    CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken),
                    boardToken.gridPosition,
                    footprint,
                    runtimeToken,
                    runtimeToken.IsDead,
                    runtimeToken.CurrentHp);

                if (runtimeToken.IsDead)
                {
                    tokenSpawner.ApplyDeadVisual(playerViewRuntimeToken, footprint);
                }
            }
        }

        // в”Ђв”Ђ In-place sync (avoids full rebuild) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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
                var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                var targetPosition = PlayerViewTokenWorldPosition(new Vector2Int(player.gridX, player.gridY), footprint);
                var key = PlayerViewTokenKey("player", mapId, player.id);

                if (!playerViewTokenTransforms.TryGetValue(key, out var tokenTransform) || tokenTransform == null)
                {
                    return false;
                }

                var isGraveInPV = tokenTransform.Find("Dead Token") != null;
                if (player.isDead != isGraveInPV)
                {
                    return false;
                }

                if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                {
                    return false;
                }

                SyncPlayerViewRuntimeToken(
                    tokenTransform,
                    player.name,
                    player.characterPath,
                    player.tokenPath,
                    TokenTeam.Player,
                    true,
                    player.id,
                    player.id,
                    new Vector2Int(player.gridX, player.gridY),
                    footprint,
                    FindRuntimeTokenByPlayerId(player.id),
                    player.isDead,
                    player.currentHp);
            }

            if (mapId == context.CurrentMapNode?.id && context.TokenRoot != null)
            {
                if (!SyncRuntimePlayerViewTokens(mapId, activeTokenKeys))
                {
                    return false;
                }
            }
            else if (mapTokenStatesContains(mapId, out var states))
            {
                for (var i = 0; i < states.Count; i++)
                {
                    var state = states[i];

                    if (!state.visibleToPlayers || string.IsNullOrWhiteSpace(state.tokenPath))
                    {
                        continue;
                    }

                    var tokenData = UserTokenStore.LoadToken(state.tokenPath);
                    var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                    var targetPosition = PlayerViewTokenWorldPosition(state.gridPosition, footprint);
                    var key = PlayerViewTokenKey("stored", mapId, string.IsNullOrWhiteSpace(state.runtimeId) ? i.ToString() : state.runtimeId);

                    if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                    {
                        return false;
                    }

                    SyncPlayerViewRuntimeToken(
                        playerViewTokenTransforms[key],
                        state.displayName,
                        state.characterPath,
                        state.tokenPath,
                        state.team,
                        state.visibleToPlayers,
                        null,
                        state.runtimeId,
                        state.gridPosition,
                        footprint,
                        null,
                        state.isDead,
                        state.currentHp);
                }
            }

            foreach (var key in playerViewTokenTransforms.Keys)
            {
                if (!activeTokenKeys.Contains(key))
                {
                    return false;
                }
            }

            SetPlayerViewCameraTarget(CampaignGameSession.FindPlayer(context.SelectedPlayerId), new Bounds(playerViewFollowTarget, Vector3.zero));
            RefreshPlayerViewRoster(mapId);
            return true;
        }

        private bool SyncRuntimePlayerViewTokens(string mapId, HashSet<string> activeTokenKeys)
        {
            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
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

                var key = PlayerViewTokenKey("runtime", mapId, CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken));

                if (!playerViewTokenTransforms.TryGetValue(key, out var tokenTransform) || tokenTransform == null)
                {
                    return false;
                }

                var isGraveInPV = tokenTransform.Find("Dead Token") != null;
                if (runtimeToken.IsDead != isGraveInPV)
                {
                    return false;
                }

                var tokenData = UserTokenStore.LoadToken(runtimeToken.TokenPath);
                var footprint = CampaignTokenSpawner.GetFootprint(tokenData);
                var targetPosition = PlayerViewTokenWorldPosition(boardToken.gridPosition, footprint);

                if (!MoveExistingPlayerViewToken(key, targetPosition, activeTokenKeys))
                {
                    return false;
                }

                SyncPlayerViewRuntimeToken(
                    tokenTransform,
                    runtimeToken.DisplayName,
                    runtimeToken.CharacterPath,
                    runtimeToken.TokenPath,
                    runtimeToken.Team,
                    runtimeToken.VisibleToPlayers,
                    runtimeToken.PlayerId,
                    CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken),
                    boardToken.gridPosition,
                    footprint,
                    runtimeToken,
                    runtimeToken.IsDead,
                    runtimeToken.CurrentHp);
            }

            return true;
        }

        private bool MoveExistingPlayerViewToken(string key, Vector3 targetPosition, HashSet<string> activeTokenKeys)
        {
            activeTokenKeys.Add(key);
            return playerViewTokenTransforms.ContainsKey(key) && playerViewTokenTransforms[key] != null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void PlacePlayerViewToken(Transform tokenTransform, string key, Vector3 targetPosition, HashSet<string> activeTokenKeys)
        {
            activeTokenKeys.Add(key);
            tokenTransform.position = targetPosition;
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

        public Vector3 GetPlayerViewTokenWorldPosition(Vector2Int cell, int footprint)
        {
            return PlayerViewTokenWorldPosition(cell, footprint);
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

        private CampaignRuntimeToken ConfigurePlayerViewRuntimeToken(
            GameObject tokenObject,
            string displayName,
            string characterPath,
            string tokenPath,
            TokenTeam team,
            bool visibleToPlayers,
            string playerId,
            string runtimeId,
            Vector2Int cell,
            int footprint,
            CampaignRuntimeToken source,
            bool isDead,
            int fallbackCurrentHp)
        {
            RemovePlayerViewCombatComponents(tokenObject);

            var runtimeToken = tokenObject.GetComponent<CampaignRuntimeToken>();
            if (runtimeToken == null)
            {
                runtimeToken = tokenObject.AddComponent<CampaignRuntimeToken>();
            }

            var charData = string.IsNullOrWhiteSpace(characterPath)
                ? null
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);

            runtimeToken.PlayerId = playerId;
            runtimeToken.RuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? playerId : runtimeId;
            runtimeToken.TokenPath = tokenPath;
            runtimeToken.CharacterPath = characterPath;
            runtimeToken.DisplayName = displayName;
            runtimeToken.Team = team;
            runtimeToken.VisibleToPlayers = visibleToPlayers;
            runtimeToken.IsPlayerViewClone = true;
            runtimeToken.IsDead = isDead;
            runtimeToken.FootprintSize = Mathf.Max(1, footprint);
            runtimeToken.MaxHp = source != null ? source.MaxHp : (charData != null ? charData.maxHp : 10);
            int fallbackHp = source != null ? source.CurrentHp : (fallbackCurrentHp <= 0 ? runtimeToken.MaxHp : fallbackCurrentHp);
            runtimeToken.MaxArmor = source != null ? source.MaxArmor : (charData != null ? charData.maxArmor : 0);
            int fallbackArmor = source != null ? source.CurrentArmor : runtimeToken.MaxArmor;
            runtimeToken.InitializeStats(runtimeToken.MaxHp, fallbackHp, runtimeToken.MaxArmor, fallbackArmor, isDead);
            runtimeToken.MaxMovementPoints = source != null ? source.MaxMovementPoints : runtimeToken.MaxMovementPoints;
            runtimeToken.CurrentMovementPoints = source != null ? source.CurrentMovementPoints : runtimeToken.CurrentMovementPoints;
            runtimeToken.MaxRolls = source != null ? source.MaxRolls : runtimeToken.MaxRolls;
            runtimeToken.CurrentRolls = source != null ? source.CurrentRolls : runtimeToken.CurrentRolls;
            runtimeToken.ActiveWeaponIndex = source != null ? source.ActiveWeaponIndex : runtimeToken.ActiveWeaponIndex;

            if (tokenObject.GetComponent<TokenHealthArmorBars>() == null)
            {
                tokenObject.AddComponent<TokenHealthArmorBars>();
            }

            return runtimeToken;
        }

        private static void RemovePlayerViewCombatComponents(GameObject tokenObject)
        {
            var boardToken = tokenObject.GetComponent<BoardToken>();
            if (boardToken != null)
            {
                Object.Destroy(boardToken);
            }

            var collider = tokenObject.GetComponent<Collider2D>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var rootRenderer = tokenObject.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                Object.Destroy(rootRenderer);
            }
        }

        private void SyncPlayerViewRuntimeToken(
            Transform tokenTransform,
            string displayName,
            string characterPath,
            string tokenPath,
            TokenTeam team,
            bool visibleToPlayers,
            string playerId,
            string runtimeId,
            Vector2Int cell,
            int footprint,
            CampaignRuntimeToken source,
            bool isDead,
            int fallbackCurrentHp)
        {
            if (tokenTransform == null)
            {
                return;
            }

            ConfigurePlayerViewRuntimeToken(
                tokenTransform.gameObject,
                displayName,
                characterPath,
                tokenPath,
                team,
                visibleToPlayers,
                playerId,
                runtimeId,
                cell,
                footprint,
                source,
                isDead,
                fallbackCurrentHp);
        }

        private CampaignRuntimeToken FindRuntimeTokenByPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || context.TokenRoot == null)
            {
                return null;
            }

            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                if (runtimeToken != null && runtimeToken.PlayerId == playerId)
                {
                    return runtimeToken;
                }
            }

            return null;
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
                center = PlayerViewTokenWorldPosition(new Vector2Int(selectedPlayer.gridX, selectedPlayer.gridY), CampaignTokenSpawner.GetFootprint(tokenData));
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

        private void DestroyPlayerViewRoot()
        {
            if (playerViewRoot == null)
            {
                return;
            }

            playerViewRoot.gameObject.SetActive(false);
            Object.Destroy(playerViewRoot.gameObject);
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

        private string BuildPlayerViewStateKey()
        {
            var key = context.SelectedPlayerId ?? string.Empty;

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                var runtimeToken = FindRuntimeTokenByPlayerId(player.id);
                key += $"|{player.id}:{player.currentMapId}:{player.gridX}:{player.gridY}:{player.tokenPath}:{player.isDead}:{player.currentHp}:{player.maxHp}:{runtimeToken?.CurrentHp}:{runtimeToken?.MaxHp}:{runtimeToken?.CurrentArmor}:{runtimeToken?.MaxArmor}";
            }

            if (context.CurrentMapNode != null && context.TokenRoot != null)
            {
                foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
                {
                    if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                    {
                        continue;
                    }

                    var boardToken = runtimeToken.GetComponent<BoardToken>();
                    key += $"|token:{CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken)}:{runtimeToken.TokenPath}:{runtimeToken.VisibleToPlayers}:{runtimeToken.IsDead}:{boardToken?.gridPosition.x}:{boardToken?.gridPosition.y}:{runtimeToken.CurrentHp}:{runtimeToken.MaxHp}:{runtimeToken.CurrentArmor}:{runtimeToken.MaxArmor}";
                }
            }

            return key;
        }

        private void RefreshPlayerViewRoster(string activeMapId)
        {
            if (playerViewRosterRoot == null)
            {
                return;
            }

            CampaignGameUI.ClearChildren(playerViewRosterRoot);

            // РџС‹С‚Р°РµРјСЃСЏ Р·Р°РіСЂСѓР·РёС‚СЊ СЃРїРµС†РёР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р± РґР»СЏ СЌРєСЂР°РЅР° РёРіСЂРѕРєР°.
            // Р•СЃР»Рё РµРіРѕ РЅРµС‚, РёСЃРїРѕР»СЊР·СѓРµРј РґРµС„РѕР»С‚РЅСѓСЋ TokenCard.
            var cardPrefab = Resources.Load<GameObject>("Prefabs/PlayerViewCard") 
                             ?? Resources.Load<GameObject>("Prefabs/TokenCard");

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                var isSelected = player.id == context.SelectedPlayerId;
                var isOnActiveMap = !string.IsNullOrWhiteSpace(activeMapId) && player.currentMapId == activeMapId;
                var color = isOnActiveMap
                    ? new Color(0.17f, 0.12f, 0.075f, 0.98f)
                    : new Color(0.07f, 0.07f, 0.07f, 0.78f);

                if (isSelected)
                {
                    color = new Color(0.25f, 0.16f, 0.075f, 1f);
                }

                if (cardPrefab != null)
                {
                    var cardGo = Object.Instantiate(cardPrefab, playerViewRosterRoot);
                    ConfigurePlayerViewRosterCard(cardGo);
                    var cardView = cardGo.GetComponent<TokenCardView>();
                    
                    if (cardView != null)
                    {
                        Sprite portraitSprite = null;
                        if (!string.IsNullOrWhiteSpace(player.portraitPath))
                        {
                            portraitSprite = UserTokenStore.LoadSprite(player.portraitPath);
                        }
                        else if (!string.IsNullOrWhiteSpace(player.tokenPath))
                        {
                            var tokenData = UserTokenStore.LoadToken(player.tokenPath);
                            if (tokenData != null)
                            {
                                portraitSprite = UserTokenStore.LoadSprite(tokenData.portraitPath);
                            }
                        }

                        cardView.Setup(player.name, portraitSprite, player.currentHp, player.maxHp, player.isDead, null);
                    }


                    var btn = cardGo.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
                else
                {
                    var card = CampaignGameUI.CreateButton(player.name, playerViewRosterRoot, color);
                    card.GetComponent<LayoutElement>().preferredHeight = 78f;
                    card.GetComponent<Button>().interactable = false;
                }
            }
        }

        private static void ConfigurePlayerViewRosterCard(GameObject cardGo)
        {
            if (cardGo == null)
            {
                return;
            }

            var layout = cardGo.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cardGo.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = 78f;
            layout.minHeight = 78f;
            layout.preferredWidth = 240f;
            layout.minWidth = 220f;
        }

        /// <summary>
        /// Stub for accessing stored token states from the spawner.
        /// The PV manager needs to read these for maps that are not the current map.
        /// We access them via reflection-free callback stored during construction.
        /// For simplicity we access the spawner's internal state through a delegate.
        /// </summary>
        private bool mapTokenStatesContains(string mapId, out List<CampaignGameSession.RuntimeMapTokenState> states)
        {
            // Access is done through the spawner's GetMapTokenStates method
            states = tokenSpawner.GetMapTokenStates(mapId);
            return states != null;
        }

        // в”Ђв”Ђ Display detection в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        internal static bool MouseOnDisplay(int displayIndex)
        {
            return MouseDisplayIndex() == displayIndex;
        }

        private bool MouseOnPlayerViewDisplay()
        {
            return MouseOnDisplay(playerViewCamera.targetDisplay);
        }

        private static int MouseDisplayIndex()
        {
            var mouse = CampaignGameUI.MousePosition();
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

        private static Vector3 PlayerViewMousePosition()
        {
            return MousePositionForDisplay(1);
        }

        private static Vector3 MousePositionForDisplay(int displayIndex)
        {
            var mouse = CampaignGameUI.MousePosition();
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

        public Transform GetPlayerViewTokenTransform(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null) return null;
            
            var keyRuntime = PlayerViewTokenKey("runtime", playerViewMapId, CampaignTokenSpawner.EnsureRuntimeTokenId(runtimeToken));
            if (playerViewTokenTransforms.TryGetValue(keyRuntime, out var tRuntime))
            {
                return tRuntime;
            }

            if (!string.IsNullOrEmpty(runtimeToken.PlayerId))
            {
                var keyPlayer = PlayerViewTokenKey("player", playerViewMapId, runtimeToken.PlayerId);
                if (playerViewTokenTransforms.TryGetValue(keyPlayer, out var tPlayer))
                {
                    return tPlayer;
                }
            }

            return null;
        }
    }
}
