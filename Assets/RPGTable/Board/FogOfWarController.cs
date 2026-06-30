using System.Collections.Generic;
using RPGTable.Core;
using UnityEngine;

namespace RPGTable.Board
{
    public sealed class FogOfWarController : MonoBehaviour
    {
        [SerializeField] private BoardGrid grid;
        [SerializeField] private Color fogColor = new Color(0.02f, 0.025f, 0.03f, 0.82f);

        private readonly Dictionary<Vector2Int, SpriteRenderer> fogCells = new();
        private readonly HashSet<Vector2Int> revealedCells = new();

        public void Build(BoardGrid targetGrid)
        {
            grid = targetGrid;
            ClearExistingCells();

            if (grid == null)
            {
                return;
            }

            for (var y = 0; y < grid.height; y++)
            {
                for (var x = 0; x < grid.width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var child = new GameObject($"Fog {x},{y}");
                    child.transform.SetParent(transform, false);
                    child.transform.position = grid.CellToWorld(cell) + Vector3.back * 0.2f;
                    child.transform.localScale = Vector3.one * grid.cellSize;

                    var renderer = child.AddComponent<SpriteRenderer>();
                    renderer.sprite = RuntimeSpriteFactory.Square;
                    renderer.color = fogColor;
                    renderer.sortingOrder = 20;

                    fogCells[cell] = renderer;
                }
            }
        }

        public void Reveal(Vector2Int cell)
        {
            if (!fogCells.TryGetValue(cell, out var renderer))
            {
                return;
            }

            revealedCells.Add(cell);
            renderer.enabled = false;
        }

        public void SetPlayerView(bool playerView)
        {
            foreach (var pair in fogCells)
            {
                pair.Value.enabled = playerView && !revealedCells.Contains(pair.Key);
            }
        }

        private void Start()
        {
            if (grid != null && fogCells.Count == 0)
            {
                Build(grid);
            }
        }

        private void ClearExistingCells()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            fogCells.Clear();
            revealedCells.Clear();
        }
    }
}
