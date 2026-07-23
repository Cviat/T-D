using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace RPGTable.Runtime.Networking
{
    public static class GameNetworkRouter
    {
        public static bool RouteAction(
            string method, 
            string url, 
            string requestStr, 
            NetworkStream stream, 
            WebServerManager manager, 
            Action<Action> executeOnMainThreadBlocking)
        {
            // Handle player movement action
            if (method == "POST" && url == "/api/action/move")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.MovePayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool moved = false;
                        executeOnMainThreadBlocking(() =>
                        {
                            var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(payload.playerId);
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (player != null && loader != null && loader.PVManager != null)
                            {
                                if (player.currentMapId != loader.PVManager.PlayerViewMapId)
                                {
                                    // Movement is blocked on inactive maps!
                                    return;
                                }
                            }
                            moved = manager.TryMovePlayerTokenInternal(payload.playerId, payload.dirX, payload.dirY);
                        });

                        string responseJson = moved
                            ? "{\"status\":\"success\"}"
                            : "{\"status\":\"blocked\"}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(responseJson));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] move error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle transition sequence confirm/cancel
            if (method == "POST" && url == "/api/action/transition")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.TransitionPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        executeOnMainThreadBlocking(() => {
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (loader != null && loader.PendingTransitionPlayerId == payload.playerId)
                            {
                                if (payload.action == "confirm")
                                {
                                    loader.HandleConfirmTransition();
                                }
                                else if (payload.action == "cancel")
                                {
                                    loader.HandleCancelTransition();
                                }
                            }
                        });
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] transition error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle attack / request-attack
            if (method == "POST" && (url == "/api/action/request-attack" || url == "/api/action/attack"))
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.RequestAttackPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId) && !string.IsNullOrEmpty(payload.targetId))
                    {
                        bool success = false;
                        string failReason = "unknown";
                        executeOnMainThreadBlocking(() => {
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (loader != null)
                            {
                                var tokens = GameObject.FindObjectsByType<RPGTable.Runtime.CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                                var myToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => !t.IsPlayerViewClone && t.PlayerId == payload.playerId);
                                var targetToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => !t.IsPlayerViewClone && t.RuntimeId == payload.targetId);
                                
                                if (myToken != null && targetToken != null)
                                {
                                    if (targetToken.IsDead)
                                    {
                                        failReason = "target is dead";
                                    }
                                    else
                                    {
                                        loader.InitiateAttackSequence(myToken, targetToken);
                                        success = true;
                                    }
                                }
                                else
                                {
                                    failReason = "token not found";
                                }
                            }
                            else
                            {
                                failReason = "loader not found";
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : $"{{\"status\":\"failed\",\"reason\":\"{failReason}\"}}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                    else
                    {
                        HttpStaticServer.SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] attack error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle switch weapon
            if (method == "POST" && url == "/api/action/switch-weapon")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.SwitchWeaponPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        executeOnMainThreadBlocking(() => {
                            var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(payload.playerId);
                            if (player != null)
                            {
                                player.activeWeaponIndex = player.activeWeaponIndex == 0 ? 1 : 0;
                                manager.RecalculatePlayerRuntimeStatsInternal(player);
                                success = true;
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] switch-weapon error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle end turn
            if (method == "POST" && url == "/api/action/end-turn")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.EndTurnPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        executeOnMainThreadBlocking(() => {
                            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive 
                                && RPGTable.Runtime.CombatManager.Instance != null
                                && RPGTable.Runtime.CombatManager.Instance.ActiveToken != null
                                && RPGTable.Runtime.CombatManager.Instance.ActiveToken.PlayerId == payload.playerId)
                            {
                                RPGTable.Runtime.CombatManager.Instance.EndTokenTurn();
                                success = true;
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] end-turn error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle roll submit
            if (method == "POST" && url == "/api/roll/submit")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.SubmitRollPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        executeOnMainThreadBlocking(() => {
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (loader != null)
                            {
                                success = loader.SubmitRoll(payload.playerId, payload.rollResult);
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] roll/submit error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle roll reroll
            if (method == "POST" && url == "/api/roll/reroll")
            {
                try
                {
                    var payload = JsonUtility.FromJson<WebServerManager.RerollPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        int newRoll = 0;
                        executeOnMainThreadBlocking(() => {
                            var player = CampaignGameSession.FindPlayer(payload.playerId);
                            if (player != null && player.rerollCoins > 0)
                            {
                                player.rerollCoins--;
                                newRoll = UnityEngine.Random.Range(1, 7);
                                CampaignGameSession.UpdateTokenCombatStats(
                                    player.id, player.currentMapId,
                                    player.currentHp, player.maxHp,
                                    player.currentArmor, player.maxArmor,
                                    player.currentMovementPoints, player.maxMovementPoints,
                                    player.currentRolls, player.maxRolls,
                                    player.activeWeaponIndex, player.rerollCoins,
                                    player.statusEffects, player.isDead);
                            }
                        });
                        string resp = newRoll > 0 
                            ? $"{{\"status\":\"success\",\"rollResult\":{newRoll}}}"
                            : "{\"status\":\"failed\",\"reason\":\"No coins left\"}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] roll/reroll error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            // Handle play card
            if (method == "POST" && url == "/api/action/play-card")
            {
                try
                {
                    var payload = JsonUtility.FromJson<PlayCardPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        string error = "";
                        executeOnMainThreadBlocking(() => {
                            if (RPGTable.Runtime.ActionCards.ActionCardManager.Instance != null)
                            {
                                success = RPGTable.Runtime.ActionCards.ActionCardManager.Instance.PlayCard(
                                    payload.playerId, 
                                    payload.cardId, 
                                    new Vector2Int(payload.targetX, payload.targetY), 
                                    out error);
                            }
                            else
                            {
                                error = "ActionCardManager not active in scene";
                            }
                        });

                        string resp = success 
                            ? "{\"status\":\"success\"}" 
                            : $"{{\"status\":\"failed\",\"reason\":\"{error}\"}}";
                        HttpStaticServer.SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameNetworkRouter] play-card error: {ex}");
                    HttpStaticServer.SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return true;
            }

            return false; // Not handled by router
        }

        [Serializable]
        public class PlayCardPayload
        {
            public string playerId;
            public string cardId;
            public int targetX;
            public int targetY;
        }
    }
}
