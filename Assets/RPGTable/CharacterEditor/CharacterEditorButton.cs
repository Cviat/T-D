using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public enum CharacterEditorButtonAction
    {
        Back,
        Save,
        Open,
        ImportPortrait,
        SelectToken,
        CreateToken,
        AddAbility
    }

    [RequireComponent(typeof(Button))]
    public sealed class CharacterEditorButton : MonoBehaviour
    {
        [SerializeField] private CharacterEditorController controller;
        [SerializeField] private CharacterEditorButtonAction action;

        public void Initialize(CharacterEditorController characterController, CharacterEditorButtonAction buttonAction)
        {
            controller = characterController;
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

            if (action == CharacterEditorButtonAction.Back) controller.Back();
            else if (action == CharacterEditorButtonAction.Save) controller.RequestSave();
            else if (action == CharacterEditorButtonAction.Open) controller.RequestOpen();
            else if (action == CharacterEditorButtonAction.ImportPortrait) controller.ImportPortrait();
            else if (action == CharacterEditorButtonAction.SelectToken) controller.SelectToken();
            else if (action == CharacterEditorButtonAction.CreateToken) controller.CreateToken();
            else controller.AddAbilityImage();
        }
    }
}
