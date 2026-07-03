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
        private BoardGrid grid;
        private BoardToken token;
        private Camera mainCamera;
        private Vector3 dragOffset;
        private bool dragging;

        private void Awake()
        {
            token = GetComponent<BoardToken>();
            grid = FindFirstObjectByType<BoardGrid>();
            mainCamera = Camera.main;
        }

        private void OnMouseDown()
        {
            if (RPGTable.Runtime.CampaignGameLoader.PlayerViewCameraControlActive)
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
            token.SnapToGrid(grid);
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
