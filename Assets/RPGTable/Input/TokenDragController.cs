using RPGTable.Board;
using RPGTable.Core;
using RPGTable.GameMaster;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.Input
{
    [RequireComponent(typeof(BoardToken))]
    public sealed class TokenDragController : MonoBehaviour
    {
        private BoardToken token;
        private Camera mainCamera;
        private Vector3 dragOffset;
        private bool dragging;
        public bool IsDragging => dragging;
        private Vector2Int startGridPos;

        private void Awake()
        {
            token = GetComponent<BoardToken>();
            mainCamera = Camera.main;
        }

        private void OnMouseDown()
        {
            if (RPGTable.Runtime.CampaignPlayerViewManager.PlayerViewCameraControlActive)
            {
                return;
            }



            if (ViewModeController.Instance != null && ViewModeController.Instance.IsPlayerView)
            {
                return;
            }

            var runtimeToken = GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                var combatMgr = RPGTable.Runtime.CombatManager.Instance;
                if (combatMgr.ActiveToken != runtimeToken)
                {
                    return; // Block dragging other tokens in combat
                }
            }

            dragging = true;
            dragOffset = transform.position - MouseWorldPosition();
            startGridPos = token.gridPosition;

            if (runtimeToken != null)
            {
#if UNITY_2023_1_OR_NEWER
                var loader = FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                var loader = FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                if (loader != null)
                {
                    loader.SelectRuntimeToken(runtimeToken);
                }
            }
        }

        private void OnMouseDrag()
        {
            if (!dragging)
            {
                return;
            }

            transform.position = MouseWorldPosition() + dragOffset;
        }

        private void OnMouseUp()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;

            BoardGrid activeGrid = null;
            GameObject boardGridObj = GameObject.Find("Board Grid");
            if (boardGridObj != null)
            {
                activeGrid = boardGridObj.GetComponent<BoardGrid>();
            }
            if (activeGrid == null)
            {
#if UNITY_2023_1_OR_NEWER
                activeGrid = FindAnyObjectByType<BoardGrid>();
#else
                activeGrid = FindAnyObjectByType<BoardGrid>();
#endif
            }

            token.SnapToGrid(activeGrid);
            Vector2Int newGridPos = token.gridPosition;

            var runtimeToken = GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
            if (runtimeToken != null)
            {
                int fp = Mathf.Max(1, token.footprintSize);
                int cellDist = Mathf.Max(Mathf.Abs(newGridPos.x - startGridPos.x), Mathf.Abs(newGridPos.y - startGridPos.y));
                int steps = Mathf.Max(1, Mathf.CeilToInt((float)cellDist / fp)); // round up: partial footprint = 1 step
                if (cellDist > 0 && RPGTable.Runtime.CampaignGameSession.IsCombatActive)
                {
                    if (steps > runtimeToken.CurrentMovementPoints)
                    {
                        token.gridPosition = startGridPos;
                        var size = Mathf.Max(1, token.footprintSize);
                        var offset = activeGrid != null
                            ? new Vector3((size - 1) * activeGrid.cellSize * 0.5f, (size - 1) * activeGrid.cellSize * 0.5f, 0f)
                            : Vector3.zero;
                        transform.position = activeGrid != null
                            ? activeGrid.CellToWorld(startGridPos) + offset
                            : transform.position;
                        return;
                    }
                    else
                    {
                        runtimeToken.CurrentMovementPoints = Mathf.Max(0, runtimeToken.CurrentMovementPoints - steps);
                    }
                }

                if (cellDist > 0)
                {
                    string mapId = "";
#if UNITY_2023_1_OR_NEWER
                    var loader = FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                    var loader = FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                    if (loader != null && loader.Context != null && loader.Context.CurrentMapNode != null)
                    {
                        mapId = loader.Context.CurrentMapNode.id;
                    }



                    RPGTable.Runtime.CampaignGameSession.MoveToken(
                        string.IsNullOrEmpty(runtimeToken.PlayerId) ? runtimeToken.RuntimeId : runtimeToken.PlayerId,
                        mapId,
                        newGridPos
                    );

                    if (loader != null)
                    {
                        loader.SelectRuntimeToken(runtimeToken);
                    }
                }
            }
        }

        private Vector3 MouseWorldPosition()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            var mousePosition = CurrentMousePosition();
            mousePosition.z = Mathf.Abs(mainCamera.transform.position.z);
            var world = mainCamera.ScreenToWorldPoint(mousePosition);
            world.z = transform.position.z;
            return world;
        }

        private static Vector3 CurrentMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return Vector3.zero;
            }

            return Mouse.current.position.ReadValue();
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static bool PrimaryMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }
    }
}

