using RPGTable.Board;
using UnityEngine;

namespace RPGTable.Core
{
    public enum TokenTeam
    {
        Player,
        Ally,
        Enemy,
        Neutral
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BoardToken : MonoBehaviour
    {
        public string displayName = "Token";
        public TokenTeam team;
        public bool visibleToPlayers = true;
        public int initiative;
        public Vector2Int gridPosition;
        public int footprintSize = 1;

        private SpriteRenderer spriteRenderer;

        public void SnapToGrid(BoardGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            var size = Mathf.Max(1, footprintSize);
            var offset = new Vector3((size - 1) * grid.cellSize * 0.5f, (size - 1) * grid.cellSize * 0.5f, 0f);

            // Find nearest raw cell under the cursor
            Vector2Int raw = grid.WorldToCell(transform.position - offset);

            // Snap to footprint-aligned position: round to nearest multiple of footprintSize
            int snappedX = Mathf.RoundToInt((float)raw.x / size) * size;
            int snappedY = Mathf.RoundToInt((float)raw.y / size) * size;

            gridPosition = new Vector2Int(
                Mathf.Clamp(snappedX, 0, Mathf.Max(0, grid.width  - size)),
                Mathf.Clamp(snappedY, 0, Mathf.Max(0, grid.height - size)));

            transform.position = grid.CellToWorld(gridPosition) + offset;
        }

        public void ApplyViewMode(bool playerView)
        {
            gameObject.SetActive(!playerView || visibleToPlayers);
        }

        public void SetTint(Color color)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.color = color;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = RuntimeSpriteFactory.Circle;
            }
        }
    }
}
