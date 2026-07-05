using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private MainMenuAction action;
        [SerializeField] private MainMenuController controller;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.white;

        private Button button;

        public MainMenuAction Action => action;

        public void Initialize(
            MainMenuController menuController,
            MainMenuAction menuAction,
            Graphic graphic,
            Color normal,
            Color hover)
        {
            controller = menuController;
            action = menuAction;
            targetGraphic = graphic;
            normalColor = normal;
            hoverColor = hover;
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Execute);

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (controller == null)
            {
                controller = FindFirstObjectByType<MainMenuController>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = normalColor;
            }
        }

        private void Execute()
        {
            if (controller == null)
            {
                Debug.LogWarning("Main menu button has no controller.");
                return;
            }

            controller.Execute(action);
        }
    }
}
