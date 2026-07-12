using System;
using System.Collections.Generic;
using UnityEngine;
using RPGTable.Core;

namespace RPGTable.Board
{
    public sealed class GridHighlighter : MonoBehaviour
    {
        public static GridHighlighter Instance { get; private set; }

        [SerializeField] private Color moveRangeColor = new Color(0.2f, 0.8f, 0.2f, 0.35f); // Green
        [SerializeField] private Color actionRangeColor = new Color(0.8f, 0.2f, 0.2f, 0.25f); // Red

        private BoardGrid grid;
        private readonly List<GameObject> activeHighlights = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
            grid = GetComponent<BoardGrid>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void HighlightTokenRanges(RPGTable.Runtime.CampaignRuntimeToken token)
        {
            Clear();

            if (token == null || grid == null || !RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                return;
            }

            var boardToken = token.GetComponent<BoardToken>();
            if (boardToken == null) return;

            int fp = Mathf.Max(1, boardToken.footprintSize);
            Vector2Int startCell = boardToken.gridPosition; // top-left corner of the token

            int moveSteps  = token.CurrentMovementPoints;  // steps (each step = fp cells)
            int actionRange = GetMaxAbilityRange(token);   // ability range in cells (not steps)

            // Build sets of all cells that would be painted
            HashSet<Vector2Int> moveCells   = new HashSet<Vector2Int>();
            HashSet<Vector2Int> actionCells = new HashSet<Vector2Int>();

            // --- Movement range: stamp footprint at each reachable step position ---
            for (int sx = -moveSteps; sx <= moveSteps; sx++)
            {
                for (int sy = -moveSteps; sy <= moveSteps; sy++)
                {
                    if (sx == 0 && sy == 0) continue;
                    if (Mathf.Max(Mathf.Abs(sx), Mathf.Abs(sy)) > moveSteps) continue;

                    // Top-left corner of candidate position (each step = fp cells)
                    Vector2Int topLeft = new Vector2Int(startCell.x + sx * fp, startCell.y + sy * fp);

                    // Make sure the entire footprint fits on the board
                    if (topLeft.x < 0 || topLeft.y < 0 ||
                        topLeft.x + fp > grid.width || topLeft.y + fp > grid.height)
                        continue;

                    // Stamp all fp×fp cells for this position
                    for (int fx = 0; fx < fp; fx++)
                        for (int fy = 0; fy < fp; fy++)
                            moveCells.Add(new Vector2Int(topLeft.x + fx, topLeft.y + fy));
                }
            }

            // --- Action range: Chebyshev in cells from the token's CENTER ---
            // (ability range is about reach, not step-size)
            Vector2 center = new Vector2(startCell.x + (fp - 1) * 0.5f, startCell.y + (fp - 1) * 0.5f);
            int maxR = actionRange;
            for (int x = -maxR; x <= maxR; x++)
            {
                for (int y = -maxR; y <= maxR; y++)
                {
                    Vector2Int targetCell = new Vector2Int(Mathf.RoundToInt(center.x) + x, Mathf.RoundToInt(center.y) + y);
                    if (!grid.Contains(targetCell)) continue;

                    // Skip cells already occupied by the token itself
                    bool ownCell = targetCell.x >= startCell.x && targetCell.x < startCell.x + fp &&
                                   targetCell.y >= startCell.y && targetCell.y < startCell.y + fp;
                    if (ownCell) continue;

                    int dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (dist <= actionRange)
                        actionCells.Add(targetCell);
                }
            }

            // Draw action range first (underneath), then movement range on top
            foreach (var cell in actionCells)
                SpawnHighlight(cell, actionRangeColor);

            foreach (var cell in moveCells)
                SpawnHighlight(cell, moveRangeColor);

            // Spawn reticle on targetable enemies in action range
            var allTokens = GameObject.FindObjectsByType<RPGTable.Runtime.CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            Sprite reticleSprite = Resources.Load<Sprite>("image/Gemini_Generated_Image_m2vmn7m2vmn7m2vm");
            foreach (var t in allTokens)
            {
                if (t == token || t.IsPlayerViewClone || t.IsDead) continue;
                if (!AreHostile(token.Team, t.Team)) continue;

                var bt = t.GetComponent<BoardToken>();
                if (bt == null) continue;

                int dist = RPGTable.Runtime.CampaignGameLoader.GetTokenDistance(boardToken, bt);
                if (dist <= actionRange)
                {
                    SpawnReticle(bt, reticleSprite);
                }
            }
        }

        private void SpawnReticle(BoardToken targetToken, Sprite reticleSprite)
        {
            var reticleGo = new GameObject("ActionTargetReticle");
            reticleGo.transform.SetParent(transform, false);
            reticleGo.transform.position = targetToken.transform.position + new Vector3(0f, 0f, -0.5f);

            var renderer = reticleGo.AddComponent<SpriteRenderer>();
            renderer.sprite = reticleSprite;
            renderer.color = new Color(1f, 0.2f, 0.2f, 0.85f);
            
            // Render on top of the token by finding the maximum sorting order of its sprite layers
            int maxSortingOrder = 0;
            var renderers = targetToken.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                if (r.sortingOrder > maxSortingOrder)
                {
                    maxSortingOrder = r.sortingOrder;
                }
            }
            renderer.sortingOrder = maxSortingOrder + 1;

            if (reticleSprite != null)
            {
                float size = targetToken.footprintSize * grid.cellSize;
                reticleGo.transform.localScale = new Vector3(size / reticleSprite.bounds.size.x, size / reticleSprite.bounds.size.y, 1f);
            }

            activeHighlights.Add(reticleGo);
        }

        private void SpawnHighlight(Vector2Int cell, Color color)
        {
            var highlightGo = new GameObject("GridHighlight");
            highlightGo.transform.SetParent(transform, false);
            highlightGo.transform.position = grid.CellToWorld(cell) + new Vector3(0f, 0f, -0.05f); // slightly in front of map, behind tokens
            highlightGo.transform.localScale = new Vector3(grid.cellSize * 0.95f, grid.cellSize * 0.95f, 1f); // slightly smaller for a nice border effect!

            var renderer = highlightGo.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.Square;
            renderer.color = color;
            renderer.sortingOrder = -5; // Behind tokens (usually sortingOrder 0+), in front of background grid (sortingOrder -20)

            activeHighlights.Add(highlightGo);
        }

        public void Clear()
        {
            foreach (var hl in activeHighlights)
            {
                if (hl != null)
                {
                    Destroy(hl);
                }
            }
            activeHighlights.Clear();
        }

        private int GetMaxAbilityRange(RPGTable.Runtime.CampaignRuntimeToken token)
        {
            int maxRange = 1; // Default melee range is 1 cell
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

        private static bool AreHostile(TokenTeam attackerTeam, TokenTeam targetTeam)
        {
            bool attackerIsParty = attackerTeam == TokenTeam.Player || attackerTeam == TokenTeam.Ally;
            bool targetIsParty = targetTeam == TokenTeam.Player || targetTeam == TokenTeam.Ally;

            if (attackerIsParty && targetTeam == TokenTeam.Enemy)
            {
                return true;
            }

            if (attackerTeam == TokenTeam.Enemy && targetIsParty)
            {
                return true;
            }

            return false;
        }
    }
}
