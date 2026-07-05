using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.TokenEditor
{
    [RequireComponent(typeof(Button))]
    public sealed class TokenFrameOptionButton : MonoBehaviour
    {
        [SerializeField] private TokenEditorController controller;
        [SerializeField] private string framePath;
        [SerializeField] private Image previewImage;

        public void Initialize(TokenEditorController tokenEditorController, string path, Image preview)
        {
            controller = tokenEditorController;
            framePath = path;
            previewImage = preview;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Click);
        }

        private void Click()
        {
            if (controller == null || previewImage == null)
            {
                return;
            }

            controller.SetFrame(framePath, previewImage.sprite);
        }
    }
}
