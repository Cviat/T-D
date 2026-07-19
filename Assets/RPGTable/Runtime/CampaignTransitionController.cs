using System.Collections.Generic;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.MapEditor;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Detects when player tokens enter exit zones and manages the
    /// map-transition prompt flow (show / confirm / cancel).
    /// Also synchronises runtime player positions each frame.
    /// </summary>
    internal sealed class CampaignTransitionController
    {
        private readonly CampaignGameContext context;
        private readonly CampaignGameUI ui;
        private readonly CampaignMapLoader mapLoader;
        private readonly CampaignTokenSpawner spawner;
        private readonly HashSet<string> ignoredTransitionKeys = new HashSet<string>();

        private CampaignPlayerData pendingTransitionPlayer;
        private SavedCampaignLinkData pendingTransitionLink;

        public string PendingPlayerId => pendingTransitionPlayer?.id;
        public string PendingPromptText { get; private set; }

        internal CampaignTransitionController(
            CampaignGameContext context,
            CampaignGameUI ui,
            CampaignMapLoader mapLoader,
            CampaignTokenSpawner spawner)
        {
            this.context = context;
            this.ui = ui;
            this.mapLoader = mapLoader;
            this.spawner = spawner;
        }

        // ── Per-frame sync ───────────────────────────────────────────────

        public void SyncRuntimePlayerPositions()
        {
            if (context.CurrentMapNode == null || context.TokenRoot == null || context.Grid == null)
            {
                return;
            }

            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
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

                player.currentMapId = context.CurrentMapNode.id;
                player.gridX = boardToken.gridPosition.x;
                player.gridY = boardToken.gridPosition.y;
                player.isDead = runtimeToken.IsDead;
                player.currentHp = runtimeToken.CurrentHp;
                player.maxHp = runtimeToken.MaxHp;
            }
        }

        // ── Transition detection ─────────────────────────────────────────

        public void CheckPlayerTransitions()
        {
            if (ui.IsPromptVisible)
            {
                return;
            }

            if (context.Campaign?.links == null)
            {
                return;
            }

            var activeTransitionKeys = new HashSet<string>();

            if (CampaignGameSession.CurrentPlayers == null)
            {
                return;
            }

            // Find all tokens currently active in the scene (both GM main board tokens and Player View clone tokens)
#if UNITY_2023_1_OR_NEWER
            var allTokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsSortMode.None);
#else
            var allTokens = GameObject.FindObjectsOfType<CampaignRuntimeToken>();
