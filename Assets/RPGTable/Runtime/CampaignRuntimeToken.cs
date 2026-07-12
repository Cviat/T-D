using UnityEngine;
using System;
using System.Collections.Generic;

namespace RPGTable.Runtime
{
    using RPGTable.Core;

    public sealed class CampaignRuntimeToken : MonoBehaviour
    {
        public string PlayerId { get; set; }
        public string RuntimeId { get; set; }
        public string TokenPath { get; set; }
        public string CharacterPath { get; set; }
        public string DisplayName { get; set; }
        public TokenTeam Team { get; set; }
        public bool VisibleToPlayers { get; set; }
        public bool IsPlayerViewClone { get; set; }

        private bool isInitializing = true;
        private Vector3 targetWorldPosition;
        private bool hasTargetPosition = false;

        private void Start()
        {
            isInitializing = false;
            targetWorldPosition = transform.position;
            hasTargetPosition = true;

            var boardToken = GetComponent<RPGTable.Core.BoardToken>();
            if (boardToken != null)
            {
                string mapId = GetMyMapId();
                string myId = string.IsNullOrEmpty(PlayerId) ? RuntimeId : PlayerId;
                if (!string.IsNullOrEmpty(mapId) && !string.IsNullOrEmpty(myId))
                {
                    if (string.IsNullOrEmpty(PlayerId))
                    {
                        var npc = CampaignGameSession.FindNPCState(mapId, myId);
                        if (npc == null)
                        {
                            var states = CampaignGameSession.MapTokenStates.ContainsKey(mapId) 
                                ? CampaignGameSession.MapTokenStates[mapId] 
                                : new List<CampaignGameSession.RuntimeMapTokenState>();
                            states.Add(new CampaignGameSession.RuntimeMapTokenState
                            {
                                runtimeId = myId,
                                displayName = DisplayName,
                                characterPath = CharacterPath,
                                tokenPath = TokenPath,
                                team = Team,
                                visibleToPlayers = VisibleToPlayers,
                                gridPosition = boardToken.gridPosition,
                                isDead = IsDead,
                                currentHp = CurrentHp,
                                maxHp = MaxHp,
                                currentArmor = CurrentArmor,
                                maxArmor = MaxArmor
                            });
                            CampaignGameSession.MapTokenStates[mapId] = states;
                        }
                    }
                    else
                    {
                        var player = CampaignGameSession.FindPlayer(myId);
                        if (player != null)
                        {
                            player.gridX = boardToken.gridPosition.x;
                            player.gridY = boardToken.gridPosition.y;
                            player.currentMapId = mapId;
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            CampaignGameSession.OnTokenDataChanged += HandleTokenDataChanged;
            CampaignGameSession.OnTokenPositionChanged += HandleTokenPositionChanged;
            CampaignGameSession.OnTokenActionTriggered += HandleTokenActionTriggered;
            CampaignGameSession.OnTokenFocused += HandleTokenFocused;
        }

        private void OnDisable()
        {
            CampaignGameSession.OnTokenDataChanged -= HandleTokenDataChanged;
            CampaignGameSession.OnTokenPositionChanged -= HandleTokenPositionChanged;
            CampaignGameSession.OnTokenActionTriggered -= HandleTokenActionTriggered;
            CampaignGameSession.OnTokenFocused -= HandleTokenFocused;
        }

        private void Update()
        {
            var dragController = GetComponent<RPGTable.Input.TokenDragController>();
            if (dragController != null && dragController.IsDragging)
            {
                return;
            }

            if (hasTargetPosition)
            {
                transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * 6f);
                if (Vector3.Distance(transform.position, targetWorldPosition) < 0.002f)
                {
                    transform.position = targetWorldPosition;
                }
            }
        }

        private void HandleTokenDataChanged(string id, string mapId, int hp, int armor, bool dead)
        {
            string myId = string.IsNullOrEmpty(PlayerId) ? RuntimeId : PlayerId;
            if (id == myId && mapId == GetMyMapId())
            {
                bool wasDead = isDead;

                // Sync stats from session data directly
                if (CampaignGameSession.TryGetTokenCombatStats(myId, mapId, out int sesHp, out int sesMaxHp, out int sesArmor, out int sesMaxArmor, out int movement, out int maxMovement, out int rolls, out int maxRolls, out int activeWeapon, out int coins, out var effects, out bool sesDead))
                {
                    currentHp = sesHp;
                    MaxHp = sesMaxHp;
                    currentArmor = sesArmor;
                    MaxArmor = sesMaxArmor;
                    currentMovementPoints = movement;
                    maxMovementPoints = maxMovement;
                    currentRolls = rolls;
                    maxRolls = maxRolls;
                    activeWeaponIndex = activeWeapon;
                    rerollCoins = coins;
                    statusEffects = effects ?? new List<ActiveStatusEffect>();
                    isDead = sesDead;
                }
                else
                {
                    currentHp = hp;
                    currentArmor = armor;
                    isDead = dead;
                }


                if (!wasDead && isDead)
                {
                    TriggerDeathVisual();
                }

                // If GM view, refresh UI inspector
                if (!IsPlayerViewClone)
                {
                    var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
                    if (loader != null && loader.UI != null)
                    {
                        loader.UI.RefreshActiveTokensPanel();
                        bool isSelectedOrActive = (loader.Context != null && loader.Context.SelectedToken == this)
                            || (CombatManager.Instance != null && CombatManager.Instance.ActiveTokenId == myId);
                        if (isSelectedOrActive)
                        {
                            loader.UI.RefreshEntityInspector(this);
                            if (RPGTable.Board.GridHighlighter.Instance != null)
                            {
                                RPGTable.Board.GridHighlighter.Instance.HighlightTokenRanges(this);
                            }
                        }
                    }
                }
            }
        }

        private void HandleTokenPositionChanged(string id, string mapId, Vector2Int cell)
        {
            string myId = string.IsNullOrEmpty(PlayerId) ? RuntimeId : PlayerId;
            if (id == myId && mapId == GetMyMapId())
            {
                UpdateTargetWorldPosition(cell);
                var boardToken = GetComponent<RPGTable.Core.BoardToken>();
                if (boardToken != null)
                {
                    boardToken.gridPosition = cell;
                }

                if (!IsPlayerViewClone)
                {
                    var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
                    bool isSelectedOrActive = (loader != null && loader.Context != null && loader.Context.SelectedToken == this)
                        || (CombatManager.Instance != null && CombatManager.Instance.ActiveTokenId == myId);
                    if (isSelectedOrActive && RPGTable.Board.GridHighlighter.Instance != null)
                    {
                        RPGTable.Board.GridHighlighter.Instance.HighlightTokenRanges(this);
                    }
                }
            }
        }

        private void HandleTokenActionTriggered(string attackerId, string targetId, string actionType, string details)
        {
            string myId = string.IsNullOrEmpty(PlayerId) ? RuntimeId : PlayerId;
            if (attackerId == myId && actionType == "attack")
            {
                var targetToken = FindVisualById(targetId);
                if (targetToken != null)
                {
                    var anim = gameObject.GetComponent<TokenAttackAnimator>();
                    if (anim == null) anim = gameObject.AddComponent<TokenAttackAnimator>();
                    anim.AnimateAttack(targetToken.transform.position);
                }
            }
            else if (targetId == myId && actionType == "damage")
            {
                var attackerToken = FindVisualById(attackerId);
                if (attackerToken != null)
                {
                    var anim = gameObject.GetComponent<TokenAttackAnimator>();
                    if (anim == null) anim = gameObject.AddComponent<TokenAttackAnimator>();
                    anim.AnimateDamage(attackerToken.transform.position);
                }
            }
        }

        private void HandleTokenFocused(string id)
        {
            string myId = string.IsNullOrEmpty(PlayerId) ? RuntimeId : PlayerId;
            if (id == myId)
            {
                FocusCameraLocal();
            }
        }

        private CampaignRuntimeToken FindVisualById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var tokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            foreach (var t in tokens)
            {
                if (t.gameObject.layer == gameObject.layer && (t.RuntimeId == id || t.PlayerId == id))
                {
                    return t;
                }
            }
            return null;
        }

        private void FocusCameraLocal()
        {
            if (IsPlayerViewClone)
            {
                var pvCamObj = GameObject.Find("Player View Camera");
                if (pvCamObj != null)
                {
                    var pvCam = pvCamObj.GetComponent<Camera>();
                    if (pvCam != null)
                    {
                        var targetPos = new Vector3(transform.position.x, transform.position.y, pvCam.transform.position.z);
                        StartCoroutine(SmoothPanCamera(pvCam.transform, targetPos));
                    }
                }
            }
            else
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var targetPos = new Vector3(transform.position.x, transform.position.y, cam.transform.position.z);
                    StartCoroutine(SmoothPanCamera(cam.transform, targetPos));
                }
            }
        }

