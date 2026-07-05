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

            if (context.Campaign?.links == null || context.CurrentMapNode == null)
            {
                return;
            }

            var data = mapLoader.GetMap(context.CurrentMapNode.mapPath);

            if (data?.exitPoints == null)
            {
                return;
            }

            var activeTransitionKeys = new HashSet<string>();

            if (context.TokenRoot == null)
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

                if (player == null)
                {
                    continue;
                }

                if (player.currentMapId != context.CurrentMapNode.id)
                {
                    continue;
                }

                var boardToken = runtimeToken.GetComponent<BoardToken>();
                var tokenCell = boardToken == null
                    ? context.Grid.WorldToCell(runtimeToken.transform.position)
                    : boardToken.gridPosition;
                player.gridX = tokenCell.x;
                player.gridY = tokenCell.y;

                foreach (var exit in data.exitPoints)
                {
                    var contains = Contains(exit, runtimeToken.transform.position);
                    
                    if (!contains)
                    {
                        continue;
                    }

                    var link = FindLinkFrom(exit.id);
                    var transitionKey = TransitionKey(player.id, context.CurrentMapNode.id, exit.id);
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

        private SavedCampaignLinkData FindLinkFrom(string exitId)
        {
            foreach (var link in context.Campaign.links)
            {
                if (link.fromMapId == context.CurrentMapNode.id && link.fromExitId == exitId)
                {
                    return link;
                }
            }

            foreach (var link in context.Campaign.links)
            {
                if (link.toMapId == context.CurrentMapNode.id && link.toExitId == exitId)
                {
                    return new SavedCampaignLinkData
                    {
                        fromMapId = context.CurrentMapNode.id,
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