#endif

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (player == null || player.isDead || string.IsNullOrWhiteSpace(player.currentMapId))
                {
                    continue;
                }

                if (!context.MapNodes.TryGetValue(player.currentMapId, out var mapNode))
                {
                    continue;
                }

                var data = mapLoader.GetMap(mapNode.mapPath);
                if (data?.exitPoints == null)
                {
                    continue;
                }

                // Find the token representing this player on their current map
                CampaignRuntimeToken activeToken = null;
                foreach (var t in allTokens)
                {
                    if (t != null && t.PlayerId == player.id)
                    {
                        // If player is on the GM's active map, sync their grid position and use their main token
                        if (context.CurrentMapNode != null && player.currentMapId == context.CurrentMapNode.id && !t.IsPlayerViewClone)
                        {
                            activeToken = t;
                            var boardToken = t.GetComponent<BoardToken>();
                            if (boardToken != null)
                            {
                                player.gridX = boardToken.gridPosition.x;
                                player.gridY = boardToken.gridPosition.y;
                            }
                            break;
                        }
                        // If player is on the Player View active map, use their clone token
                        var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                        if (loader != null && loader.PVManager != null && player.currentMapId == loader.PVManager.PlayerViewMapId && t.IsPlayerViewClone)
                        {
                            activeToken = t;
                        }
                    }
                }

                Vector3 positionToCheck;
                if (activeToken != null)
                {
                    positionToCheck = activeToken.transform.position;
                }
                else
                {
                    // Fallback to cell coordinates if no physical token is loaded/found in scene
                    if (context.Grid == null)
                    {
                        continue;
                    }
                    int footprint = 1;
                    if (!string.IsNullOrEmpty(player.tokenPath))
                    {
                        var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(player.tokenPath);
                        footprint = Mathf.Clamp(tokenData == null ? 1 : tokenData.footprintSize, 1, 5);
                    }
                    positionToCheck = context.Grid.CellToWorld(new Vector2Int(player.gridX, player.gridY)) +
                        new Vector3(
                            (footprint - 1) * context.Grid.cellSize * 0.5f,
                            (footprint - 1) * context.Grid.cellSize * 0.5f,
                            0f);
                }

                foreach (var exit in data.exitPoints)
                {
                    var contains = Contains(exit, positionToCheck);
                    if (!contains)
                    {
                        continue;
                    }

                    var link = FindLinkFrom(player.currentMapId, exit.id);
                    if (link != null)
                    {
                        var transitionKey = TransitionKey(player.id, player.currentMapId, exit.id);
                        activeTransitionKeys.Add(transitionKey);

                        if (!ignoredTransitionKeys.Contains(transitionKey))
                        {
                            ShowTransitionPrompt(player, link);
                            RemoveInactiveIgnoredTransitions(activeTransitionKeys);
                            return;
                        }
                    }
                }
            }

            RemoveInactiveIgnoredTransitions(activeTransitionKeys);
        }

        // ── Confirm / Cancel ─────────────────────────────────────────────

        /// <summary>
        /// Confirms the pending transition. Returns the target map node to load,
        /// or null if the transition data is invalid.
        /// </summary>
        public SavedCampaignMapNodeData ConfirmTransition()
        {
            if (pendingTransitionPlayer == null
                || pendingTransitionLink == null
                || !context.MapNodes.TryGetValue(pendingTransitionLink.toMapId, out var targetNode))
            {
                ClearPendingTransition();
                return null;
            }

            pendingTransitionPlayer.currentMapId = targetNode.id;
            spawner.SetPendingSpawnExit(pendingTransitionPlayer.id, pendingTransitionLink.toExitId);
            ignoredTransitionKeys.Add(TransitionKey(pendingTransitionPlayer.id, targetNode.id, pendingTransitionLink.toExitId));
            ClearPendingTransition();
            return targetNode;
        }

        public void CancelTransition()
        {
            if (pendingTransitionPlayer != null && pendingTransitionLink != null)
            {
                ignoredTransitionKeys.Add(TransitionKey(
                    pendingTransitionPlayer.id,
                    pendingTransitionLink.fromMapId,
                    pendingTransitionLink.fromExitId));
            }

            ClearPendingTransition();
        }

        // ── Private helpers ──────────────────────────────────────────────

        private void ShowTransitionPrompt(CampaignPlayerData player, SavedCampaignLinkData link)
        {
            pendingTransitionPlayer = player;
            pendingTransitionLink = link;

            var targetName = context.MapNodes.TryGetValue(link.toMapId, out var targetNode)
                ? mapLoader.GetMap(targetNode.mapPath)?.name
                : null;

            var text = string.IsNullOrWhiteSpace(targetName)
                ? $"Перейти на другую карту: {player.name}?"
                : $"Перейти на карту \"{targetName}\": {player.name}?";

            PendingPromptText = text;
            ui.ShowPrompt(text);
        }

        private void ClearPendingTransition()
        {
            pendingTransitionPlayer = null;
            pendingTransitionLink = null;
            PendingPromptText = null;
        }

        private SavedCampaignLinkData FindLinkFrom(string mapId, string exitId)
        {
            foreach (var link in context.Campaign.links)
            {
                if (link.fromMapId == mapId && link.fromExitId == exitId)
                {
                    return link;
                }
            }

            foreach (var link in context.Campaign.links)
            {
                if (link.toMapId == mapId && link.toExitId == exitId)
                {
                    return new SavedCampaignLinkData
                    {
                        fromMapId = mapId,
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
    }
}