        private System.Collections.IEnumerator SmoothPanCamera(Transform camTransform, Vector3 targetPos)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startPos = camTransform.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                camTransform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }
            camTransform.position = targetPos;
        }

        private void UpdateTargetWorldPosition(Vector2Int cell)
        {
            if (IsPlayerViewClone)
            {
                if (CampaignPlayerViewManager.Instance != null)
                {
                    targetWorldPosition = CampaignPlayerViewManager.Instance.GetPlayerViewTokenWorldPosition(cell, FootprintSize);
                    hasTargetPosition = true;
                }
            }
            else
            {
                var grid = GameObject.FindAnyObjectByType<RPGTable.Board.BoardGrid>();
                if (grid != null)
                {
                    int size = Mathf.Max(1, FootprintSize);
                    var offset = new Vector3((size - 1) * grid.cellSize * 0.5f, (size - 1) * grid.cellSize * 0.5f, 0f);
                    targetWorldPosition = grid.CellToWorld(cell) + offset;
                    hasTargetPosition = true;
                }
            }
        }

        private void TriggerDeathVisual()
        {
            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            if (loader != null && loader.Spawner != null)
            {
                loader.Spawner.ApplyDeadVisual(this, FootprintSize);
            }
        }

        private bool isDead;
        public bool IsDead
        {
            get => isDead;
            set
            {
                if (isDead == value) return;
                isDead = value;
                SyncToSession();
            }
        }

        public int FootprintSize { get; set; } = 1;
        public int MaxHp { get; set; }

        private int currentHp;
        public int CurrentHp
        {
            get => currentHp;
            set
            {
                if (currentHp == value) return;
                currentHp = value;
                SyncToSession();
            }
        }

        public int MaxArmor { get; set; }

        private int currentArmor;
        public int CurrentArmor
        {
            get => currentArmor;
            set
            {
                if (currentArmor == value) return;
                currentArmor = value;
                SyncToSession();
            }
        }

        private int maxMovementPoints = 3;
        public int MaxMovementPoints
        {
            get => maxMovementPoints;
            set { if (maxMovementPoints == value) return; maxMovementPoints = value; SyncToSession(); }
        }

        private int currentMovementPoints = 3;
        public int CurrentMovementPoints
        {
            get => currentMovementPoints;
            set { if (currentMovementPoints == value) return; currentMovementPoints = value; SyncToSession(); }
        }

        private int maxRolls = 1;
        public int MaxRolls
        {
            get => maxRolls;
            set { if (maxRolls == value) return; maxRolls = value; SyncToSession(); }
        }

        private int currentRolls = 1;
        public int CurrentRolls
        {
            get => currentRolls;
            set { if (currentRolls == value) return; currentRolls = value; SyncToSession(); }
        }

        private int activeWeaponIndex = 0;
        public int ActiveWeaponIndex
        {
            get => activeWeaponIndex;
            set { if (activeWeaponIndex == value) return; activeWeaponIndex = value; SyncToSession(); }
        }

        private int rerollCoins = 3;
        public int RerollCoins
        {
            get => rerollCoins;
            set { if (rerollCoins == value) return; rerollCoins = value; SyncToSession(); }
        }
        public List<ActiveStatusEffect> statusEffects = new List<ActiveStatusEffect>();

        public void InitializeStats(int maxHp, int curHp, int maxArmor, int curArmor, bool dead)
        {
            isInitializing = true;

            MaxHp = maxHp;
            MaxArmor = maxArmor;

            string mapId = GetMyMapId();
            if (!string.IsNullOrEmpty(mapId) && CampaignGameSession.TryGetTokenData(RuntimeId, mapId, out int sesHp, out int sesMaxHp, out int sesArmor, out int sesMaxArmor, out bool sesDead))
            {
                currentHp = sesHp;
                currentArmor = sesArmor;
                isDead = sesDead;
                MaxHp = sesMaxHp > 0 ? sesMaxHp : MaxHp;
                MaxArmor = sesMaxArmor > 0 ? sesMaxArmor : MaxArmor;
            }
            else
            {
                currentHp = curHp;
                currentArmor = curArmor;
                isDead = dead;
                if (!string.IsNullOrEmpty(mapId))
                {
                    CampaignGameSession.UpdateTokenData(RuntimeId, mapId, currentHp, currentArmor, isDead);
                }
            }

            isInitializing = false;
        }

        private string GetMyMapId()
        {
            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            if (loader != null)
            {
                if (IsPlayerViewClone)
                {
                    if (loader.PVManager != null) return loader.PVManager.PlayerViewMapId;
                }
                else
                {
                    if (loader.Context != null && loader.Context.CurrentMapNode != null)
                    {
                        return loader.Context.CurrentMapNode.id;
                    }
                }
            }
            return "";
        }

        private void SyncToSession()
        {
            if (isInitializing) return;

            string mapId = GetMyMapId();
            if (!string.IsNullOrEmpty(mapId) && !string.IsNullOrEmpty(RuntimeId))
            {
                CampaignGameSession.UpdateTokenCombatStats(
                    RuntimeId, mapId,
                    currentHp, MaxHp,
                    currentArmor, MaxArmor,
                    CurrentMovementPoints, MaxMovementPoints,
                    CurrentRolls, MaxRolls,
                    ActiveWeaponIndex, RerollCoins,
                    statusEffects, isDead);
            }
        }
    }
}
