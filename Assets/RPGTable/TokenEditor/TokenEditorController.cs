using System.Collections.Generic;
using RPGTable.MapEditor;
using RPGTable.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGTable.TokenEditor
{
    public sealed class TokenEditorController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private Image frameImage;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text portraitPlusText;
        [SerializeField] private Text footprintLabel;
        [SerializeField] private InputField nameInput;

        private string currentTokenName;
        private string framePath;
        private string portraitPath;
        private int footprintSize = 1;

        public void Initialize(
            Image tokenFrame,
            Image tokenPortrait,
            Text plusText,
            Text sizeLabel,
            InputField tokenNameInput)
        {
            frameImage = tokenFrame;
            portraitImage = tokenPortrait;
            portraitPlusText = plusText;
            footprintLabel = sizeLabel;
            nameInput = tokenNameInput;
        }

        private void Start()
        {
            ApplyPendingPlayerDefaults();
            RefreshPortrait();
            RefreshFootprintLabel();
        }

        public void BackToMainMenu()
        {
            CampaignGameSession.PendingTokenPlayerId = null;

            if (TryReturnToSelection())
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public void SetFrame(string path, Sprite sprite)
        {
            framePath = path;

            if (frameImage != null)
            {
                frameImage.sprite = sprite;
                frameImage.color = sprite == null ? new Color(0.2f, 0.16f, 0.12f, 1f) : Color.white;
                frameImage.preserveAspect = true;
            }
        }

        public void SetFootprintSize(int size)
        {
            footprintSize = Mathf.Clamp(size, 1, 5);
            RefreshFootprintLabel();
        }

        public void ImportPortrait()
        {
            var path = UserTokenStore.ImportImageWithDialog("Import token portrait");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            portraitPath = path;
            RefreshPortrait();
        }

        public void RequestSaveToken()
        {
            var defaultName = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text)
                ? nameInput.text
                : currentTokenName;
            MapEditorMapDialog.ShowSave(defaultName, SaveToken, "Save Token");
        }

        public void RequestOpenToken()
        {
            TokenEditorDialog.ShowOpenToken(UserTokenStore.GetTokenPaths(), OpenToken);
        }

        private void SaveToken(string tokenName)
        {
            var data = new SavedTokenData
            {
                name = tokenName,
                framePath = framePath,
                portraitPath = portraitPath,
                footprintSize = footprintSize,
                hasPortraitMaskLayout = TryReadPortraitMaskLayout(out var maskPositionRatio, out var maskSizeRatio),
                portraitMaskPositionRatio = maskPositionRatio,
                portraitMaskSizeRatio = maskSizeRatio
            };

            var path = UserTokenStore.SaveToken(tokenName, data);

            if (!string.IsNullOrWhiteSpace(path))
            {
                currentTokenName = tokenName.Trim();

                if (nameInput != null)
                {
                    nameInput.text = currentTokenName;
                }

                if (!string.IsNullOrWhiteSpace(CampaignGameSession.PendingTokenPlayerId))
                {
                    CampaignGameSession.AssignTokenToPendingPlayer(path);
                    TryReturnToSelection();
                }
                else if (!string.IsNullOrWhiteSpace(CampaignGameSession.TokenEditorReturnSceneName))
                {
                    CampaignGameSession.PendingCharacterTokenPath = path;
                    TryReturnToSelection();
                }
            }
        }

        private void OpenToken(string path)
        {
            var data = UserTokenStore.LoadToken(path);

            if (data == null)
            {
                return;
            }

            currentTokenName = data.name;
            framePath = data.framePath;
            portraitPath = data.portraitPath;
            footprintSize = Mathf.Clamp(data.footprintSize <= 0 ? 1 : data.footprintSize, 1, 5);

            if (nameInput != null)
            {
                nameInput.text = data.name ?? string.Empty;
            }

            SetFrame(framePath, LoadAssetSprite(framePath));
            ApplyPortraitMaskLayout(data);
            RefreshPortrait();
            RefreshFootprintLabel();
        }

        private void RefreshPortrait()
        {
            if (portraitImage == null)
            {
                return;
            }

            var sprite = UserTokenStore.LoadSprite(portraitPath);
            portraitImage.sprite = sprite;
            portraitImage.color = sprite == null ? new Color(0.12f, 0.11f, 0.1f, 1f) : Color.white;
            portraitImage.preserveAspect = true;

            if (portraitPlusText != null)
            {
                portraitPlusText.gameObject.SetActive(sprite == null);
            }
        }

        private void ApplyPendingPlayerDefaults()
        {
            var pendingPlayer = CampaignGameSession.FindPlayer(CampaignGameSession.PendingTokenPlayerId);

            if (pendingPlayer == null)
            {
                return;
            }

            if (nameInput != null && string.IsNullOrWhiteSpace(nameInput.text))
            {
                nameInput.text = pendingPlayer.name;
            }
        }

        private bool TryReturnToSelection()
        {
            var returnSceneName = CampaignGameSession.TokenEditorReturnSceneName;

            if (string.IsNullOrWhiteSpace(returnSceneName) || !Application.CanStreamedLevelBeLoaded(returnSceneName))
            {
                return false;
            }

            CampaignGameSession.TokenEditorReturnSceneName = null;
            SceneManager.LoadScene(returnSceneName);
            return true;
        }

        private void RefreshFootprintLabel()
        {
            if (footprintLabel != null)
            {
                footprintLabel.text = $"{footprintSize}x{footprintSize}";
            }
        }

        private bool TryReadPortraitMaskLayout(out Vector2 positionRatio, out Vector2 sizeRatio)
        {
            positionRatio = Vector2.zero;
            sizeRatio = Vector2.zero;

            var maskRect = portraitImage == null ? null : portraitImage.transform.parent as RectTransform;
            var rootRect = maskRect == null ? null : maskRect.parent as RectTransform;

            if (maskRect == null || rootRect == null)
            {
                return false;
            }

            var rootSize = rootRect.rect.size;

            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f)
            {
                rootSize = rootRect.sizeDelta;
            }

            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f)
            {
                return false;
            }

            positionRatio = new Vector2(maskRect.anchoredPosition.x / rootSize.x, maskRect.anchoredPosition.y / rootSize.y);
            sizeRatio = new Vector2(maskRect.sizeDelta.x / rootSize.x, maskRect.sizeDelta.y / rootSize.y);
            return true;
        }

        private void ApplyPortraitMaskLayout(SavedTokenData data)
        {
            if (data == null || !data.hasPortraitMaskLayout)
            {
                return;
            }

            var maskRect = portraitImage == null ? null : portraitImage.transform.parent as RectTransform;
            var rootRect = maskRect == null ? null : maskRect.parent as RectTransform;

            if (maskRect == null || rootRect == null)
            {
                return;
            }

            var rootSize = rootRect.rect.size;

            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f)
            {
                rootSize = rootRect.sizeDelta;
            }

            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f)
            {
                return;
            }

            maskRect.anchoredPosition = new Vector2(
                data.portraitMaskPositionRatio.x * rootSize.x,
                data.portraitMaskPositionRatio.y * rootSize.y);
            maskRect.sizeDelta = new Vector2(
                data.portraitMaskSizeRatio.x * rootSize.x,
                data.portraitMaskSizeRatio.y * rootSize.y);
        }

        private static Sprite LoadAssetSprite(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
