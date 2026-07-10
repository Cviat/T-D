using System;
using System.Collections;
using RPGTable.MapEditor;
using RPGTable.Core;
using UnityEngine;
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
            HandleCombatInputs();
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
                    if (hitToken != null && hitToken != activeToken && !hitToken.IsDead)
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
                        if (t == activeToken || t.IsDead) continue;
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
                    var btTarget = targetToken.GetComponent<BoardToken>();
                    int dist = GetTokenDistance(btActive, btTarget);
                    int maxRange = GetMaxAbilityRange(activeToken);
                    if (dist <= maxRange)
                    {
                        ExecuteD6Attack(activeToken, targetToken);
                        return;
                    }
                    else
                    {
                        // Target is clicked but out of range. Block movement from falling through!
                        return;
                    }
                }

                // 2. Check if clicked a highlighted green cell to move
                int moveDist = Mathf.Max(Mathf.Abs(clickedCell.x - startCell.x), Mathf.Abs(clickedCell.y - startCell.y));
                if (moveDist > 0 && moveDist <= activeToken.CurrentMovementPoints)
                {
                    var offset = new Vector3((btActive.footprintSize - 1) * context.Grid.cellSize * 0.5f,
                                             (btActive.footprintSize - 1) * context.Grid.cellSize * 0.5f, 0f);
                    activeToken.transform.position = context.Grid.CellToWorld(clickedCell) + offset;
                    btActive.gridPosition = clickedCell;

                    activeToken.CurrentMovementPoints -= moveDist;

                    SelectRuntimeToken(activeToken);
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

        private int GetMaxAbilityRange(CampaignRuntimeToken token)
        {
            int maxRange = 1;
            var charData = string.IsNullOrEmpty(token.CharacterPath) 
                ? null 
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(token.CharacterPath);

            if (charData != null)
            {
                var slots = (token.ActiveWeaponIndex == 0) ? charData.attackSlots : charData.attack2Slots;
                if (slots != null)
                {
                    foreach (var name in slots)
                    {
                        int r = GetAbilityRange(name);
                        if (r > maxRange) maxRange = r;
                    }
                }
            }
            return maxRange;
        }

        private int GetAbilityRange(string name)
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

        private void ExecuteD6Attack(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            if (attacker.CurrentRolls <= 0)
            {
                CombatManager.Instance.SpawnTextBanner("НЕТ АТАК!", Color.red, 1.5f, false);
                return;
            }

            var charData = string.IsNullOrEmpty(attacker.CharacterPath) 
                ? null 
                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(attacker.CharacterPath);

            if (charData == null) return;

            int roll = UnityEngine.Random.Range(1, 7);
            CombatManager.Instance.SpawnTextBanner($"КУБИК: {roll}", new Color(1f, 0.5f, 0f, 1f), 1.5f, false);

            int slotIndex = roll - 1;
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

            AbilityCard ability = null;
            var abilities = Resources.LoadAll<AbilityCard>("AbilityCards");
            foreach (var a in abilities)
            {
                if (a != null && string.Equals(a.title, abilityName, StringComparison.OrdinalIgnoreCase))
                {
                    ability = a;
                    break;
                }
            }

            if (ability == null)
            {
                StartCoroutine(ExecuteMissRoutine(attacker, target));
                return;
            }

            StartCoroutine(ExecuteHitRoutine(attacker, target, ability, charData));
        }

        private IEnumerator ExecuteMissRoutine(CampaignRuntimeToken attacker, CampaignRuntimeToken target)
        {
            attacker.CurrentRolls--;
            CombatManager.Instance.SpawnTextBanner("ПРОМАХ!", Color.gray, 1.5f, false);

            AnimateTokenAttack(attacker, target);

            yield return new WaitForSeconds(0.5f);

            SelectRuntimeToken(attacker);

            if (attacker.CurrentRolls <= 0)
            {
                CombatManager.Instance.EndTokenTurn();
            }
        }

        private IEnumerator ExecuteHitRoutine(CampaignRuntimeToken attacker, CampaignRuntimeToken target, AbilityCard ability, RPGTable.CharacterEditor.SavedCharacterData charData)
        {
            attacker.CurrentRolls--;
            CombatManager.Instance.SpawnTextBanner(ability.title, Color.yellow, 1.5f, false);

            AnimateTokenAttack(attacker, target);
            AnimateTokenDamage(target, attacker);

            yield return new WaitForSeconds(0.3f);

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

            // Check if ShieldBuff is active on target statusEffects
            ActiveStatusEffect shieldEffect = null;
            if (target.statusEffects != null)
            {
                foreach (var effect in target.statusEffects)
                {
                    if (string.Equals(effect.effectName, "ShieldBuff", StringComparison.OrdinalIgnoreCase) || string.Equals(effect.effectName, "Эгида", StringComparison.OrdinalIgnoreCase))
                    {
                        shieldEffect = effect;
                        break;
                    }
                }
            }

            if (shieldEffect != null)
            {
                target.statusEffects.Remove(shieldEffect);
                CombatManager.Instance.SpawnTextBanner("БЛОК!", Color.cyan, 1.5f, false);
                finalDamage = 0;
            }

            // Check if Pierce is active on the ability or weapon attributes
            bool isPierce = false;
            if (ability.attributes != null)
            {
                foreach (var attr in ability.attributes)
                {
                    if (attr != null && (string.Equals(attr.attributeName, "Pierce", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие брони", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие", StringComparison.OrdinalIgnoreCase)))
                        isPierce = true;
                }
            }
            if (weapon != null && weapon.attributes != null)
            {
                foreach (var attr in weapon.attributes)
                {
                    if (attr != null && (string.Equals(attr.attributeName, "Pierce", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие брони", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие", StringComparison.OrdinalIgnoreCase)))
                        isPierce = true;
                }
            }

            if (isPierce)
            {
                target.CurrentHp = Mathf.Max(0, target.CurrentHp - finalDamage);
            }
            else
            {
                if (target.CurrentArmor > 0)
                {
                    int armorAbsorb = Mathf.Min(target.CurrentArmor, finalDamage);
                    target.CurrentArmor -= armorAbsorb;
                    finalDamage -= armorAbsorb;
                }

                if (finalDamage > 0)
                {
                    target.CurrentHp = Mathf.Max(0, target.CurrentHp - finalDamage);
                }
            }

            // Apply ability attributes
            if (ability.attributes != null)
            {
                foreach (var attr in ability.attributes)
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

            yield return new WaitForSeconds(0.2f);

            SelectRuntimeToken(attacker);

            if (attacker.CurrentRolls <= 0)
            {
                CombatManager.Instance.EndTokenTurn();
            }
        }

        private void ApplyAttributeEffect(CampaignRuntimeToken attacker, CampaignRuntimeToken target, CombatAttribute attr)
        {
            if (attr == null) return;

            // Pierce is handled separately during damage calculation
            if (string.Equals(attr.attributeName, "Pierce", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие брони", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.attributeName, "Пробитие", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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
                    durationTurns = attr.durationTurns,
                    appliedToSelf = attr.appliedToSelf
                };
                subject.statusEffects.Add(debuff);
            }
        }

        private void ApplyStatChange(CampaignRuntimeToken token, string stat, int delta)
        {
            if (string.IsNullOrEmpty(stat)) return;
            switch (stat.ToUpper())
            {
                case "HP":
                    token.CurrentHp = Mathf.Clamp(token.CurrentHp + delta, 0, token.MaxHp);
                    break;
                case "MOVEMENTPOINTS":
                    token.CurrentMovementPoints = Mathf.Max(0, token.CurrentMovementPoints + delta);
                    break;
                case "ROLLS":
                    token.CurrentRolls = Mathf.Max(0, token.CurrentRolls + delta);
                    break;
                case "ARMOR":
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
                    return card;
                }
            }
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
