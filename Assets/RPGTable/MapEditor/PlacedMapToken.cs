using UnityEngine;
using RPGTable.Board;
using RPGTable.Core;

namespace RPGTable.MapEditor
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlacedMapToken : MonoBehaviour
    {
        public string displayName;
        public string characterPath;
        public string tokenPath;
        public Vector2Int gridPosition;
        public TokenTeam team = TokenTeam.Enemy;
        public bool visibleToPlayers = true;

        private Camera mainCamera;
        private BoardGrid activeGrid;
        private bool moving;
        private Vector3 dragOffset;
        private int footprintSize = 1;

        public void Initialize(string displayName, string characterPath, string tokenPath, Vector2 worldPos, TokenTeam team, bool visibleToPlayers, int footprint, bool isNewPlacement = false)
        {
            this.displayName = displayName;
            this.characterPath = characterPath;
            this.tokenPath = tokenPath;
            this.team = team;
            this.visibleToPlayers = visibleToPlayers;
            this.footprintSize = footprint;

            mainCamera = Camera.main;
            
            // Find active grid
            GameObject boardGridObj = GameObject.Find("Map Editor Board");
            if (boardGridObj != null) activeGrid = boardGridObj.GetComponent<BoardGrid>();
            if (activeGrid == null) activeGrid = FindFirstObjectByType<BoardGrid>();

            transform.position = new Vector3(worldPos.x, worldPos.y, -0.2f);

            if (isNewPlacement)
            {
                SnapToGridPosition();
            }
            else
            {
                if (activeGrid != null)
                {
                    gridPosition = activeGrid.WorldToCell(transform.position);
                }
            }

            UpdateTeamIndicator();
        }

        public void SetTeam(TokenTeam newTeam)
        {
            this.team = newTeam;
            UpdateTeamIndicator();
        }

        private void UpdateTeamIndicator()
        {
            var indicatorName = "Team Indicator";
            var indicatorTransform = transform.Find(indicatorName);
            GameObject indicatorGo;
            if (indicatorTransform == null)
            {
                indicatorGo = new GameObject(indicatorName);
                indicatorGo.transform.SetParent(transform, false);
                indicatorGo.transform.localPosition = Vector3.zero;
                
                var sr = indicatorGo.AddComponent<SpriteRenderer>();
                sr.sprite = RuntimeSpriteFactory.Circle;
                sr.sortingOrder = 9; // Render behind the portrait (which is at 10)
            }
            else
            {
                indicatorGo = indicatorTransform.gameObject;
            }

            var srIndicator = indicatorGo.GetComponent<SpriteRenderer>();
            if (srIndicator != null)
            {
                Color ringColor;
                switch (team)
                {
                    case TokenTeam.Enemy:
                        ringColor = new Color(0.9f, 0.15f, 0.15f, 0.85f);
                        break;
                    case TokenTeam.Ally:
                        ringColor = new Color(0.15f, 0.85f, 0.15f, 0.85f);
                        break;
                    case TokenTeam.Neutral:
                        ringColor = new Color(0.9f, 0.8f, 0.1f, 0.85f);
                        break;
                    default:
                        ringColor = new Color(0.15f, 0.6f, 0.9f, 0.85f);
                        break;
                }
                srIndicator.color = ringColor;
                
                float size = footprintSize * 1.15f; 
                var spriteSize = srIndicator.sprite != null ? srIndicator.sprite.bounds.size : Vector3.one;
                float maxSide = Mathf.Max(0.01f, Mathf.Max(spriteSize.x, spriteSize.y));
                float scale = size / maxSide;
                indicatorGo.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnMouseDown()
        {
            moving = true;
            dragOffset = transform.position - MouseWorld();
        }

        private void OnMouseDrag()
        {
            if (!moving) return;
            transform.position = MouseWorld() + dragOffset;
        }

        private void OnMouseUp()
        {
            moving = false;
            SnapToGridPosition();
        }

        private void OnMouseOver()
        {
            #if ENABLE_INPUT_SYSTEM
            bool rightClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
            #else
            bool rightClick = UnityEngine.Input.GetMouseButtonDown(1);
            #endif

            if (rightClick)
            {
                #if ENABLE_INPUT_SYSTEM
                var mousePos = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
                #else
                var mousePos = (Vector2)UnityEngine.Input.mousePosition;
                #endif
                MapEditorTokenSettingsPopup.Show(mousePos, this);
            }
        }

        private void SnapToGridPosition()
        {
            if (activeGrid == null) return;
            
            // Snap position based on footprint
            Vector2Int cell = activeGrid.WorldToCell(transform.position);
            
            // Keep within grid bounds
            cell.x = Mathf.Clamp(cell.x, 0, activeGrid.width - footprintSize);
            cell.y = Mathf.Clamp(cell.y, 0, activeGrid.height - footprintSize);
            
            gridPosition = cell;
            
            // Set position (zOffset = -0.2f to render in front of map elements but behind UI)
            var offset = new Vector3((footprintSize - 1) * activeGrid.cellSize * 0.5f, (footprintSize - 1) * activeGrid.cellSize * 0.5f, -0.2f);
            transform.position = activeGrid.CellToWorld(cell) + offset;
        }

        public void SetupVisuals(string tokenPath, int footprint)
        {
            var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(tokenPath);
            if (tokenData == null) return;

            // Clear old visual children
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            float targetSize = footprint * 1.0f; // cell size is 1f
            int sortingBase = 10;

            var portraitSprite = RPGTable.TokenEditor.UserTokenStore.LoadSprite(tokenData.portraitPath);
            var frameSprite = RPGTable.TokenEditor.UserTokenStore.LoadSprite(tokenData.framePath);

            if (portraitSprite == null && frameSprite == null)
            {
                var fallback = CreateSpriteLayer("Fallback", transform, RuntimeSpriteFactory.Circle, sortingBase + 2, Color.white);
                FitSprite(fallback.transform, RuntimeSpriteFactory.Circle, targetSize);
                return;
            }

            var positionRatio = new Vector2(0f, 44f / 560f);
            var sizeRatio = new Vector2(360f / 560f, 360f / 560f);
            if (tokenData.hasPortraitMaskLayout)
            {
                positionRatio = tokenData.portraitMaskPositionRatio;
                sizeRatio = tokenData.portraitMaskSizeRatio;
            }
            var maskPos = new Vector3(positionRatio.x * targetSize, positionRatio.y * targetSize, 0f);
            var maskSize = new Vector2(Mathf.Max(0.05f, sizeRatio.x * targetSize), Mathf.Max(0.05f, sizeRatio.y * targetSize));

            if (portraitSprite != null)
            {
                var maskObject = new GameObject("Portrait Mask");
                maskObject.transform.SetParent(transform, false);
                maskObject.transform.localPosition = maskPos;
                FitSprite(maskObject.transform, RuntimeSpriteFactory.Circle, maskSize);

                var mask = maskObject.AddComponent<SpriteMask>();
                mask.sprite = RuntimeSpriteFactory.Circle;
                mask.isCustomRangeActive = true;
                mask.backSortingOrder = sortingBase + 1;
                mask.frontSortingOrder = sortingBase + 3;

                var portrait = CreateSpriteLayer("Portrait", transform, portraitSprite, sortingBase + 2, Color.white);
                portrait.transform.localPosition = maskPos;
                portrait.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                FitSpriteCover(portrait.transform, portraitSprite, maskSize);
            }

            if (frameSprite != null)
            {
                var frame = CreateSpriteLayer("Frame", transform, frameSprite, sortingBase + 4, Color.white);
                FitSprite(frame.transform, frameSprite, targetSize);
            }

            UpdateTeamIndicator();
        }

        private GameObject CreateSpriteLayer(string name, Transform parent, Sprite sprite, int sortingOrder, Color color)
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

        private void FitSprite(Transform t, Sprite sprite, float targetSize)
        {
            if (sprite == null) return;
            var spriteSize = sprite.bounds.size;
            var maxSide = Mathf.Max(0.01f, Mathf.Max(spriteSize.x, spriteSize.y));
            var scale = targetSize / maxSide;
            t.localScale = new Vector3(scale, scale, 1f);
        }

        private void FitSprite(Transform t, Sprite sprite, Vector2 targetSize)
        {
            if (sprite == null) return;
            var spriteSize = sprite.bounds.size;
            t.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.01f, spriteSize.x),
                targetSize.y / Mathf.Max(0.01f, spriteSize.y),
                1f);
        }

        private void FitSpriteCover(Transform t, Sprite sprite, float targetSize)
        {
            if (sprite == null) return;
            var spriteSize = sprite.bounds.size;
            var minSide = Mathf.Max(0.01f, Mathf.Min(spriteSize.x, spriteSize.y));
            var scale = targetSize / minSide;
            t.localScale = new Vector3(scale, scale, 1f);
        }

        private void FitSpriteCover(Transform t, Sprite sprite, Vector2 targetSize)
        {
            if (sprite == null) return;
            var spriteSize = sprite.bounds.size;
            var scaleX = targetSize.x / Mathf.Max(0.01f, spriteSize.x);
            var scaleY = targetSize.y / Mathf.Max(0.01f, spriteSize.y);
            var scale = Mathf.Max(scaleX, scaleY);
            t.localScale = new Vector3(scale, scale, 1f);
        }

        private Vector3 MouseWorld()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 mouse = Vector3.zero;
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mouse = (Vector3)UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            #else
            mouse = UnityEngine.Input.mousePosition;
            #endif
            mouse.z = Mathf.Abs(mainCamera.transform.position.z);
            var world = mainCamera.ScreenToWorldPoint(mouse);
            world.z = -0.2f;
            return world;
        }
    }
}
