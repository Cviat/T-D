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

        public void Execute(MainMenuAction action)
        {
            switch (action)
            {
                case MainMenuAction.StartGame:
                case MainMenuAction.ContinueGame:
                    LoadPrototypeTable();
                    break;
                case MainMenuAction.AddContent:
                    Debug.Log("Add Content is a placeholder. Content import will be implemented after the table UI.");
                    break;
                case MainMenuAction.Quit:
                    QuitApplication();
                    break;
            }
        }

        private void LoadPrototypeTable()
        {
            if (Application.CanStreamedLevelBeLoaded(prototypeSceneName))
            {
                SceneManager.LoadScene(prototypeSceneName);
                return;
            }

            Debug.LogWarning($"Scene '{prototypeSceneName}' is not in Build Settings yet.");
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
