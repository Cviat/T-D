using RPGTable.Board;
using RPGTable.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPGTable.GameMaster
{
    public enum TableViewMode
    {
        Master,
        Player
    }

    public sealed class ViewModeController : MonoBehaviour
    {
        public static ViewModeController Instance { get; private set; }

        [SerializeField] private TableViewMode mode;
        [SerializeField] private FogOfWarController fogOfWar;

        public TableViewMode Mode => mode;
        public bool IsPlayerView => mode == TableViewMode.Player;

        public void ToggleMode()
        {
            SetMode(IsPlayerView ? TableViewMode.Master : TableViewMode.Player);
        }

        public void SetMode(TableViewMode nextMode)
        {
            mode = nextMode;
            ApplyMode();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ApplyMode();
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                ToggleMode();
            }
        }

        private bool WasTogglePressed()
        {
            return UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame;
        }

        private void ApplyMode()
        {
            foreach (var token in FindObjectsByType<BoardToken>(FindObjectsInactive.Exclude))
            {
                token.ApplyViewMode(IsPlayerView);
            }

            if (fogOfWar != null)
            {
                fogOfWar.SetPlayerView(IsPlayerView);
            }
        }
    }
}
