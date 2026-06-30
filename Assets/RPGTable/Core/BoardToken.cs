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

        private SpriteRenderer spriteRenderer;

        public void SnapToGrid(BoardGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            gridPosition = grid.WorldToCell(transform.position);
            transform.position = grid.CellToWorld(gridPosition);
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
