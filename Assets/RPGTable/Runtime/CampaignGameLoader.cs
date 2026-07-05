using RPGTable.MapEditor;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Thin orchestrator MonoBehaviour. Delegates domain work to focused services:
    /// <see cref="CampaignMapLoader"/>, <see cref="CampaignTokenSpawner"/>,
    /// <see cref="CampaignPlayerViewManager"/>, <see cref="CampaignTransitionController"/>,
    /// <see cref="CampaignGameUI"/>.
    /// </summary>
    public sealed class CampaignGameLoader : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;

        private CampaignGameContext context;
        internal CampaignGameContext Context => context;

        private CampaignMapLoader mapLoader;
        private CampaignTokenSpawner spawner;
        private CampaignPlayerViewManager pvManager;
        private CampaignTransitionController transitionController;
        private CampaignGameUI ui;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            context = new CampaignGameContext { WorldCamera = worldCamera };
            mapLoader = new CampaignMapLoader();
            spawner = new CampaignTokenSpawner(context, this);
            ui = new CampaignGameUI();
            pvManager = new CampaignPlayerViewManager(context, mapLoader, spawner);
            transitionController = new CampaignTransitionController(context, ui, mapLoader, spawner);

            if (worldCamera != null)
            {
                worldCamera.cullingMask &= ~pvManager.PlayerViewLayerMaskValue;
            }

            ui.BuildUi(
                onPromptConfirm: HandleConfirmTransition,
                onPromptCancel: HandleCancelTransition,
                onTogglePVCamera: HandleTogglePlayerViewCamera,
                onBankTokenSelected: path => spawner.SelectedBankTokenPath = path);

            pvManager.BuildPlayerView();
            LoadSelectedCampaign();
        }

        private void Update()
        {
            spawner.HandleBankPlacement();
            transitionController.SyncRuntimePlayerPositions();
            transitionController.CheckPlayerTransitions();
            pvManager.RefreshPlayerViewIfNeeded();
            pvManager.HandlePlayerViewCameraPan();
            pvManager.HandlePlayerViewCameraZoom();
            pvManager.UpdatePlayerViewCamera();
        }

        // ── Campaign loading ─────────────────────────────────────────────

        private void LoadSelectedCampaign()
        {
            context.Campaign = UserCampaignStore.LoadCampaign(CampaignGameSession.SelectedCampaignPath);

            if (context.Campaign == null || context.Campaign.maps == null || context.Campaign.maps.Length == 0)
            {
                return;
            }

            context.MapNodes.Clear();

            foreach (var node in context.Campaign.maps)
            {
                if (!string.IsNullOrWhiteSpace(node.id))
                {
                    context.MapNodes[node.id] = node;
                }
            }

            context.CurrentMapNode = FindStartMap();
            pvManager.SelectDefaultPlayer();
            LoadMap(context.CurrentMapNode);
            RefreshMapPanel();
            RefreshPlayerPanel();
            pvManager.RefreshPlayerView(true);
        }

        private SavedCampaignMapNodeData FindStartMap()
        {
            if (!string.IsNullOrWhiteSpace(context.Campaign.startMapId)
                && context.MapNodes.TryGetValue(context.Campaign.startMapId, out var startNode))
            {
                return startNode;
            }

            return context.Campaign.maps[0];
        }

        // ── Map loading ──────────────────────────────────────────────────

        private void LoadMap(SavedCampaignMapNodeData node)
        {
            if (node == null)
            {
                return;
            }

            spawner.SaveCurrentMapTokenState();
            context.SelectedToken = null;
            context.CurrentMapNode = node;
            mapLoader.ClearWorld();

            var data = mapLoader.GetMap(node.mapPath);

            if (data == null)
            {
                return;
            }

            mapLoader.CreateMapRoots();
            context.MapRoot = mapLoader.MapRoot;
            context.TokenRoot = mapLoader.TokenRoot;

            var bounds = mapLoader.BuildMapElements(data);
            context.Grid = mapLoader.BuildGrid(bounds, data);
            mapLoader.BuildExitZones(data);
            spawner.SpawnPlayersForCurrentMap(data);
            spawner.SpawnStoredTokensForCurrentMap();
            CampaignMapLoader.FocusCamera(bounds, context.WorldCamera);
            RefreshMapPanel();
            RefreshPlayerPanel();
            pvManager.RefreshPlayerView(true);
        }

        private void SwitchMap(string mapId)
        {
            if (context.MapNodes.TryGetValue(mapId, out var node))
            {
                LoadMap(node);
            }
        }

        // ── Player selection ─────────────────────────────────────────────

        private void SelectPlayer(CampaignPlayerData player)
        {
            if (player == null)
            {
                return;
            }

            context.SelectedPlayerId = player.id;
            pvManager.ResetPlayerViewOffset();
            RefreshPlayerPanel();
            pvManager.RefreshPlayerView(true);
        }

        public string PendingTransitionPlayerId => transitionController.PendingPlayerId;
        public string PendingTransitionPrompt => transitionController.PendingPromptText;

        // ── Transition callbacks ─────────────────────────────────────────

        public void HandleConfirmTransition()
        {
            var targetNode = transitionController.ConfirmTransition();
            ui.HidePrompt();

            if (targetNode != null)
            {
                LoadMap(targetNode);
            }
        }

        public void HandleCancelTransition()
        {
            transitionController.CancelTransition();
            ui.HidePrompt();
        }

        // ── PV camera toggle ─────────────────────────────────────────────

        private void HandleTogglePlayerViewCamera()
        {
            pvManager.TogglePlayerViewCameraControl();
            ui.UpdatePlayerViewControlButton(CampaignPlayerViewManager.PlayerViewCameraControlActive);
        }

        // ── Public API for context-click menu ────────────────────────────

        public void SelectRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            context.SelectedToken = runtimeToken;
            ui.RefreshEntityInspector(runtimeToken);
            ui.RefreshActiveTokensPanel();
        }

        public void DeleteRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            if (context.SelectedToken == runtimeToken)
            {
                SelectRuntimeToken(null);
            }
            spawner.DeleteRuntimeToken(runtimeToken);
            ui.RefreshActiveTokensPanel();
        }

        public void KillRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            spawner.KillRuntimeToken(runtimeToken);
            ui.RefreshActiveTokensPanel();
            if (context.SelectedToken == runtimeToken)
            {
                ui.RefreshEntityInspector(runtimeToken);
            }
        }

        public void ReviveRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            spawner.ReviveRuntimeToken(runtimeToken);
            ui.RefreshActiveTokensPanel();
            if (context.SelectedToken == runtimeToken)
            {
                ui.RefreshEntityInspector(runtimeToken);
            }
        }

        // ── UI refresh helpers ───────────────────────────────────────────

        private void RefreshPlayerPanel()
        {
            ui.RefreshPlayerPanel(
                CampaignGameSession.CurrentPlayers,
                context.CurrentMapNode?.id,
                context.SelectedPlayerId,
                SelectPlayer);
        }

        private void RefreshMapPanel()
        {
            ui.RefreshMapPanel(
                context.Campaign,
                context.CurrentMapNode?.id,
                mapLoader.GetMap,
                SwitchMap);
        }
    }
}
