using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public enum CampaignEditorButtonAction
    {
        Back,
        Save,
        Open,
        ImportCover
    }

    [RequireComponent(typeof(Button))]
    public sealed class CampaignEditorButton : MonoBehaviour
    {
        [SerializeField] private CampaignEditorController controller;
        [SerializeField] private CampaignEditorButtonAction action;

        public void Initialize(CampaignEditorController campaignController, CampaignEditorButtonAction buttonAction)
        {
            controller = campaignController;
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
                Debug.LogWarning("Campaign button has no controller.");
                return;
            }

            if (action == CampaignEditorButtonAction.Back)
            {
                controller.BackToMainMenu();
            }
            else if (action == CampaignEditorButtonAction.Save)
            {
                controller.RequestSaveCampaign();
            }
            else if (action == CampaignEditorButtonAction.Open)
            {
                controller.RequestOpenCampaign();
            }
            else
            {
                controller.RequestImportCover();
            }
        }
    }
}
