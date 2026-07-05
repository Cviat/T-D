using RPGTable.MapEditor;
using RPGTable.Runtime;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class CharacterEditorController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string tokenEditorSceneName = "TokenEditor";
        [SerializeField] private InputField nameInput;
        [SerializeField] private InputField descriptionInput;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text tokenLabel;

        private string currentCharacterName;
        private string portraitPath;
        private string tokenPath;

        public void Initialize(InputField nameField, InputField descriptionField, Image portrait, Text selectedTokenLabel)
        {
            nameInput = nameField;
            descriptionInput = descriptionField;
            portraitImage = portrait;
            tokenLabel = selectedTokenLabel;
        }

        private void Start()
        {
            ApplyPendingDraft();
            ApplyCreatedToken();
            RefreshPortrait();
            RefreshTokenLabel();
        }

        public void Back()
        {
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public void RequestSave()
        {
            var defaultName = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text)
                ? nameInput.text
                : currentCharacterName;
            MapEditorMapDialog.ShowSave(defaultName, SaveCharacter, "Save Character");
        }

        public void RequestOpen()
        {
            CharacterEditorDialog.ShowOpenCharacter(UserCharacterStore.GetCharacterPaths(), OpenCharacter);
        }

        public void ImportPortrait()
        {
            var path = UserCharacterStore.ImportPortraitWithDialog();

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            portraitPath = path;
            RefreshPortrait();
        }

        public void SelectToken()
        {
            TokenEditorDialog.ShowOpenToken(UserTokenStore.GetTokenPaths(), SetToken);
        }

        public void CreateToken()
        {
            CampaignGameSession.PendingCharacterDraftName = nameInput == null ? currentCharacterName : nameInput.text;
            CampaignGameSession.PendingCharacterDraftDescription = descriptionInput == null ? string.Empty : descriptionInput.text;
            CampaignGameSession.PendingCharacterDraftPortraitPath = portraitPath;
            CampaignGameSession.TokenEditorReturnSceneName = gameObject.scene.name;

            if (Application.CanStreamedLevelBeLoaded(tokenEditorSceneName))
            {
                SceneManager.LoadScene(tokenEditorSceneName);
            }
        }

        private void SaveCharacter(string characterName)
        {
            var data = new SavedCharacterData
            {
                description = descriptionInput == null ? string.Empty : descriptionInput.text,
                portraitPath = portraitPath,
                tokenPath = tokenPath
            };
            var path = UserCharacterStore.SaveCharacter(characterName, data);

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            currentCharacterName = characterName.Trim();

            if (nameInput != null)
            {
                nameInput.text = currentCharacterName;
            }
        }

        private void OpenCharacter(string path)
        {
            var data = UserCharacterStore.LoadCharacter(path);

            if (data == null)
            {
                return;
            }

            currentCharacterName = data.name;
            portraitPath = data.portraitPath;
            tokenPath = data.tokenPath;

            if (nameInput != null)
            {
                nameInput.text = data.name ?? string.Empty;
            }

            if (descriptionInput != null)
            {
                descriptionInput.text = data.description ?? string.Empty;
            }

            RefreshPortrait();
            RefreshTokenLabel();
        }

        private void SetToken(string path)
        {
            tokenPath = path;
            RefreshTokenLabel();
        }

        private void RefreshPortrait()
        {
            if (portraitImage == null)
            {
                return;
            }

            var sprite = UserCharacterStore.LoadPortraitSprite(portraitPath);
            portraitImage.sprite = sprite;
            portraitImage.color = sprite == null ? new Color(0.12f, 0.11f, 0.1f, 1f) : Color.white;
            portraitImage.preserveAspect = true;
        }

        private void RefreshTokenLabel()
        {
            if (tokenLabel == null)
            {
                return;
            }

            tokenLabel.text = string.IsNullOrWhiteSpace(tokenPath)
                ? "Token: not selected"
                : $"Token: {UserTokenStore.GetDisplayName(tokenPath)}";
        }

        private void ApplyCreatedToken()
        {
            if (string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterTokenPath))
            {
                return;
            }

            tokenPath = CampaignGameSession.PendingCharacterTokenPath;
            CampaignGameSession.PendingCharacterTokenPath = null;
        }

        private void ApplyPendingDraft()
        {
            var hasDraft =
                !string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftName) ||
                !string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftDescription) ||
                !string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftPortraitPath);

            if (!hasDraft)
            {
                return;
            }

            currentCharacterName = CampaignGameSession.PendingCharacterDraftName;
            portraitPath = CampaignGameSession.PendingCharacterDraftPortraitPath;

            if (nameInput != null)
            {
                nameInput.text = CampaignGameSession.PendingCharacterDraftName ?? string.Empty;
            }

            if (descriptionInput != null)
            {
                descriptionInput.text = CampaignGameSession.PendingCharacterDraftDescription ?? string.Empty;
            }

            CampaignGameSession.PendingCharacterDraftName = null;
            CampaignGameSession.PendingCharacterDraftDescription = null;
            CampaignGameSession.PendingCharacterDraftPortraitPath = null;
        }
    }
}
