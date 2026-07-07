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

            if (!PrimaryMousePressed())
            {
                return;
            }

            if (ViewModeController.Instance != null && ViewModeController.Instance.IsPlayerView)
            {
                return;
            }

            dragging = true;
            dragOffset = transform.position - MouseWorldPosition();
            startGridPos = token.gridPosition;

            var runtimeToken = GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
            if (runtimeToken != null)
            {
#if UNITY_2023_1_OR_NEWER
                var loader = FindFirstObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                var loader = FindObjectOfType<RPGTable.Runtime.CampaignGameLoader>();
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
                activeGrid = FindFirstObjectByType<BoardGrid>();
#else
                activeGrid = FindObjectOfType<BoardGrid>();
#endif
            }

            token.SnapToGrid(activeGrid);

            var runtimeToken = GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
            if (runtimeToken != null)
            {
                int distance = Mathf.Max(Mathf.Abs(token.gridPosition.x - startGridPos.x), Mathf.Abs(token.gridPosition.y - startGridPos.y));
                if (distance > 0 && RPGTable.Runtime.CampaignGameSession.IsCombatActive)
                {
                    runtimeToken.CurrentMovementPoints = Mathf.Max(0, runtimeToken.CurrentMovementPoints - distance);
                }

#if UNITY_2023_1_OR_NEWER
                var loader = FindFirstObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                var loader = FindObjectOfType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                if (loader != null)
                {
                    loader.SelectRuntimeToken(runtimeToken);
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

