using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RPGTable.MapEditor
{
    public sealed class MapEditorElementPalette : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private MapEditorElementSpawner spawner;

        public void ImportImage()
        {
            var importedPath = UserElementAssetStore.ImportImageWithDialog();

            if (!string.IsNullOrWhiteSpace(importedPath))
            {
                Reload();
            }
        }

        public void Reload()
        {
            Clear();

            var grid = contentRoot != null ? contentRoot.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                if (CampaignEditSession.IsEditingPresetTokens)
                {
                    grid.cellSize = new Vector2(240f, 64f);
                }
                else
                {
                    grid.cellSize = new Vector2(96f, 96f);
                }
            }

            if (CampaignEditSession.IsEditingPresetTokens)
            {
                foreach (var path in RPGTable.CharacterEditor.UserCharacterStore.GetCharacterPaths())
                {
                    var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(path);
                    if (charData == null) continue;

                    var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(charData.tokenPath);
                    if (tokenData == null) continue;

                    var portrait = RPGTable.TokenEditor.UserTokenStore.LoadSprite(tokenData.portraitPath);
                    if (portrait != null)
                    {
                        CreateCharacterPaletteItem(path, charData.name, portrait, charData.maxHp);
                    }
                }
            }
            else
            {
                foreach (var path in UserElementAssetStore.GetImagePaths())
                {
                    var sprite = UserElementAssetStore.LoadSprite(path);

                    if (sprite != null)
                    {
                        CreateItem(path, sprite);
                    }
                }
            }
        }

        private void Start()
        {
            EnsureMapControls();
            if (CampaignEditSession.IsEditingPresetTokens)
            {
                var mapController = GetComponent<MapEditorMapController>();
                if (mapController != null)
                {
                    mapController.OpenMapPresetTokens();
                }
                Reload();
            }
            else
            {
                Reload();
            }
        }

        private void EnsureMapControls()
        {
            var mapController = GetComponent<MapEditorMapController>();

            if (mapController == null)
            {
                mapController = gameObject.AddComponent<MapEditorMapController>();
            }

            mapController.Initialize(spawner);

            if (CampaignEditSession.IsEditingPresetTokens)
            {
                EnsureMapButton("Back Button", "Назад", new Vector2(0f, -18f), mapController, MapEditorMapButtonAction.Back);
                EnsureMapButton("Save Map Button", "Применить", new Vector2(0f, -76f), mapController, MapEditorMapButtonAction.Save);
                
                var openBtn = transform.Find("Open Map Button");
                if (openBtn != null) openBtn.gameObject.SetActive(false);
                var exitBtn = transform.Find("Add Exit Button");
                if (exitBtn != null) exitBtn.gameObject.SetActive(false);
                var spawnBtn = transform.Find("Add Spawn Button");
                if (spawnBtn != null) spawnBtn.gameObject.SetActive(false);
                var importBtn = transform.Find("Import Image Button");
                if (importBtn != null) importBtn.gameObject.SetActive(false);

                if (contentRoot != null)
                {
                    contentRoot.offsetMax = new Vector2(-18f, -140f);
                }
            }
            else
            {
                EnsureMapButton("Back Button", "Back", new Vector2(0f, -18f), mapController, MapEditorMapButtonAction.Back);
                EnsureMapButton("Save Map Button", "Save Map", new Vector2(0f, -76f), mapController, MapEditorMapButtonAction.Save);
                EnsureMapButton("Open Map Button", "Import Map", new Vector2(0f, -134f), mapController, MapEditorMapButtonAction.Open);
                EnsureMapButton("Add Exit Button", "Add Exit", new Vector2(0f, -192f), mapController, MapEditorMapButtonAction.AddExit);
                EnsureMapButton("Add Spawn Button", "Add Spawn", new Vector2(0f, -250f), mapController, MapEditorMapButtonAction.AddSpawn);

                var openBtn = transform.Find("Open Map Button");
                if (openBtn != null) openBtn.gameObject.SetActive(true);
                var exitBtn = transform.Find("Add Exit Button");
                if (exitBtn != null) exitBtn.gameObject.SetActive(true);
                var spawnBtn = transform.Find("Add Spawn Button");
                if (spawnBtn != null) spawnBtn.gameObject.SetActive(true);
                var importBtn = transform.Find("Import Image Button") as RectTransform;
                if (importBtn != null)
                {
                    importBtn.gameObject.SetActive(true);
                    importBtn.sizeDelta = new Vector2(240f, 48f);
                    importBtn.anchorMin = new Vector2(0.5f, 1f);
                    importBtn.anchorMax = new Vector2(0.5f, 1f);
                    importBtn.pivot = new Vector2(0.5f, 1f);
                    importBtn.anchoredPosition = new Vector2(0f, -308f);
                }

                if (contentRoot != null)
                {
                    contentRoot.offsetMax = new Vector2(-18f, -374f);
                }
            }
        }

        private void EnsureMapButton(
            string buttonName,
            string label,
            Vector2 anchoredPosition,
            MapEditorMapController mapController,
            MapEditorMapButtonAction action)
        {
            var existing = transform.Find(buttonName);
            if (existing != null)
            {
                var existingBinder = existing.GetComponent<MapEditorMapButton>();
                if (existingBinder == null)
                {
                    existingBinder = existing.gameObject.AddComponent<MapEditorMapButton>();
                }
                existingBinder.Initialize(mapController, action);
                return;
            }

            var buttonObject = CreateRuntimeTextButton(buttonName, label);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;

            var binder = buttonObject.AddComponent<MapEditorMapButton>();
            binder.Initialize(mapController, action);
        }

        private GameObject CreateRuntimeTextButton(string buttonName, string label)
        {
            var buttonObject = new GameObject(buttonName, typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 48f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            return buttonObject;
        }

        private void CreateItem(string path, Sprite sprite)
        {
            var item = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(RectTransform));
            item.transform.SetParent(contentRoot, false);

            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;

            var paletteItem = item.AddComponent<MapEditorPaletteItem>();
            paletteItem.Initialize(path, sprite, spawner, this);
        }

        private void CreateCharacterPaletteItem(string path, string charName, Sprite portrait, int maxHp)
        {
            var cardPrefab = Resources.Load<GameObject>("Prefabs/TokenCard");
            if (cardPrefab != null && contentRoot != null)
            {
                var item = UnityEngine.Object.Instantiate(cardPrefab, contentRoot);
                var cardView = item.GetComponent<RPGTable.Runtime.TokenCardView>();
                if (cardView != null)
                {
                    cardView.Setup(charName, portrait, maxHp, maxHp, false, () => spawner.BeginCharacterDrag(path, portrait));
                }
            }
        }

        private void Clear()
        {
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}
