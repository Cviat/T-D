using RPGTable.Core;
using UnityEngine;

namespace RPGTable.Board
{
    [RequireComponent(typeof(BoardGrid))]
    public sealed class BoardGridVisual : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new Color(0.32f, 0.32f, 0.32f, 1f);
        [SerializeField] private Color lineColor = new Color(0.12f, 0.12f, 0.12f, 0.55f);
        [SerializeField] private float lineWidth = 0.025f;

        private BoardGrid grid;

        private void Awake()
        {
            grid = GetComponent<BoardGrid>();
            Build();
        }

        public void Build()
        {
            grid = grid == null ? GetComponent<BoardGrid>() : grid;
            Clear();
            CreateBackground();
            CreateLines();
        }

        private void CreateBackground()
        {
            var background = new GameObject("Grid Background");
            background.transform.SetParent(transform, false);
            background.transform.localPosition = new Vector3(grid.width * grid.cellSize * 0.5f, grid.height * grid.cellSize * 0.5f, 0.1f);
            background.transform.localScale = new Vector3(grid.width * grid.cellSize, grid.height * grid.cellSize, 1f);

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.Square;
            renderer.color = backgroundColor;
            renderer.sortingOrder = -20;
        }

        private void CreateLines()
        {
            for (var x = 0; x <= grid.width; x++)
            {
                CreateLine(
                    new Vector3(x * grid.cellSize, 0f, 0f),
                    new Vector3(x * grid.cellSize, grid.height * grid.cellSize, 0f));
            }

            for (var y = 0; y <= grid.height; y++)
            {
                CreateLine(
                    new Vector3(0f, y * grid.cellSize, 0f),
                    new Vector3(grid.width * grid.cellSize, y * grid.cellSize, 0f));
            }
        }

        private void CreateLine(Vector3 start, Vector3 end)
        {
            var lineObject = new GameObject("Grid Line");
            lineObject.transform.SetParent(transform, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.sortingOrder = -10;
        }

        private void Clear()
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
        }
    }
}
