using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public enum MapEditorMapButtonAction
    {
        Back,
        Save,
        Open,
        AddExit,
        AddSpawn
    }

    [RequireComponent(typeof(Button))]
    public sealed class MapEditorMapButton : MonoBehaviour
    {
        [SerializeField] private MapEditorMapController controller;
        [SerializeField] private MapEditorMapButtonAction action;

        public void Initialize(MapEditorMapController mapController, MapEditorMapButtonAction buttonAction)
        {
            controller = mapController;
            action = buttonAction;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Click);
        }

        private void Click()
        {
            if (controller == null)
            {
                Debug.LogWarning("Map button has no controller.");
                return;
            }

            if (action == MapEditorMapButtonAction.Back)
            {
                controller.BackToMainMenu();
            }
            else if (action == MapEditorMapButtonAction.Save)
            {
                controller.RequestSaveMap();
            }
            else if (action == MapEditorMapButtonAction.Open)
            {
                controller.RequestOpenMap();
            }
            else if (action == MapEditorMapButtonAction.AddExit)
            {
                controller.BeginAddExitPoint();
            }
            else
            {
                controller.BeginAddSpawnZone();
            }
        }
    }
}
