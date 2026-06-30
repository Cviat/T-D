using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGTable.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string prototypeSceneName = "RPGTablePrototype";
        [SerializeField] private string mapEditorSceneName = "MapEditor";
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject contentMenuPanel;

        private void Awake()
        {
            ShowMainMenu();
        }

        public void Execute(MainMenuAction action)
        {
            switch (action)
            {
                case MainMenuAction.StartGame:
                case MainMenuAction.ContinueGame:
                    LoadPrototypeTable();
                    break;
                case MainMenuAction.AddContent:
                    ShowContentMenu();
                    break;
                case MainMenuAction.CreateMap:
                    LoadScene(mapEditorSceneName);
                    break;
                case MainMenuAction.CreateToken:
                    Debug.Log("Create Token selected. Token editor screen is the next implementation step.");
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

        private void LoadPrototypeTable()
        {
            LoadScene(prototypeSceneName);
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
