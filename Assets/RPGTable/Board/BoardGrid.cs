using UnityEngine;

namespace RPGTable.Board
{
    public sealed class BoardGrid : MonoBehaviour
    {
        public int width = 12;
        public int height = 8;
        public float cellSize = 1f;
        public Color gridColor = new Color(0.25f, 0.32f, 0.36f, 0.75f);

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return transform.position + new Vector3(
                (cell.x + 0.5f) * cellSize,
                (cell.y + 0.5f) * cellSize,
                0f);
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            var local = worldPosition - transform.position;
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize), 0, width - 1),
                Mathf.Clamp(Mathf.FloorToInt(local.y / cellSize), 0, height - 1));
        }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gridColor;

            for (var x = 0; x <= width; x++)
            {
                var start = transform.position + new Vector3(x * cellSize, 0f, 0f);
                var end = transform.position + new Vector3(x * cellSize, height * cellSize, 0f);
                Gizmos.DrawLine(start, end);
            }

            for (var y = 0; y <= height; y++)
            {
                var start = transform.position + new Vector3(0f, y * cellSize, 0f);
                var end = transform.position + new Vector3(width * cellSize, y * cellSize, 0f);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
