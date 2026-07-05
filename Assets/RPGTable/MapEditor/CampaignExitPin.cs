using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignExitPin : MonoBehaviour
    {
        private CampaignEditorController controller;
        private Image image;

        public CampaignMapNode Owner { get; private set; }
        public string ExitId { get; private set; }
        public RectTransform RectTransform { get; private set; }

        public void Initialize(CampaignEditorController campaignController, CampaignMapNode owner, string exitId)
        {
            controller = campaignController;
            Owner = owner;
            ExitId = exitId;
            RectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            GetComponent<Button>().onClick.AddListener(() => controller.SelectPin(this));
        }

        public void SetSelected(bool selected)
        {
            if (image != null)
            {
                image.color = selected ? Color.yellow : new Color(1f, 0.25f, 0.9f, 1f);
            }
        }
    }
}
