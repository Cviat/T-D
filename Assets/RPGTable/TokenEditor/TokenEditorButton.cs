using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.TokenEditor
{
    public enum TokenEditorButtonAction
    {
        Back,
        Save,
        Open,
        ImportPortrait
    }

    [RequireComponent(typeof(Button))]
    public sealed class TokenEditorButton : MonoBehaviour
    {
        [SerializeField] private TokenEditorController controller;
        [SerializeField] private TokenEditorButtonAction action;

        public void Initialize(TokenEditorController tokenEditorController, TokenEditorButtonAction buttonAction)
        {
            controller = tokenEditorController;
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

            if (action == TokenEditorButtonAction.Back)
            {
                controller.BackToMainMenu();
            }
            else if (action == TokenEditorButtonAction.Save)
            {
                controller.RequestSaveToken();
            }
            else if (action == TokenEditorButtonAction.Open)
            {
                controller.RequestOpenToken();
            }
            else if (action == TokenEditorButtonAction.ImportPortrait)
            {
                controller.ImportPortrait();
            }
        }
    }
}
