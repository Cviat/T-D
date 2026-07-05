using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public enum CampaignSelectionButtonAction
    {
        Back,
        AddPlayer,
        StartGame
    }

    [RequireComponent(typeof(Button))]
    public sealed class CampaignSelectionButton : MonoBehaviour
    {
        [SerializeField] private CampaignSelectionController controller;
        [SerializeField] private CampaignSelectionButtonAction action;

        public void Initialize(CampaignSelectionController selectionController, CampaignSelectionButtonAction buttonAction)
        {
            controller = selectionController;
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
                return;
            }

            if (action == CampaignSelectionButtonAction.Back)
            {
                controller.BackToMainMenu();
            }
            else if (action == CampaignSelectionButtonAction.AddPlayer)
            {
                controller.AddDefaultPlayer();
            }
            else
            {
                controller.StartSelectedCampaign();
            }
        }
    }
}
