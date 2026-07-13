using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGTable.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string campaignSelectionSceneName = "CampaignSelection";
        [SerializeField] private string mapEditorSceneName = "MapEditor";
        [SerializeField] private string campaignEditorSceneName = "CampaignEditor";
        [SerializeField] private string tokenEditorSceneName = "TokenEditor";
        [SerializeField] private string characterEditorSceneName = "CharacterEditor";
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject contentMenuPanel;

        private void Awake()
        {
            EnsureCampaignButton();
            EnsureCharacterButton();
            ShowMainMenu();

            // Initialize SoundManager to start background music
            var s = RPGTable.Runtime.SoundManager.Instance;

            if (FindAnyObjectByType<MainMenuPlayerViewManager>() == null)
            {
                var go = new GameObject("MainMenuPlayerViewManager");
                go.AddComponent<MainMenuPlayerViewManager>();
            }
        }

        public void Execute(MainMenuAction action)
        {
            switch (action)
            {
                case MainMenuAction.StartGame:
                case MainMenuAction.ContinueGame:
                    LoadCampaignSelection();
                    break;
                case MainMenuAction.AddContent:
                    ShowContentMenu();
                    break;
                case MainMenuAction.CreateMap:
                    LoadScene(mapEditorSceneName);
                    break;
                case MainMenuAction.CreateCampaign:
                    LoadScene(campaignEditorSceneName);
                    break;
                case MainMenuAction.CreateToken:
                    LoadScene(tokenEditorSceneName);
                    break;
                case MainMenuAction.CreateCharacter:
                    LoadScene(characterEditorSceneName);
                    break;
                case MainMenuAction.BackToMain:
                    ShowMainMenu();
                    break;
                case MainMenuAction.Quit:
                    QuitApplication();
                    break;
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (contentMenuPanel != null)
            {
                contentMenuPanel.SetActive(false);
            }
        }

        private void ShowContentMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (contentMenuPanel != null)
            {
                contentMenuPanel.SetActive(true);
            }
        }

        private void EnsureCampaignButton()
        {
            if (contentMenuPanel == null)
            {
                return;
            }

            var buttons = contentMenuPanel.GetComponentsInChildren<MainMenuButton>(true);
            Transform stack = null;
            var insertIndex = -1;

            foreach (var button in buttons)
            {
                if (button.Action == MainMenuAction.CreateCampaign)
                {
                    return;
                }

                if (button.Action == MainMenuAction.CreateMap)
                {
                    stack = button.transform.parent;
                    insertIndex = button.transform.GetSiblingIndex() + 1;
                }
            }

            if (stack == null)
            {
                stack = contentMenuPanel.transform;
            }

            var stackRect = stack as RectTransform;

            if (stackRect != null && stackRect.sizeDelta.y < 380f)
            {
                stackRect.sizeDelta = new Vector2(stackRect.sizeDelta.x, 380f);
            }

            var root = new GameObject("Create Campaign button", typeof(RectTransform));
            root.transform.SetParent(stack, false);

            if (insertIndex >= 0)
            {
                root.transform.SetSiblingIndex(insertIndex);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(390f, 72f);

            var normal = new Color(0.12f, 0.075f, 0.035f, 0.9f);
            var image = root.AddComponent<Image>();
            image.color = normal;

            var buttonComponent = root.AddComponent<Button>();
            buttonComponent.targetGraphic = image;
            buttonComponent.transition = Selectable.Transition.None;

            var menuButton = root.AddComponent<MainMenuButton>();
            menuButton.Initialize(this, MainMenuAction.CreateCampaign, image, normal, new Color(0.22f, 0.14f, 0.06f, 0.95f));

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 0f);
            textRect.offsetMax = new Vector2(-22f, 0f);

            var text = textObject.AddComponent<Text>();
            text.text = "Создать кампанию";
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private void EnsureCharacterButton()
        {
            if (contentMenuPanel == null)
            {
                return;
            }

            var buttons = contentMenuPanel.GetComponentsInChildren<MainMenuButton>(true);
            Transform stack = null;
            var insertIndex = -1;

            foreach (var button in buttons)
            {
                if (button.Action == MainMenuAction.CreateCharacter)
                {
                    return;
                }

                if (button.Action == MainMenuAction.CreateToken)
                {
                    stack = button.transform.parent;
                    insertIndex = button.transform.GetSiblingIndex() + 1;
                }
            }

            if (stack == null)
            {
                stack = contentMenuPanel.transform;
            }

            var stackRect = stack as RectTransform;

            if (stackRect != null && stackRect.sizeDelta.y < 470f)
            {
                stackRect.sizeDelta = new Vector2(stackRect.sizeDelta.x, 470f);
            }

            CreateRuntimeContentButton(stack, insertIndex, "Create Character button", "Создать персонажа", MainMenuAction.CreateCharacter);
        }

        private void CreateRuntimeContentButton(Transform stack, int insertIndex, string objectName, string label, MainMenuAction action)
        {
            var root = new GameObject(objectName, typeof(RectTransform));
            root.transform.SetParent(stack, false);

            if (insertIndex >= 0)
            {
                root.transform.SetSiblingIndex(insertIndex);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(390f, 72f);

            var normal = new Color(0.12f, 0.075f, 0.035f, 0.9f);
            var image = root.AddComponent<Image>();
            image.color = normal;

            var buttonComponent = root.AddComponent<Button>();
            buttonComponent.targetGraphic = image;
            buttonComponent.transition = Selectable.Transition.None;

            var menuButton = root.AddComponent<MainMenuButton>();
            menuButton.Initialize(this, action, image, normal, new Color(0.22f, 0.14f, 0.06f, 0.95f));

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 0f);
            textRect.offsetMax = new Vector2(-22f, 0f);

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private void LoadCampaignSelection()
        {
            LoadScene(campaignSelectionSceneName);
        }

        private static void LoadScene(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings yet.");
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
