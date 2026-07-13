using System;
using System.Collections;
using RPGTable.MapEditor;
using RPGTable.Core;
using UnityEngine;
using RPGTable.Runtime.Networking;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        internal CampaignTokenSpawner Spawner => spawner;
        private CampaignPlayerViewManager pvManager;
        internal CampaignPlayerViewManager PVManager => pvManager;
        private CampaignTransitionController transitionController;
        private CampaignGameUI ui;
        internal CampaignGameUI UI => ui;

        // в”Ђв”Ђ Lifecycle в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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
            HandleCombatInputs();
        }

        // в”Ђв”Ђ Campaign loading в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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

        // в”Ђв”Ђ Map loading в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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

        // в”Ђв”Ђ Player selection в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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

        public void FocusPlayerViewOnPlayer(string playerId)
        {
            SelectPlayer(CampaignGameSession.FindPlayer(playerId));
        }

        public string PendingTransitionPlayerId => transitionController.PendingPlayerId;
        public string PendingTransitionPrompt => transitionController.PendingPromptText;

        // в”Ђв”Ђ Transition callbacks в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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

        // в”Ђв”Ђ PV camera toggle в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        private void HandleTogglePlayerViewCamera()
        {
            pvManager.TogglePlayerViewCameraControl();
            ui.UpdatePlayerViewControlButton(CampaignPlayerViewManager.PlayerViewCameraControlActive);
        }

        // в”Ђв”Ђ Public API for context-click menu в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

        public void SelectRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            context.SelectedToken = runtimeToken;
            ui.RefreshEntityInspector(runtimeToken);
            ui.RefreshActiveTokensPanel();

            if (RPGTable.Board.GridHighlighter.Instance != null)
            {
                RPGTable.Board.GridHighlighter.Instance.HighlightTokenRanges(runtimeToken);
            }
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

        // в”Ђв”Ђ UI refresh helpers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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

        private void HandleCombatInputs()
        {
            if (!CampaignGameSession.IsCombatActive) return;

            var activeToken = CombatManager.Instance.ActiveToken;
            if (activeToken == null || activeToken.IsDead) return;

            if (PrimaryMousePressed())
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                var mousePos = InputMousePosition();
                mousePos.z = Mathf.Abs(worldCamera.transform.position.z);
                var worldPos = worldCamera.ScreenToWorldPoint(mousePos);
                worldPos.z = 0f;

                Vector2Int clickedCell = context.Grid.WorldToCell(worldPos);
                var btActive = activeToken.GetComponent<BoardToken>();
                if (btActive == null) return;
                Vector2Int startCell = btActive.gridPosition;

                // 1. Check if clicked an enemy/other token to attack
                CampaignRuntimeToken targetToken = null;

                // A. Physics2D Raycast check
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null)
                {
                    var hitToken = hit.collider.GetComponent<CampaignRuntimeToken>();
                    if (hitToken == null)
                    {
                        hitToken = hit.collider.GetComponentInParent<CampaignRuntimeToken>();
                    }
                    if (hitToken != null && hitToken != activeToken && !hitToken.IsPlayerViewClone && !hitToken.IsDead)
                    {
                        targetToken = hitToken;
                    }
                }

                // B. Fallback grid footprint cell check
                if (targetToken == null)
                {
                    var allTokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                    foreach (var t in allTokens)
                    {
                        if (t == activeToken || t.IsPlayerViewClone || t.IsDead) continue;
                        var bt = t.GetComponent<BoardToken>();
                        if (bt != null)
                        {
                            if (clickedCell.x >= bt.gridPosition.x && clickedCell.x < bt.gridPosition.x + bt.footprintSize &&
                                clickedCell.y >= bt.gridPosition.y && clickedCell.y < bt.gridPosition.y + bt.footprintSize)
                            {
                                targetToken = t;
                                break;
                            }
                        }
                    }
                }

                if (targetToken != null)
                {
                    if (!CanAttack(activeToken, targetToken))
                    {
                        return;
                    }

                    var btTarget = targetToken.GetComponent<BoardToken>();
                    int dist = GetTokenDistance(btActive, btTarget);
                    int maxRange = GetMaxAbilityRange(activeToken);
                    if (dist <= maxRange)
                    {
                        InitiateAttackSequence(activeToken, targetToken);
                        return;
                    }
                    else
                    {
                        // Target is clicked but out of range. Block movement from falling through!
                        return;
                    }
                }

                // 2. Check if clicked a highlighted green cell to move
                // Distance is measured in footprint-steps, not individual cells
                int fp = Mathf.Max(1, btActive != null ? btActive.footprintSize : 1);
                int cellDist = Mathf.Max(Mathf.Abs(clickedCell.x - startCell.x), Mathf.Abs(clickedCell.y - startCell.y));
                int moveSteps = Mathf.CeilToInt((float)cellDist / fp);
                // Snap the destination to a footprint-aligned position
                int destX = startCell.x + Mathf.RoundToInt((float)(clickedCell.x - startCell.x) / fp) * fp;
                int destY = startCell.y + Mathf.RoundToInt((float)(clickedCell.y - startCell.y) / fp) * fp;
                Vector2Int dest = new Vector2Int(destX, destY);
                if (moveSteps > 0 && moveSteps <= activeToken.CurrentMovementPoints)
                {
                    if (RPGTable.Board.GridHighlighter.Instance != null &&
                        RPGTable.Board.GridHighlighter.Instance.MovePositions.Contains(dest))
                    {
                        activeToken.CurrentMovementPoints -= moveSteps;
                        CampaignGameSession.MoveToken(
                            string.IsNullOrEmpty(activeToken.PlayerId) ? activeToken.RuntimeId : activeToken.PlayerId,
                            context.CurrentMapNode.id,
                            dest
                        );
                        SelectRuntimeToken(activeToken);
                    }
                }
            }
        }

        private bool PrimaryMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        private Vector3 InputMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        public int GetMaxAbilityRange(CampaignRuntimeToken token)
        {
            var player = string.IsNullOrEmpty(token.PlayerId) ? null : RPGTable.Runtime.CampaignGameSession.FindPlayer(token.PlayerId);
            var charData = (player != null && player.characterRuntimeData != null)
                ? player.characterRuntimeData
                : (string.IsNullOrEmpty(token.CharacterPath) 
                    ? null 
                    : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(token.CharacterPath));
            return GetMaxAbilityRangeFromData(charData, token.ActiveWeaponIndex);
        }

        public static int GetMaxAbilityRange(string characterPath, int activeWeaponIndex)
        {
            var charData = string.IsNullOrEmpty(characterPath) 
                ? null 
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);
            return GetMaxAbilityRangeFromData(charData, activeWeaponIndex);
        }

        public static int GetMaxAbilityRangeFromData(RPGTable.CharacterEditor.SavedCharacterData charData, int activeWeaponIndex)
        {
            int maxRange = 1;
            if (charData != null)
            {
                var slots = (activeWeaponIndex == 0) ? charData.attackSlots : charData.attack2Slots;
                if (slots != null)
                {
                    foreach (var name in slots)
                    {
                        int r = GetAbilityRangeStatic(name);
                        if (r > maxRange) maxRange = r;
                    }
                }
            }
            return maxRange;
        }

        private static int GetAbilityRangeStatic(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            var cards = Resources.LoadAll<AbilityCard>("AbilityCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, name, StringComparison.OrdinalIgnoreCase))
                {
                    return card.range;
                }
            }
            return 0;
        }

        public bool IsPlayerConnected(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return false;
            if (WebServerManager.LastSeenTimes.TryGetValue(playerId, out var lastSeen))
            {
                return (System.DateTime.UtcNow - lastSeen).TotalSeconds < 10.0;
            }
            return false;
        }

        private int GetAbilityRange(string name)
        {
            return GetAbilityRangeStatic(name);
        }

        public void InitiateAttackSequence(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            if (!RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                if (RPGTable.Runtime.CombatManager.Instance != null)
                {
                    RPGTable.Runtime.CombatManager.Instance.StartCombat();
                }
                attacker.CurrentRolls = attacker.MaxRolls;
            }

            Debug.Log($"[CampaignGameLoader] InitiateAttackSequence: attacker={attacker.DisplayName}, target={target.DisplayName}, rolls={attacker.CurrentRolls}");
            if (attacker.CurrentRolls <= 0)
            {
                Debug.LogWarning($"[CampaignGameLoader] Attacker {attacker.DisplayName} has no rolls left ({attacker.CurrentRolls})!");
                SpawnCombatText(attacker, "НЕТ АТАК!", Color.red, 32f);
                return;
            }

            var player = string.IsNullOrEmpty(attacker.PlayerId) ? null : RPGTable.Runtime.CampaignGameSession.FindPlayer(attacker.PlayerId);
            var charData = (player != null && player.characterRuntimeData != null)
                ? player.characterRuntimeData
                : (string.IsNullOrEmpty(attacker.CharacterPath) 
                    ? null 
                    : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(attacker.CharacterPath));

            if (charData == null)
            {
                Debug.LogWarning($"[CampaignGameLoader] Attacker {attacker.DisplayName} character data is null (Path: {attacker.CharacterPath})!");
                return;
            }

            // Check if attacker is player and is connected
            if (!string.IsNullOrEmpty(attacker.PlayerId) && IsPlayerConnected(attacker.PlayerId))
            {
                Debug.Log($"[CampaignGameLoader] Attacker is player (Id: {attacker.PlayerId}) and is connected. Creating pending roll...");
                // Create PendingRoll for the player
                if (WebServerManager.Instance != null)
                {
                    var attackRoll = new WebServerManager.PendingRoll
                    {
                        id = System.Guid.NewGuid().ToString("N"),
                        type = "attack",
                        playerId = attacker.PlayerId,
                        targetTokenId = target.RuntimeId,
                        canReroll = attacker.RerollCoins > 0,
                        rerollCost = 1
                    };
                    WebServerManager.Instance.ActiveRolls[attacker.PlayerId] = attackRoll;
                    Debug.Log($"[CampaignGameLoader] Registered pending roll in WebServerManager. ActiveRolls count={WebServerManager.Instance.ActiveRolls.Count}");
                }
                else
                {
                    Debug.LogError("[CampaignGameLoader] WebServerManager.Instance is null!");
                }
            }
            else
            {
                Debug.Log($"[CampaignGameLoader] Attacker '{attacker.DisplayName}' is NPC or disconnected player. Auto-rolling D6 attack...");
                // Attacker is NPC or disconnected player. Perform auto D6 roll
                int roll = UnityEngine.Random.Range(1, 7);
                ProcessAttackRoll(attacker, target, roll);
            }
        }

        public bool SubmitRoll(string playerId, int rollResult)
        {
            if (WebServerManager.Instance == null) return false;
            if (!WebServerManager.Instance.ActiveRolls.TryGetValue(playerId, out var pr)) return false;

            // Remove from active rolls
            WebServerManager.Instance.ActiveRolls.Remove(playerId);

            CampaignRuntimeToken attackerToken = null;
            CampaignRuntimeToken targetToken = null;

            if (pr.type == "attack")
            {
                attackerToken = FindTokenByPlayerIdOrRuntimeId(pr.playerId);
                targetToken = FindTokenByPlayerIdOrRuntimeId(pr.targetTokenId);

                if (attackerToken == null || targetToken == null || targetToken.IsDead)
                {
                    return false;
                }

                ProcessAttackRoll(attackerToken, targetToken, rollResult);
                return true;
            }
            else if (pr.type == "defense")
            {
                // For defense rolls, pr.attackerTokenId is the attacker, pr.targetTokenId is the defender
                attackerToken = FindTokenByPlayerIdOrRuntimeId(pr.attackerTokenId);
                targetToken = FindTokenByPlayerIdOrRuntimeId(pr.targetTokenId);

                if (attackerToken == null || targetToken == null || targetToken.IsDead)
                {
                    return false;
                }

                var attackAbility = FindAbilityCard(pr.attackerAbilityName);
                ProcessDefenseRoll(attackerToken, targetToken, attackAbility, pr.attackerRollResult, pr.baseDamage, rollResult);
                return true;
            }

            return false;
        }

        private CampaignRuntimeToken FindTokenByPlayerIdOrRuntimeId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var tokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            foreach (var t in tokens)
            {
                if (!t.IsPlayerViewClone && (t.PlayerId == id || t.RuntimeId == id)) return t;
            }
            return null;
        }

        public void ProcessAttackRoll(CampaignRuntimeToken attacker, CampaignRuntimeToken target, int attackRoll)
        {
            Debug.Log($"[CampaignGameLoader] ProcessAttackRoll starting: attacker={attacker.DisplayName}, target={target.DisplayName}, roll={attackRoll}");
            attacker.CurrentRolls = Mathf.Max(0, attacker.CurrentRolls - 1);

            Debug.Log($"[CampaignGameLoader] Attacker CharacterPath: '{attacker.CharacterPath}'");
            var player = string.IsNullOrEmpty(attacker.PlayerId) ? null : RPGTable.Runtime.CampaignGameSession.FindPlayer(attacker.PlayerId);
            var charData = (player != null && player.characterRuntimeData != null)
                ? player.characterRuntimeData
                : (string.IsNullOrEmpty(attacker.CharacterPath) 
                    ? null 
                    : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(attacker.CharacterPath));

            if (charData == null)
            {
                Debug.LogWarning($"[CampaignGameLoader] ProcessAttackRoll aborted: charData is null for attacker {attacker.DisplayName} (Path: {attacker.CharacterPath})");
                return;
            }
            Debug.Log($"[CampaignGameLoader] Character data loaded successfully for {attacker.DisplayName}.");

            SpawnCombatText(attacker, $"КУБИК: {attackRoll}", new Color(1f, 0.55f, 0f, 1f), 30f);

            int slotIndex = attackRoll - 1;
            string abilityName = "";

            var slots = (attacker.ActiveWeaponIndex == 0) ? charData.attackSlots : charData.attack2Slots;
            if (slots != null && slotIndex < slots.Length && !string.IsNullOrEmpty(slots[slotIndex]))
            {
                abilityName = slots[slotIndex];
            }

            if (string.IsNullOrEmpty(abilityName))
            {
                StartCoroutine(ExecuteMissRoutine(attacker, target));
                return;
            }

            AbilityCard ability = FindAbilityCard(abilityName);
            if (ability == null)
            {
                StartCoroutine(ExecuteMissRoutine(attacker, target));
                return;
            }

            // Calculate base damage
            float baseDmg = 2f;
            string weaponName = (attacker.ActiveWeaponIndex == 0) ? charData.eqWeapon : charData.eqWeapon2;
            RPGTable.Core.ItemCard weapon = FindItemCard(weaponName);

            if (weapon != null)
            {
                float scaling = 0f;
                scaling += GetStatValue(charData, weapon.scaleStat1) * weapon.coef1;
                scaling += GetStatValue(charData, weapon.scaleStat2) * weapon.coef2;
                baseDmg = Mathf.Max(2f, scaling);
            }

            int finalDamage = Mathf.RoundToInt(baseDmg * ability.multiplier);

            // Is target a player?
            // Is target a player and is connected?
            if (!string.IsNullOrEmpty(target.PlayerId) && IsPlayerConnected(target.PlayerId))
            {
                // Target is a player! Create a defense pending roll
                if (WebServerManager.Instance != null)
                {
                    var defenseRoll = new WebServerManager.PendingRoll
                    {
                        id = System.Guid.NewGuid().ToString("N"),
                        type = "defense",
                        playerId = target.PlayerId,
                        targetTokenId = target.RuntimeId,
                        attackerTokenId = attacker.RuntimeId,
                        attackerAbilityName = ability.title,
                        attackerRollResult = attackRoll,
                        baseDamage = finalDamage,
                        canReroll = target.RerollCoins > 0,
                        rerollCost = 1
                    };
                    WebServerManager.Instance.ActiveRolls[target.PlayerId] = defenseRoll;
                }
                
                SpawnCombatText(attacker, ability.title, Color.yellow, 30f);
            }
            else
            {
                // Target is NPC/monster or disconnected player. Roll defense automatically!
                int defenseRoll = UnityEngine.Random.Range(1, 7);
                ProcessDefenseRoll(attacker, target, ability, attackRoll, finalDamage, defenseRoll);
            }
        }

        private IEnumerator ExecuteMissRoutine(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            SpawnCombatText(target, "ПРОМАХ", Color.gray, 32f);

            AnimateTokenAttack(attacker, target);

            yield return new WaitForSeconds(0.5f);

            SelectRuntimeToken(attacker);

            if (attacker.CurrentRolls <= 0)
            {
                CombatManager.Instance.EndTokenTurn();
            }
        }

        public void ProcessDefenseRoll(CampaignRuntimeToken attacker, CampaignRuntimeToken target, AbilityCard attackAbility, int attackRoll, float baseDamage, int defenseRoll)
        {
            StartCoroutine(ExecuteDefenseAndResolveDamageRoutine(attacker, target, attackAbility, attackRoll, baseDamage, defenseRoll));
        }

        private IEnumerator ExecuteDefenseAndResolveDamageRoutine(CampaignRuntimeToken attacker, CampaignRuntimeToken target, AbilityCard attackAbility, int attackRoll, float baseDamage, int defenseRoll)
        {
            string attackTitle = attackAbility != null ? attackAbility.title : "Атака";
            Debug.Log($"[CampaignGameLoader] ExecuteDefenseAndResolveDamageRoutine: Attacker={attacker.DisplayName}, Target={target.DisplayName}, Ability={attackTitle}, BaseDmg={baseDamage}, DefRoll={defenseRoll}");
            SpawnCombatText(attacker, attackTitle, Color.yellow, 30f);

            var charData = string.IsNullOrEmpty(target.CharacterPath)
                ? null
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(target.CharacterPath);

            AbilityCard defenseAbility = null;
            if (charData != null && charData.defenseSlots != null)
            {
                int slotIndex = defenseRoll - 1;
                if (slotIndex >= 0 && slotIndex < charData.defenseSlots.Length)
                {
                    defenseAbility = FindAbilityCard(charData.defenseSlots[slotIndex]);
                    if (defenseAbility != null && defenseAbility.attackType != AttackType.Defense)
                    {
                        defenseAbility = null;
                    }
                }
            }

            SpawnCombatText(target, $"ЗАЩИТА D6: {defenseRoll}", Color.cyan, 28f);
            if (defenseAbility != null)
            {
                SpawnCombatText(target, defenseAbility.title, Color.cyan, 28f);
            }

            AnimateTokenAttack(attacker, target);
            AnimateTokenDamage(target, attacker);

            yield return new WaitForSeconds(0.3f);

            string weaponName = "";
            var attackerCharData = string.IsNullOrEmpty(attacker.CharacterPath)
                ? null
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(attacker.CharacterPath);
            if (attackerCharData != null)
            {
                weaponName = (attacker.ActiveWeaponIndex == 0) ? attackerCharData.eqWeapon : attackerCharData.eqWeapon2;
            }
            RPGTable.Core.ItemCard weapon = FindItemCard(weaponName);

            int finalDamage = Mathf.RoundToInt(baseDamage);
            if (defenseAbility != null)
            {
                int defenseValue = Mathf.Max(0, defenseAbility.defenseValue);
                if (defenseValue > 0)
                {
                    SpawnCombatText(target, $"ЗАЩИТА -{defenseValue}", Color.cyan, 30f);
                    finalDamage = Mathf.Max(0, finalDamage - defenseValue);
                }
            }

            int armorDamage = 0;
            if (target.CurrentArmor > 0)
            {
                int armorAbsorb = Mathf.Min(target.CurrentArmor, finalDamage);
                target.CurrentArmor -= armorAbsorb;
                finalDamage -= armorAbsorb;
                armorDamage = armorAbsorb;
            }

            if (armorDamage > 0)
            {
                SpawnCombatText(target, $"-{armorDamage} ARM", new Color(0.62f, 0.72f, 1f, 1f), 30f);
            }

            if (finalDamage > 0)
            {
                target.CurrentHp = Mathf.Max(0, target.CurrentHp - finalDamage);
                SpawnCombatText(target, $"-{finalDamage} HP", new Color(1f, 0.18f, 0.12f, 1f), 34f);
            }
            Debug.Log($"[CampaignGameLoader] ExecuteDefenseAndResolveDamageRoutine applied: Target={target.DisplayName}, HP={target.CurrentHp}/{target.MaxHp}, Armor={target.CurrentArmor}/{target.MaxArmor}");

            // Apply ability attributes
            if (attackAbility != null && attackAbility.attributes != null)
            {
                foreach (var attr in attackAbility.attributes)
                {
                    ApplyAttributeEffect(attacker, target, attr);
                }
            }

            // Apply weapon attributes
            if (weapon != null && weapon.attributes != null)
            {
                foreach (var attr in weapon.attributes)
                {
                    ApplyAttributeEffect(attacker, target, attr);
                }
            }

            // Apply defense attributes using defender as source and attacker as opponent.
            if (defenseAbility != null && defenseAbility.attributes != null)
            {
                foreach (var attr in defenseAbility.attributes)
                {
                    ApplyAttributeEffect(target, attacker, attr);
                }
            }

            if (!string.IsNullOrWhiteSpace(target.PlayerId))
            {
                var player = CampaignGameSession.FindPlayer(target.PlayerId);
                if (player != null)
                {
                    player.currentHp = target.CurrentHp;
                }
            }

            if (target.CurrentHp <= 0)
            {
                KillRuntimeToken(target);
            }

            if (UI != null)
            {
                UI.RefreshActiveTokensPanel();
                UI.RefreshEntityInspector(target);
                UI.RefreshEntityInspector(attacker);
            }

            yield return new WaitForSeconds(0.2f);

            SelectRuntimeToken(attacker);

            if (attacker.CurrentRolls <= 0)
            {
                CombatManager.Instance.EndTokenTurn();
            }
        }

        private AbilityCard RollDefenseAbility(CampaignRuntimeToken defender)
        {
            var charData = string.IsNullOrEmpty(defender.CharacterPath)
                ? null
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(defender.CharacterPath);

            if (charData == null || charData.defenseSlots == null)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(1, 7);
            SpawnCombatText(defender, $"\u0417\u0410\u0429\u0418\u0422\u0410 D6: {roll}", Color.cyan, 28f);

            int slotIndex = roll - 1;
            if (slotIndex < 0 || slotIndex >= charData.defenseSlots.Length)
            {
                return null;
            }

            AbilityCard defenseAbility = FindAbilityCard(charData.defenseSlots[slotIndex]);
            if (defenseAbility == null || defenseAbility.attackType != AttackType.Defense)
            {
                return null;
            }

            SpawnCombatText(defender, defenseAbility.title, Color.cyan, 28f);
            return defenseAbility;
        }

        private void SpawnCombatText(CampaignRuntimeToken token, string message, Color color, float fontSize)
        {
            if (token == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var textOffset = FloatingTextOffset(token);
            CombatManager.Instance.SpawnFloatingText(
                token.transform.position + textOffset,
                message,
                color,
                1.05f,
                fontSize,
                token.gameObject.layer,
                FloatingTextStackKey(token, false));

            if (pvManager != null)
            {
                var playerViewToken = pvManager.GetPlayerViewTokenTransform(token);
                if (playerViewToken != null)
                {
                    CombatManager.Instance.SpawnFloatingText(
                        playerViewToken.position + textOffset,
                        message,
                        color,
                        1.05f,
                        fontSize,
                        playerViewToken.gameObject.layer,
                        FloatingTextStackKey(token, true));
                }
            }
        }

        private static int FloatingTextStackKey(CampaignRuntimeToken token, bool playerView)
        {
            var id = token.RuntimeId;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = token.PlayerId;
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                id = token.DisplayName;
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                id = token.name;
            }

            return ((playerView ? "pv:" : "gm:") + id).GetHashCode();
        }

        private static Vector3 FloatingTextOffset(CampaignRuntimeToken token)
        {
            var footprint = Mathf.Max(1, token.FootprintSize);
            var boardToken = token.GetComponent<BoardToken>();
            if (boardToken != null)
            {
                footprint = Mathf.Max(1, boardToken.footprintSize);
            }

            return new Vector3(0f, footprint * 0.5f + 0.35f, 0f);
        }

        private AbilityCard FindAbilityCard(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var abilities = Resources.LoadAll<AbilityCard>("AbilityCards");
            foreach (var ability in abilities)
            {
                if (ability != null && string.Equals(ability.title, name, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[CampaignGameLoader] FindAbilityCard: Found card for '{name}' with asset '{ability.name}' (Title: '{ability.title}')");
                    return ability;
                }
            }

            Debug.LogWarning($"[CampaignGameLoader] FindAbilityCard: FAILED to find card for '{name}'! Total loaded={abilities.Length}");
            return null;
        }

        private bool CanAttack(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            if (attacker == null || target == null || attacker == target || target.IsDead)
            {
                return false;
            }

            return AreHostile(attacker.Team, target.Team);
        }

        private static bool AreHostile(TokenTeam attackerTeam, TokenTeam targetTeam)
        {
            if (attackerTeam == targetTeam)
            {
                return false;
            }

            bool attackerIsParty = attackerTeam == TokenTeam.Player || attackerTeam == TokenTeam.Ally;
            bool targetIsParty = targetTeam == TokenTeam.Player || targetTeam == TokenTeam.Ally;

            if (attackerIsParty && targetIsParty)
            {
                return false;
            }

            return true;
        }

        private void ApplyAttributeEffect(CampaignRuntimeToken attacker, CampaignRuntimeToken target, CombatAttribute attr)
        {
            if (attr == null) return;



            CampaignRuntimeToken subject = attr.appliedToSelf ? attacker : target;

            if (attr.durationTurns <= 0)
            {
                ApplyStatChange(subject, attr.affectedStat, attr.value);
            }
            else
            {
                var debuff = new ActiveStatusEffect
                {
                    effectName = attr.attributeName,
                    affectedStat = attr.affectedStat,
                    value = attr.value,
                    durationTurns = attr.durationTurns
                };
                subject.statusEffects.Add(debuff);
            }

            if (CombatManager.Instance != null && subject != null && attr != null && attr.icon != null)
            {
                CombatManager.Instance.SpawnFloatingStatusIcon(subject.transform.position, attr.icon);
            }
        }

        private void ApplyStatChange(CampaignRuntimeToken token, CombatAttributeStat stat, int delta)
        {
            switch (stat)
            {
                case CombatAttributeStat.HP:
                    token.CurrentHp = Mathf.Clamp(token.CurrentHp + delta, 0, token.MaxHp);
                    break;
                case CombatAttributeStat.MovementPoints:
                    token.CurrentMovementPoints = Mathf.Max(0, token.CurrentMovementPoints + delta);
                    break;
                case CombatAttributeStat.Rolls:
                    token.CurrentRolls = Mathf.Max(0, token.CurrentRolls + delta);
                    break;
                case CombatAttributeStat.Armor:
                    token.CurrentArmor = Mathf.Clamp(token.CurrentArmor + delta, 0, token.MaxArmor);
                    break;
            }
        }

        private RPGTable.Core.ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.ItemCard>("ItemCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[CampaignGameLoader] FindItemCard: Found item card for '{title}' with asset '{card.name}'");
                    return card;
                }
            }
            Debug.LogWarning($"[CampaignGameLoader] FindItemCard: FAILED to find item card for '{title}'! Total loaded={cards.Length}");
            return null;
        }

        private int GetStatValue(RPGTable.CharacterEditor.SavedCharacterData charData, string statName)
        {
            if (charData == null) return 10;
            switch (statName.ToUpper())
            {
                case "STR": return charData.strength;
                case "AGI": return charData.agility;
                case "INT": return charData.intelligence;
                case "HOL": return charData.holiness;
                default: return 0;
            }
        }

        private void AnimateTokenAttack(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            var anim = attacker.gameObject.GetComponent<TokenAttackAnimator>();
            if (anim == null) anim = attacker.gameObject.AddComponent<TokenAttackAnimator>();
            anim.AnimateAttack(target.transform.position);

            if (pvManager != null)
            {
                var pvAttacker = pvManager.GetPlayerViewTokenTransform(attacker);
                var pvTarget = pvManager.GetPlayerViewTokenTransform(target);
                if (pvAttacker != null && pvTarget != null)
                {
                    var pvAnim = pvAttacker.gameObject.GetComponent<TokenAttackAnimator>();
                    if (pvAnim == null) pvAnim = pvAttacker.gameObject.AddComponent<TokenAttackAnimator>();
                    pvAnim.AnimateAttack(pvTarget.position);
                }
            }
        }

        private void AnimateTokenDamage(CampaignRuntimeToken target, CampaignRuntimeToken attacker)
        {
            var tAnim = target.gameObject.GetComponent<TokenAttackAnimator>();
            if (tAnim == null) tAnim = target.gameObject.AddComponent<TokenAttackAnimator>();
            tAnim.AnimateDamage(attacker.transform.position);

            if (pvManager != null)
            {
                var pvTarget = pvManager.GetPlayerViewTokenTransform(target);
                var pvAttacker = pvManager.GetPlayerViewTokenTransform(attacker);
                if (pvTarget != null && pvAttacker != null)
                {
                    var pvTAnim = pvTarget.gameObject.GetComponent<TokenAttackAnimator>();
                    if (pvTAnim == null) pvTAnim = pvTarget.gameObject.AddComponent<TokenAttackAnimator>();
                    pvTAnim.AnimateDamage(pvAttacker.position);
                }
            }
        }

        public static int GetTokenDistance(BoardToken t1, BoardToken t2)
        {
            if (t1 == null || t2 == null) return 9999;

            int minDist = 9999;
            for (int x1 = 0; x1 < t1.footprintSize; x1++)
            {
                for (int y1 = 0; y1 < t1.footprintSize; y1++)
                {
                    Vector2Int cell1 = new Vector2Int(t1.gridPosition.x + x1, t1.gridPosition.y + y1);

                    for (int x2 = 0; x2 < t2.footprintSize; x2++)
                    {
                        for (int y2 = 0; y2 < t2.footprintSize; y2++)
                        {
                            Vector2Int cell2 = new Vector2Int(t2.gridPosition.x + x2, t2.gridPosition.y + y2);

                            int dist = Mathf.Max(Mathf.Abs(cell1.x - cell2.x), Mathf.Abs(cell1.y - cell2.y));
                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }
                    }
                }
            }
            return minDist;
        }
    }
}

