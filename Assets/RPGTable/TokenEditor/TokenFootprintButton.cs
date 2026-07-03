using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.TokenEditor
{
    [RequireComponent(typeof(Button))]
    public sealed class TokenFootprintButton : MonoBehaviour
    {
        [SerializeField] private TokenEditorController controller;
        [SerializeField] private int size = 1;

        public void Initialize(TokenEditorController tokenEditorController, int footprintSize)
        {
            controller = tokenEditorController;
            size = footprintSize;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => controller?.SetFootprintSize(size));
        }
    }
}
