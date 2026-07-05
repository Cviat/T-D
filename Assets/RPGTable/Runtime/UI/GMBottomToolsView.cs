using System;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class GMBottomToolsView : MonoBehaviour
    {
        [Header("UI Buttons")]
        public Button playerViewCameraButton;
        public Button drawButton;
        public Button measureButton;

        public void Initialize(Action onToggleCamera, Action onDrawActive, Action onMeasureActive)
        {
            if (playerViewCameraButton != null)
            {
                playerViewCameraButton.onClick.RemoveAllListeners();
                playerViewCameraButton.onClick.AddListener(() => onToggleCamera?.Invoke());
            }

            if (drawButton != null)
            {
                drawButton.onClick.RemoveAllListeners();
                drawButton.onClick.AddListener(() => onDrawActive?.Invoke());
            }

            if (measureButton != null)
            {
                measureButton.onClick.RemoveAllListeners();
                measureButton.onClick.AddListener(() => onMeasureActive?.Invoke());
            }
        }

        public void SetPlayerViewCameraStatus(bool active)
        {
            if (playerViewCameraButton != null)
            {
                playerViewCameraButton.GetComponent<Image>().color = active
                    ? new Color(0.24f, 0.14f, 0.045f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
    }
}
