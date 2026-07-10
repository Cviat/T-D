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

            Vector2Int startCell = boardToken.gridPosition;

            // 1. Calculate action range (max range from equipped abilities)
            int actionRange = GetMaxAbilityRange(token);

            // 2. Calculate cells
            HashSet<Vector2Int> moveCells = new HashSet<Vector2Int>();
            HashSet<Vector2Int> actionCells = new HashSet<Vector2Int>();

            int moveRange = token.CurrentMovementPoints;

            // Chebyshev distance loop
            // Chebyshev distance: Max(abs(dx), abs(dy)) <= R
            int maxR = Mathf.Max(moveRange, actionRange);
            for (int x = -maxR; x <= maxR; x++)
            {
                for (int y = -maxR; y <= maxR; y++)
                {
                    Vector2Int targetCell = new Vector2Int(startCell.x + x, startCell.y + y);
                    if (!grid.Contains(targetCell)) continue;
                    if (targetCell == startCell) continue;

                    int dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    
                    if (dist <= moveRange)
                    {
                        moveCells.Add(targetCell);
                    }
                    else if (dist <= actionRange)
                    {
                        actionCells.Add(targetCell);
                    }
                }
            }

            // Spawn highlights
            // Action cells first (so they are rendered behind or we just draw them separately)
            foreach (var cell in actionCells)
            {
                SpawnHighlight(cell, actionRangeColor);
            }

            foreach (var cell in moveCells)
            {
                SpawnHighlight(cell, moveRangeColor);
            }

            // Spawn reticle on targetable enemies in action range
            var allTokens = GameObject.FindObjectsOfType<RPGTable.Runtime.CampaignRuntimeToken>();
            Sprite reticleSprite = Resources.Load<Sprite>("image/Gemini_Generated_Image_m2vmn7m2vmn7m2vm");
            foreach (var t in allTokens)
            {
                if (t == token || t.IsDead) continue;
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
    }
}
