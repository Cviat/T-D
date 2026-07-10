using System.Collections.Generic;
using System.IO;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.Input;
using RPGTable.MapEditor;
using RPGTable.TokenEditor;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Creates, manages and destroys tokens on the board.
    /// Handles occupied-cell tracking, spawn placement, token visuals,
    /// dead state, bank placement and per-map token state persistence.
    /// </summary>
    internal sealed class CampaignTokenSpawner
    {
        private const float CellSize = 1f;
        private const string DeadTokenAssetPath = "Assets/RPGTable/Art/Tokens/DeadToken.png";

        private readonly CampaignGameContext context;
        private readonly CampaignGameLoader loader;
        private readonly Dictionary<string, List<RuntimeMapTokenState>> mapTokenStates =
            new Dictionary<string, List<RuntimeMapTokenState>>();
        private readonly Dictionary<string, Vector2Int> occupiedCells =
            new Dictionary<string, Vector2Int>();
        private readonly Dictionary<string, string> pendingSpawnExitIds =
            new Dictionary<string, string>();

        private int nextTokenSortingOrder = 100;
        private Sprite deadTokenSprite;

        public string SelectedBankTokenPath { get; set; }

        internal sealed class RuntimeMapTokenState
        {
            public string runtimeId;
            public string displayName;
            public string characterPath;
            public string tokenPath;
            public TokenTeam team;
            public bool visibleToPlayers;
            public Vector2Int gridPosition;
            public bool isDead;
            public int currentHp;
        }

        internal CampaignTokenSpawner(CampaignGameContext context, CampaignGameLoader loader)
        {
            this.context = context;
            this.loader = loader;
        }

        public void SetPendingSpawnExit(string playerId, string exitId)
        {
            pendingSpawnExitIds[playerId] = exitId;
        }

        public int AllocateSortingOrder()
        {
            var order = nextTokenSortingOrder;
            nextTokenSortingOrder += 10;
            return order;
        }

        public List<RuntimeMapTokenState> GetMapTokenStates(string mapId)
        {
            if (!string.IsNullOrWhiteSpace(mapId) && mapTokenStates.TryGetValue(mapId, out var states))
            {
                return states;
            }

            return null;
        }

        // ── Token state persistence ──────────────────────────────────────

        public void SaveCurrentMapTokenState()
        {
            if (context.CurrentMapNode == null || context.TokenRoot == null || context.Grid == null)
            {
                return;
            }

            var states = new List<RuntimeMapTokenState>();

            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
            {
                var boardToken = runtimeToken.GetComponent<BoardToken>();

                if (boardToken == null)
                {
                    continue;
                }

                boardToken.SnapToGrid(context.Grid);

                if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
                {
                    var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);

                    if (player != null)
                    {
                        player.currentMapId = context.CurrentMapNode.id;
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
                    characterPath = runtimeToken.CharacterPath,
                    tokenPath = runtimeToken.TokenPath,
                    team = runtimeToken.Team,
                    visibleToPlayers = runtimeToken.VisibleToPlayers,
                    gridPosition = boardToken.gridPosition,
                    isDead = runtimeToken.IsDead,
                    currentHp = runtimeToken.CurrentHp
                });
            }

            mapTokenStates[context.CurrentMapNode.id] = states;
        }

        // ── Spawning ─────────────────────────────────────────────────────

        public void SpawnPlayersForCurrentMap(SavedMapData data)
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
                    player.currentMapId = context.CurrentMapNode.id;
                    var spawn = FindSpawnCell(data, footprint, spawnExitId);
                    player.gridX = spawn.x;
                    player.gridY = spawn.y;
                    pendingSpawnExitIds.Remove(player.id);
                }
                else if (string.IsNullOrWhiteSpace(player.currentMapId))
                {
                    player.currentMapId = context.CurrentMapNode.id;
                    var spawn = FindSpawnCell(data, footprint, null);
                    player.gridX = spawn.x;
                    player.gridY = spawn.y;
                }

                if (player.currentMapId != context.CurrentMapNode.id)
                {
                    continue;
                }

                var cell = FindFreeCell(new Vector2Int(player.gridX, player.gridY), footprint);
                player.gridX = cell.x;
                player.gridY = cell.y;
                var runtimeToken = CreateTokenObject(player.name, player.characterPath, player.tokenPath, cell, TokenTeam.Player, true, player.id);
                runtimeToken.IsDead = player.isDead;

                if (player.maxHp <= 0)
                {
                    player.maxHp = runtimeToken.MaxHp;
                }
                if (player.currentHp <= 0)
                {
                    player.currentHp = player.maxHp;
                }
                runtimeToken.CurrentHp = player.currentHp;
                runtimeToken.MaxHp = player.maxHp;

                if (runtimeToken.IsDead)
                {
                    ApplyDeadVisual(runtimeToken);
                }

                ReserveCells(player.id, cell, footprint);
            }
        }

        public void SpawnStoredTokensForCurrentMap()
        {
            if (context.CurrentMapNode == null || !mapTokenStates.TryGetValue(context.CurrentMapNode.id, out var states))
            {
                return;
            }

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var tokenData = UserTokenStore.LoadToken(state.tokenPath);
                var footprint = GetFootprint(tokenData);
                var cell = FindFreeCell(state.gridPosition, footprint);
                // For stored tokens, if it's a player, we get character path from player. Otherwise from state.
                var player = CampaignGameSession.FindPlayer(state.runtimeId);
                var charPath = player != null ? player.characterPath : state.characterPath;
                var runtimeToken = CreateTokenObject(state.displayName, charPath, state.tokenPath, cell, state.team, state.visibleToPlayers, null);
                runtimeToken.RuntimeId = string.IsNullOrWhiteSpace(state.runtimeId) ? NewRuntimeTokenId() : state.runtimeId;
                runtimeToken.IsDead = state.isDead;
                runtimeToken.CurrentHp = state.currentHp;
                if (state.currentHp <= 0 && !runtimeToken.IsDead)
                {
                    runtimeToken.CurrentHp = runtimeToken.MaxHp;
                }

                if (runtimeToken.IsDead)
                {
                    ApplyDeadVisual(runtimeToken);
                }

                ReserveCells($"stored_{context.CurrentMapNode.id}_{i}", cell, footprint);
            }
        }

        // ── Bank placement ───────────────────────────────────────────────

        public void HandleBankPlacement()
        {
            if (string.IsNullOrWhiteSpace(SelectedBankTokenPath) || context.Grid == null || context.WorldCamera == null)
            {
                return;
            }

            if (CampaignPlayerViewManager.PlayerViewCameraControlActive
                || !CampaignPlayerViewManager.MouseOnDisplay(context.WorldCamera.targetDisplay)
                || !CampaignGameUI.PrimaryMousePressed()
                || CampaignGameUI.IsPointerOverUi())
            {
                return;
            }

            var mouse = CampaignGameUI.MousePosition();
            mouse.z = Mathf.Abs(context.WorldCamera.transform.position.z);
            var world = context.WorldCamera.ScreenToWorldPoint(mouse);
            var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(SelectedBankTokenPath);
            if (charData == null) { SelectedBankTokenPath = null; return; }

            var tokenData = UserTokenStore.LoadToken(charData.tokenPath);
            var footprint = GetFootprint(tokenData);
            var cell = FindFreeCell(context.Grid.WorldToCell(world), footprint);
            CreateTokenObject(charData.name, SelectedBankTokenPath, charData.tokenPath, cell, TokenTeam.Enemy, true, null);
            ReserveCells(null, cell, footprint);
            SelectedBankTokenPath = null;
        }

        // ── Token creation ───────────────────────────────────────────────

        public CampaignRuntimeToken CreateTokenObject(string displayName, string characterPath, string tokenPath, Vector2Int cell, TokenTeam team, bool visibleToPlayers, string playerId)
        {
            var tokenData = UserTokenStore.LoadToken(tokenPath);
            var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);
            var footprint = GetFootprint(tokenData);
            var sortingBase = AllocateSortingOrder();
            var tokenObject = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Token" : displayName);
            tokenObject.transform.SetParent(context.TokenRoot, false);
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
            runtime.CharacterPath = characterPath;
            runtime.DisplayName = displayName;
            runtime.Team = team;
            runtime.VisibleToPlayers = visibleToPlayers;
            runtime.MaxHp = charData != null ? charData.maxHp : 10;
            runtime.CurrentHp = runtime.MaxHp;
            runtime.MaxArmor = charData != null ? charData.maxArmor : 0;
            runtime.CurrentArmor = runtime.MaxArmor;

            tokenObject.AddComponent<TokenDragController>();
            tokenObject.AddComponent<TokenHealthArmorBars>();
            tokenObject.AddComponent<CampaignTokenContextClick>().Initialize(loader, runtime);
            return runtime;
        }

        // ── Token visuals ────────────────────────────────────────────────

        internal void CreateTokenVisual(Transform parent, SavedTokenData tokenData, int footprint, TokenTeam team, int sortingBase)
        {
            var targetSize = Mathf.Max(0.1f, footprint * CellSize);
            var maskLayout = ResolveMaskLayout(tokenData, targetSize);
            var portraitSprite = UserTokenStore.LoadSprite(tokenData?.portraitPath);
            var frameSprite = UserTokenStore.LoadSprite(tokenData?.framePath);

            if (portraitSprite == null && frameSprite == null)
            {
                var fallback = CreateSpriteLayer("Fallback", parent, RuntimeSpriteFactory.Circle, sortingBase + 2,
                    team == TokenTeam.Player ? Color.white : new Color(1f, 0.82f, 0.65f, 1f));
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

        // ── Kill / Delete ────────────────────────────────────────────────

        public void DeleteRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return;
            }

            RebuildOccupiedCells(runtimeToken);
            Object.Destroy(runtimeToken.gameObject);
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

        public void ReviveRuntimeToken(CampaignRuntimeToken runtimeToken)
        {
            if (runtimeToken == null)
            {
                return;
            }

            runtimeToken.IsDead = false;

            if (!string.IsNullOrWhiteSpace(runtimeToken.PlayerId))
            {
                var player = CampaignGameSession.FindPlayer(runtimeToken.PlayerId);
                if (player != null)
                {
                    player.isDead = false;
                }
            }

            for (var i = runtimeToken.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(runtimeToken.transform.GetChild(i).gameObject);
            }

            var tokenData = UserTokenStore.LoadToken(runtimeToken.TokenPath);
            var boardToken = runtimeToken.GetComponent<BoardToken>();
            var footprint = boardToken != null ? boardToken.footprintSize : 1;
            var sortingBase = AllocateSortingOrder();
            CreateTokenVisual(runtimeToken.transform, tokenData, footprint, runtimeToken.Team, sortingBase);
        }

        internal void ApplyDeadVisual(CampaignRuntimeToken runtimeToken, int footprint = -1)
        {
            if (runtimeToken == null)
            {
                return;
            }

            for (var i = runtimeToken.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(runtimeToken.transform.GetChild(i).gameObject);
            }

            var boardToken = runtimeToken.GetComponent<BoardToken>();
            if (footprint <= 0)
            {
                footprint = boardToken == null ? 1 : boardToken.footprintSize;
            }
            footprint = Mathf.Max(1, footprint);
            var targetSize = Mathf.Max(0.1f, footprint * CellSize);
            var rootRenderer = runtimeToken.GetComponent<SpriteRenderer>();
            var sortingOrder = rootRenderer == null ? nextTokenSortingOrder : rootRenderer.sortingOrder + 5;
            var deadSprite = LoadDeadTokenSprite();
            var layer = CreateSpriteLayer("Dead Token", runtimeToken.transform, deadSprite, sortingOrder, Color.white);
            FitSprite(layer.transform, deadSprite, targetSize);
        }

        // ── Utility ──────────────────────────────────────────────────────

        internal static string EnsureRuntimeTokenId(CampaignRuntimeToken runtimeToken)
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

        internal static string NewRuntimeTokenId()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        internal static int GetFootprint(SavedTokenData tokenData)
        {
            return Mathf.Clamp(tokenData == null ? 1 : tokenData.footprintSize, 1, 5);
        }

        internal Vector3 TokenWorldPosition(Vector2Int cell, int footprint)
        {
            return context.Grid.CellToWorld(cell) +
                   new Vector3(
                       (footprint - 1) * context.Grid.cellSize * 0.5f,
                       (footprint - 1) * context.Grid.cellSize * 0.5f,
                       0f);
        }

        // ── Cell placement helpers ───────────────────────────────────────

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

                        return FindFreeCell(context.Grid.WorldToCell(exit.position) + Vector2Int.right, footprint);
                    }
                }
            }

            if (data.spawnZones != null && data.spawnZones.Length > 0 && TryFindFreeCellInZone(data.spawnZones[0], footprint, out var cell))
            {
                return cell;
            }

            if (data.exitPoints != null && data.exitPoints.Length > 0)
            {
                return FindFreeCell(context.Grid.WorldToCell(data.exitPoints[0].position), footprint);
            }

            return FindFreeCell(new Vector2Int(context.Grid.width / 2, context.Grid.height / 2), footprint);
        }

        private bool TryFindFreeCellNearExit(SavedMapExitPointData exit, int footprint, out Vector2Int cell)
        {
            var half = exit.size * 0.5f;
            var first = context.Grid.WorldToCell(new Vector3(exit.position.x - half.x, exit.position.y - half.y, 0f));
            var second = context.Grid.WorldToCell(new Vector3(exit.position.x + half.x, exit.position.y + half.y, 0f));
            var min = new Vector2Int(Mathf.Min(first.x, second.x), Mathf.Min(first.y, second.y));
            var max = new Vector2Int(Mathf.Max(first.x, second.x), Mathf.Max(first.y, second.y));
            var center = context.Grid.WorldToCell(exit.position);
            var directions = new[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            for (var distance = 1; distance <= Mathf.Max(context.Grid.width, context.Grid.height); distance++)
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
            var first = context.Grid.WorldToCell(new Vector3(spawnZone.position.x - half.x, spawnZone.position.y - half.y, 0f));
            var second = context.Grid.WorldToCell(new Vector3(spawnZone.position.x + half.x, spawnZone.position.y + half.y, 0f));
            var preferred = context.Grid.WorldToCell(spawnZone.position);

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

        internal Vector2Int FindFreeCell(Vector2Int preferred, int footprint)
        {
            if (context.Grid == null)
            {
                return Vector2Int.zero;
            }

            footprint = Mathf.Clamp(footprint, 1, Mathf.Max(1, Mathf.Min(context.Grid.width, context.Grid.height)));
            preferred = new Vector2Int(
                Mathf.Clamp(preferred.x, 0, Mathf.Max(0, context.Grid.width - footprint)),
                Mathf.Clamp(preferred.y, 0, Mathf.Max(0, context.Grid.height - footprint)));

            if (CanPlace(preferred, footprint))
            {
                return preferred;
            }

            for (var radius = 1; radius < Mathf.Max(context.Grid.width, context.Grid.height); radius++)
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

                    if (!context.Grid.Contains(cell) || IsCellOccupied(cell))
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

        private void RebuildOccupiedCells(CampaignRuntimeToken ignoredToken = null)
        {
            occupiedCells.Clear();

            if (context.TokenRoot == null || context.Grid == null)
            {
                return;
            }

            var runtimeIndex = 0;

            foreach (var runtimeToken in context.TokenRoot.GetComponentsInChildren<CampaignRuntimeToken>())
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

                boardToken.SnapToGrid(context.Grid);
                var ownerId = !string.IsNullOrWhiteSpace(runtimeToken.PlayerId)
                    ? runtimeToken.PlayerId
                    : $"runtime_{runtimeIndex++}";
                ReserveCells(ownerId, boardToken.gridPosition, Mathf.Max(1, boardToken.footprintSize));
            }
        }

        // ── Visual helpers ───────────────────────────────────────────────

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

        internal static GameObject CreateSpriteLayer(string name, Transform parent, Sprite sprite, int sortingOrder, Color color)
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

        internal static void FitSprite(Transform transform, Sprite sprite, float targetSize)
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

        internal static void FitSprite(Transform transform, Sprite sprite, Vector2 targetSize)
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

        internal static void FitSpriteCover(Transform transform, Sprite sprite, float targetSize)
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

        internal static void FitSpriteCover(Transform transform, Sprite sprite, Vector2 targetSize)
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
    }
}
