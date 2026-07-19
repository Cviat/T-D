using System.Collections.Generic;
using RPGTable.Board;
using RPGTable.Core;
using RPGTable.MapEditor;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Loads map data from disk, builds map scene elements (sprites, grid, exit/spawn zones),
    /// manages the map cache, and handles camera focus and world cleanup.
    /// </summary>
    internal sealed class CampaignMapLoader
    {
        private const float CellSize = 1f;

        private readonly Dictionary<string, SavedMapData> loadedMaps = new Dictionary<string, SavedMapData>();

        private Transform mapRoot;
        private Transform tokenRoot;
        private BoardGrid grid;

        public Transform MapRoot => mapRoot;
        public Transform TokenRoot => tokenRoot;
        public BoardGrid Grid => grid;

        public SavedMapData GetMap(string mapPath)
        {
            if (string.IsNullOrWhiteSpace(mapPath))
            {
                return null;
            }

            if (!loadedMaps.TryGetValue(mapPath, out var data))
            {
                data = UserMapStore.LoadMap(mapPath);
                loadedMaps[mapPath] = data;
            }

            return data;
        }

        public void CreateMapRoots()
        {
            mapRoot = new GameObject("Loaded Map").transform;
            tokenRoot = new GameObject("Board Tokens").transform;
        }

        public Bounds BuildMapElements(SavedMapData data)
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, new Vector3(12f, 8f, 0f));

            if (data.elements == null)
            {
                return bounds;
            }

            foreach (var element in data.elements)
            {
                var sprite = UserElementAssetStore.LoadSprite(element.imagePath);

                if (sprite == null)
                {
                    continue;
                }

                var objectName = string.IsNullOrWhiteSpace(sprite.name) ? "Map Element" : sprite.name;
                var gameObject = new GameObject(objectName);
                gameObject.transform.SetParent(mapRoot, false);
                gameObject.transform.position = element.position;
                gameObject.transform.localScale = element.scale;

                var renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -15;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            // --- Spawn Table Background behind the map ---
            Sprite tableBgSprite = Resources.Load<Sprite>("image/a5ba2c8a-d20b-4021-9baf-3f45ea61fa8d (2)");

            if (tableBgSprite != null)
            {
                var tableGo = new GameObject("Table Background");
                tableGo.transform.SetParent(mapRoot, false);

                var renderer = tableGo.AddComponent<SpriteRenderer>();
                renderer.sprite = tableBgSprite;
                renderer.sortingOrder = -25; // Render behind map elements
                renderer.drawMode = SpriteDrawMode.Sliced;

                // Get sprite borders (in pixels)
                Vector4 border = tableBgSprite.border; // left, bottom, right, top
                float ppu = tableBgSprite.pixelsPerUnit;
                if (ppu <= 0f) ppu = 100f;

                float left = border.x / ppu;
                float bottom = border.y / ppu;
                float right = border.z / ppu;
                float top = border.w / ppu;

                float mapWidth = bounds.size.x;
                float mapHeight = bounds.size.y;

                float totalWidth = mapWidth + left + right;
                float totalHeight = mapHeight + bottom + top;

                renderer.size = new Vector2(totalWidth, totalHeight);

                float offsetX = (left - right) / 2f;
                float offsetY = (bottom - top) / 2f;

                tableGo.transform.position = new Vector3(bounds.center.x - offsetX, bounds.center.y - offsetY, 0.5f);
            }

            return bounds;
        }

        public BoardGrid BuildGrid(Bounds mapBounds, SavedMapData data)
        {
            mapBounds = ExpandBoundsForGrid(mapBounds, data);

            var gridObject = new GameObject("Board Grid");
            gridObject.transform.position = new Vector3(
                Mathf.Floor(mapBounds.min.x),
                Mathf.Floor(mapBounds.min.y),
                0f);

            grid = gridObject.AddComponent<BoardGrid>();
            grid.cellSize = CellSize;
            grid.width = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.x / CellSize));
            grid.height = Mathf.Max(4, Mathf.CeilToInt(mapBounds.size.y / CellSize));
            gridObject.AddComponent<BoardGridVisual>();
            gridObject.AddComponent<RPGTable.Board.GridHighlighter>();

            return grid;
        }

        public void BuildExitZones(SavedMapData data)
        {
            if (data.exitPoints != null)
            {
                foreach (var exit in data.exitPoints)
                {
                    var zone = new GameObject(string.IsNullOrWhiteSpace(exit.name) ? "Exit Zone" : exit.name);
                    zone.transform.SetParent(mapRoot, false);
                    zone.transform.position = new Vector3(exit.position.x, exit.position.y, -0.05f);
                    zone.transform.localScale = new Vector3(
                        Mathf.Max(0.1f, exit.size.x),
                        Mathf.Max(0.1f, exit.size.y),
                        1f);

                    var renderer = zone.AddComponent<SpriteRenderer>();
                    renderer.sprite = RuntimeSpriteFactory.Square;
                    renderer.color = new Color(0.2f, 0.75f, 1f, 0.22f);
                    renderer.sortingOrder = -4;
                }
            }

            if (data.spawnZones == null)
            {
                return;
            }

            foreach (var spawn in data.spawnZones)
            {
                var zone = new GameObject(string.IsNullOrWhiteSpace(spawn.name) ? "Spawn Zone" : spawn.name);
                zone.transform.SetParent(mapRoot, false);
                zone.transform.position = new Vector3(spawn.position.x, spawn.position.y, -0.045f);
                zone.transform.localScale = new Vector3(
                    Mathf.Max(0.1f, spawn.size.x),
                    Mathf.Max(0.1f, spawn.size.y),
                    1f);

                var renderer = zone.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSpriteFactory.Square;
                renderer.color = new Color(0.25f, 1f, 0.35f, 0.18f);
                renderer.sortingOrder = -3;
            }
        }

        public void ClearWorld()
        {
            DestroyIfExists(mapRoot);
            DestroyIfExists(tokenRoot);

            if (grid != null)
            {
                Object.Destroy(grid.gameObject);
                grid = null;
            }

            mapRoot = null;
            tokenRoot = null;
        }

        public static void FocusCamera(Bounds bounds, Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return;
            }

            var center = bounds.center;
            worldCamera.transform.position = new Vector3(center.x, center.y, -10f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = Mathf.Max(6f,
                Mathf.Max(bounds.extents.y, bounds.extents.x / worldCamera.aspect) * 1.25f);
        }

        internal static Bounds ExpandBoundsForGrid(Bounds mapBounds, SavedMapData data)
        {
            if (data.exitPoints != null)
            {
                foreach (var exit in data.exitPoints)
                {
                    mapBounds.Encapsulate(new Bounds(exit.position, new Vector3(exit.size.x, exit.size.y, 0f)));
                }
            }

            if (data.spawnZones != null)
            {
                foreach (var spawn in data.spawnZones)
                {
                    mapBounds.Encapsulate(new Bounds(spawn.position, new Vector3(spawn.size.x, spawn.size.y, 0f)));
                }
            }

            mapBounds.Expand(new Vector3(2f, 2f, 0f));
            return mapBounds;
        }

        private static void DestroyIfExists(Transform root)
        {
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }
        }
    }
}
