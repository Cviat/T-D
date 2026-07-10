using System;
using System.Collections.Generic;
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

        [SerializeField] private InputField maxHpInput;
        [SerializeField] private AbilityDropSlot[] attackSlots;
        [SerializeField] private AbilityDropSlot[] attack2Slots;
        [SerializeField] private AbilityDropSlot[] defenseSlots;

        [SerializeField] private RectTransform abilitiesRoot;

        // New stats inputs & fields
        [SerializeField] private InputField classInput;
        [SerializeField] private InputField levelInput;
        [SerializeField] private InputField xpInput;
        [SerializeField] private InputField maxArmorInput;

        [SerializeField] private Text strengthValueLabel;
        [SerializeField] private Text agilityValueLabel;
        [SerializeField] private Text intelligenceValueLabel;
        [SerializeField] private Text holinessValueLabel;

        [SerializeField] private Text weaponNameLabel;
        [SerializeField] private Text weaponScalingLabel;
        [SerializeField] private Text weaponAttributesLabel;
        [SerializeField] private Text weapon2NameLabel;
        [SerializeField] private Text weapon2ScalingLabel;
        [SerializeField] private Text weapon2AttributesLabel;

        // Tab UI Fields
        [SerializeField] private Button characterTabButton;
        [SerializeField] private Button statsTabButton;
        [SerializeField] private GameObject characterTabPanel;
        [SerializeField] private GameObject statsTabPanel;

        [SerializeField] private Image tokenPortraitPreview;
        [SerializeField] private Image tokenFramePreview;

        // Saved button references for runtime listener setup
        [SerializeField] private Button strPlusBtn;
        [SerializeField] private Button strMinusBtn;
        [SerializeField] private Button agiPlusBtn;
        [SerializeField] private Button agiMinusBtn;
        [SerializeField] private Button intPlusBtn;
        [SerializeField] private Button intMinusBtn;
        [SerializeField] private Button holPlusBtn;
        [SerializeField] private Button holMinusBtn;


        // Equipment & Inventory Slots
        [SerializeField] private InputField eqHelmetInput;
        [SerializeField] private InputField eqArmorInput;
        [SerializeField] private InputField eqWeaponInput;
        [SerializeField] private InputField eqWeapon2Input;
        [SerializeField] private InputField eqShieldInput;
        [SerializeField] private InputField eqBootsInput;
        [SerializeField] private InputField eqAmuletInput;
        [SerializeField] private InputField eqRingInput;
        [SerializeField] private InputField eqArtifactInput;
        [SerializeField] private InputField eqBeltInput;
        [SerializeField] private InputField[] backpackInputSlots;

        // Items root for drag-and-drop
        [SerializeField] private RectTransform itemsRoot;

        [SerializeField] private Sprite dialogFrameSprite;
        [SerializeField] private Sprite dialogBgSprite;

        public Sprite DialogFrameSprite => dialogFrameSprite;
        public Sprite DialogBgSprite => dialogBgSprite;

        private string currentCharacterName;
        private string portraitPath;
        private string tokenPath;

        private int strength = 10;
        private int agility = 10;
        private int intelligence = 10;
        private int holiness = 10;

        private int customAbilityCount = 1;

        public void Initialize(
            InputField nameField,
            InputField descriptionField,
            Image portrait,
            Text selectedTokenLabel,
            InputField hpField,
            AbilityDropSlot[] attackInputs,
            AbilityDropSlot[] attack2Inputs,
            AbilityDropSlot[] defenseInputs,
            RectTransform abilities,

            InputField classField,
            InputField levelField,
            InputField xpField,
            InputField armorField,

            Text strVal, Text agiVal, Text intVal, Text holVal,
            Button strPlus, Button strMinus,
            Button agiPlus, Button agiMinus,
            Button intPlus, Button intMinus,
            Button holPlus, Button holMinus,

            Text weaponNameText,
            Text weaponScalingText,
            Text weaponAttributesText,

            Text weapon2NameText,
            Text weapon2ScalingText,
            Text weapon2AttributesText,

            Button charTabBtn, Button stTabBtn,
            GameObject charTabPanelObj, GameObject stTabPanelObj,
            Image tkPortraitPreview, Image tkFramePreview,

            InputField helmetField, InputField armorEquipField, InputField weaponEquipField, InputField weapon2EquipField, InputField shieldField,
            InputField bootsField, InputField amuletField, InputField ringField, InputField artifactField,
            InputField beltField, InputField[] backpackFields,
            RectTransform items,
            Sprite dialFrame, Sprite dialBg)
        {
            nameInput = nameField;
            descriptionInput = descriptionField;
            portraitImage = portrait;
            tokenLabel = selectedTokenLabel;
            maxHpInput = hpField;
            attackSlots = attackInputs;
            attack2Slots = attack2Inputs;
            defenseSlots = defenseInputs;
            abilitiesRoot = abilities;

            classInput = classField;
            levelInput = levelField;
            xpInput = xpField;
            maxArmorInput = armorField;

            strengthValueLabel = strVal;
            agilityValueLabel = agiVal;
            intelligenceValueLabel = intVal;
            holinessValueLabel = holVal;

            weaponNameLabel = weaponNameText;
            weaponScalingLabel = weaponScalingText;
            weaponAttributesLabel = weaponAttributesText;
            weapon2NameLabel = weapon2NameText;
            weapon2ScalingLabel = weapon2ScalingText;
            weapon2AttributesLabel = weapon2AttributesText;

            characterTabButton = charTabBtn;
            statsTabButton = stTabBtn;
            characterTabPanel = charTabPanelObj;
            statsTabPanel = stTabPanelObj;

            tokenPortraitPreview = tkPortraitPreview;
            tokenFramePreview = tkFramePreview;

            strPlusBtn = strPlus;
            strMinusBtn = strMinus;
            agiPlusBtn = agiPlus;
            agiMinusBtn = agiMinus;
            intPlusBtn = intPlus;
            intMinusBtn = intMinus;
            holPlusBtn = holPlus;
            holMinusBtn = holMinus;

            eqHelmetInput = helmetField;
            eqArmorInput = armorEquipField;
            eqWeaponInput = weaponEquipField;
            eqWeapon2Input = weapon2EquipField;
            eqShieldInput = shieldField;
            eqBootsInput = bootsField;
            eqAmuletInput = amuletField;
            eqRingInput = ringField;
            eqArtifactInput = artifactField;
            eqBeltInput = beltField;
            backpackInputSlots = backpackFields;
            itemsRoot = items;
            dialogFrameSprite = dialFrame;
            dialogBgSprite = dialBg;
        }

        private void Start()
        {
            ApplyPendingDraft();
            ApplyCreatedToken();
            RefreshPortrait();
            RefreshTokenPreview();
            UpdateStatLabels();
            ReloadAvailableAbilities();
            ReloadAvailableItems();
            RecalculateEquippedBonuses();
            SwitchTab(true);

            if (strPlusBtn != null) strPlusBtn.onClick.AddListener(() => ModifyStat("STR", 1));
            if (strMinusBtn != null) strMinusBtn.onClick.AddListener(() => ModifyStat("STR", -1));
            if (agiPlusBtn != null) agiPlusBtn.onClick.AddListener(() => ModifyStat("AGI", 1));
            if (agiMinusBtn != null) agiMinusBtn.onClick.AddListener(() => ModifyStat("AGI", -1));
            if (intPlusBtn != null) intPlusBtn.onClick.AddListener(() => ModifyStat("INT", 1));
            if (intMinusBtn != null) intMinusBtn.onClick.AddListener(() => ModifyStat("INT", -1));
            if (holPlusBtn != null) holPlusBtn.onClick.AddListener(() => ModifyStat("HOL", 1));
            if (holMinusBtn != null) holMinusBtn.onClick.AddListener(() => ModifyStat("HOL", -1));



            if (characterTabButton != null) characterTabButton.onClick.AddListener(() => SwitchTab(true));
            if (statsTabButton != null) statsTabButton.onClick.AddListener(() => SwitchTab(false));

            // Set up dynamic equipment listeners
            if (eqHelmetInput != null) eqHelmetInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqArmorInput != null) eqArmorInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqWeaponInput != null) eqWeaponInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqWeapon2Input != null) eqWeapon2Input.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqShieldInput != null) eqShieldInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqBootsInput != null) eqBootsInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqAmuletInput != null) eqAmuletInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqRingInput != null) eqRingInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqArtifactInput != null) eqArtifactInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqBeltInput != null) eqBeltInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
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

        public void AddAbilityImage()
        {
            var name = "Свой прием " + customAbilityCount++;
            CreateAbilityCard(name);
        }

        private void ModifyStat(string statName, int delta)
        {
            if (statName == "STR") strength = Mathf.Max(0, strength + delta);
            else if (statName == "AGI") agility = Mathf.Max(0, agility + delta);
            else if (statName == "INT") intelligence = Mathf.Max(0, intelligence + delta);
            else if (statName == "HOL") holiness = Mathf.Max(0, holiness + delta);
            UpdateStatLabels();
            RecalculateEquippedBonuses();
        }



        private void UpdateStatLabels()
        {
            if (strengthValueLabel != null) strengthValueLabel.text = strength.ToString();
            if (agilityValueLabel != null) agilityValueLabel.text = agility.ToString();
            if (intelligenceValueLabel != null) intelligenceValueLabel.text = intelligence.ToString();
            if (holinessValueLabel != null) holinessValueLabel.text = holiness.ToString();
        }

        private void SwitchTab(bool isCharacter)
        {
            if (characterTabPanel != null) characterTabPanel.SetActive(isCharacter);
            if (statsTabPanel != null) statsTabPanel.SetActive(!isCharacter);

            if (characterTabButton != null)
            {
                characterTabButton.GetComponent<Image>().color = isCharacter
                    ? Color.white
                    : new Color(0.55f, 0.5f, 0.45f, 1f);
            }
            if (statsTabButton != null)
            {
                statsTabButton.GetComponent<Image>().color = !isCharacter
                    ? Color.white
                    : new Color(0.55f, 0.5f, 0.45f, 1f);
            }
        }

        private void RefreshTokenPreview()
        {
            if (string.IsNullOrWhiteSpace(tokenPath))
            {
                if (tokenPortraitPreview != null) tokenPortraitPreview.gameObject.SetActive(false);
                if (tokenFramePreview != null) tokenFramePreview.gameObject.SetActive(false);
                if (tokenLabel != null) tokenLabel.text = "Фишка не выбрана";
                return;
            }

            if (tokenLabel != null) tokenLabel.text = "Фишка выбрана";

            var tokenData = UserTokenStore.LoadToken(tokenPath);
            if (tokenData != null)
            {
                var portraitSprite = UserTokenStore.LoadSprite(tokenData.portraitPath);
                var frameSprite = UserTokenStore.LoadSprite(tokenData.framePath);

                if (tokenPortraitPreview != null)
                {
                    tokenPortraitPreview.sprite = portraitSprite;
                    tokenPortraitPreview.gameObject.SetActive(portraitSprite != null);
                    tokenPortraitPreview.color = Color.white;

                    if (tokenData.hasPortraitMaskLayout)
                    {
                        var maskRect = tokenPortraitPreview.transform.parent as RectTransform;
                        var rootRect = maskRect != null ? maskRect.parent as RectTransform : null;
                        if (maskRect != null && rootRect != null)
                        {
                            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
                            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
                            maskRect.pivot = new Vector2(0.5f, 0.5f);

                            var rootSize = rootRect.rect.size;
                            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f) rootSize = rootRect.sizeDelta;
                            if (rootSize.x > 0.01f && rootSize.y > 0.01f)
                            {
                                maskRect.anchoredPosition = new Vector2(
                                    tokenData.portraitMaskPositionRatio.x * rootSize.x,
                                    tokenData.portraitMaskPositionRatio.y * rootSize.y);
                                maskRect.sizeDelta = new Vector2(
                                    tokenData.portraitMaskSizeRatio.x * rootSize.x,
                                    tokenData.portraitMaskSizeRatio.y * rootSize.y);
                            }
                        }
                    }
                    else
                    {
                        var maskRect = tokenPortraitPreview.transform.parent as RectTransform;
                        if (maskRect != null)
                        {
                            maskRect.anchorMin = Vector2.zero;
                            maskRect.anchorMax = Vector2.one;
                            maskRect.pivot = new Vector2(0.5f, 0.5f);
                            maskRect.offsetMin = Vector2.zero;
                            maskRect.offsetMax = Vector2.zero;
                            maskRect.anchoredPosition = Vector2.zero;
                            maskRect.sizeDelta = Vector2.zero;
                        }
                    }
                }

                if (tokenFramePreview != null)
                {
                    tokenFramePreview.sprite = frameSprite;
                    tokenFramePreview.gameObject.SetActive(frameSprite != null);
                    tokenFramePreview.color = Color.white;
                }
            }
            else
            {
                if (tokenPortraitPreview != null) tokenPortraitPreview.gameObject.SetActive(false);
                if (tokenFramePreview != null) tokenFramePreview.gameObject.SetActive(false);
            }
        }

        private void SaveCharacter(string characterName)
        {
            int maxHpVal = 10;
            if (maxHpInput != null && int.TryParse(maxHpInput.text, out var hp))
            {
                maxHpVal = hp;
            }

            int lvlVal = 1;
            if (levelInput != null && int.TryParse(levelInput.text, out var lvl))
            {
                lvlVal = lvl;
            }

            int xpVal = 0;
            if (xpInput != null && int.TryParse(xpInput.text, out var xp))
            {
                xpVal = xp;
            }

            int armorVal = 0;
            if (maxArmorInput != null && int.TryParse(maxArmorInput.text, out var arm))
            {
                armorVal = arm;
            }

            var weapon1Card = FindItemCard(eqWeaponInput != null ? eqWeaponInput.text : "");
            var weapon2Card = FindItemCard(eqWeapon2Input != null ? eqWeapon2Input.text : "");

            bool calcMelee = (weapon1Card == null && weapon2Card == null)
                || (weapon1Card != null && weapon1Card.attackType == RPGTable.Core.AttackType.Melee)
                || (weapon2Card != null && weapon2Card.attackType == RPGTable.Core.AttackType.Melee);
            bool calcRanged = (weapon1Card != null && weapon1Card.attackType == RPGTable.Core.AttackType.Ranged)
                || (weapon2Card != null && weapon2Card.attackType == RPGTable.Core.AttackType.Ranged);
            bool calcMagic = (weapon1Card != null && weapon1Card.attackType == RPGTable.Core.AttackType.Magic)
                || (weapon2Card != null && weapon2Card.attackType == RPGTable.Core.AttackType.Magic);

            var data = new SavedCharacterData
            {
                description = descriptionInput == null ? string.Empty : descriptionInput.text,
                portraitPath = portraitPath,
                tokenPath = tokenPath,
                maxHp = maxHpVal,
                attackSlots = ReadSlots(attackSlots),
                attack2Slots = ReadSlots(attack2Slots),
                defenseSlots = ReadSlots(defenseSlots),
                abilityImagePaths = Array.Empty<string>(),

                // Save Stats & Level
                level = lvlVal,
                xp = xpVal,
                characterClass = classInput == null ? "Воин" : classInput.text,
                strength = strength,
                agility = agility,
                intelligence = intelligence,
                holiness = holiness,

                // Save Armor
                maxArmor = armorVal,

                // Save Equipment Slots
                eqHelmet = eqHelmetInput == null ? "" : eqHelmetInput.text,
                eqArmor = eqArmorInput == null ? "" : eqArmorInput.text,
                eqWeapon = eqWeaponInput == null ? "" : eqWeaponInput.text,
                eqWeapon2 = eqWeapon2Input == null ? "" : eqWeapon2Input.text,
                eqShield = eqShieldInput == null ? "" : eqShieldInput.text,
                eqBoots = eqBootsInput == null ? "" : eqBootsInput.text,
                eqAmulet = eqAmuletInput == null ? "" : eqAmuletInput.text,
                eqRing = eqRingInput == null ? "" : eqRingInput.text,
                eqArtifact = eqArtifactInput == null ? "" : eqArtifactInput.text,
                eqBelt = eqBeltInput == null ? "" : eqBeltInput.text,

                // Save Backpack (8 slots)
                backpackSlots = ReadBackpack(backpackInputSlots)
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

            if (maxHpInput != null)
            {
                maxHpInput.text = data.maxHp <= 0 ? "10" : data.maxHp.ToString();
            }

            // Load Level & Stats
            if (classInput != null) classInput.text = data.characterClass ?? "Воин";
            if (levelInput != null) levelInput.text = data.level.ToString();
            if (xpInput != null) xpInput.text = data.xp.ToString();
            if (maxArmorInput != null) maxArmorInput.text = data.maxArmor.ToString();

            strength = data.strength;
            agility = data.agility;
            intelligence = data.intelligence;
            holiness = data.holiness;
            UpdateStatLabels();

            // Update read-only weapon scaling labels
            UpdateWeaponStatsDisplay(FindItemCard(data.eqWeapon), FindItemCard(data.eqWeapon2));

            // Load Equipment Slots
            if (eqHelmetInput != null) eqHelmetInput.text = data.eqHelmet ?? "";
            if (eqArmorInput != null) eqArmorInput.text = data.eqArmor ?? "";
            if (eqWeaponInput != null) eqWeaponInput.text = data.eqWeapon ?? "";
            if (eqWeapon2Input != null) eqWeapon2Input.text = data.eqWeapon2 ?? "";
            if (eqShieldInput != null) eqShieldInput.text = data.eqShield ?? "";
            if (eqBootsInput != null) eqBootsInput.text = data.eqBoots ?? "";
            if (eqAmuletInput != null) eqAmuletInput.text = data.eqAmulet ?? "";
            if (eqRingInput != null) eqRingInput.text = data.eqRing ?? "";
            if (eqArtifactInput != null) eqArtifactInput.text = data.eqArtifact ?? "";
            if (eqBeltInput != null) eqBeltInput.text = data.eqBelt ?? "";

            // Load Backpack Slots
            WriteBackpack(backpackInputSlots, data.backpackSlots);

            WriteSlots(attackSlots, data.attackSlots);
            WriteSlots(attack2Slots, data.attack2Slots);
            WriteSlots(defenseSlots, data.defenseSlots);



            RefreshPortrait();
            RefreshTokenPreview();
            RecalculateEquippedBonuses();
        }

        private void SetToken(string path)
        {
            tokenPath = path;
            RefreshTokenPreview();
        }

        private void RefreshPortrait()
        {
            if (portraitImage == null)
            {
                return;
            }

            var sprite = UserCharacterStore.LoadSprite(portraitPath);
            portraitImage.sprite = sprite;
            portraitImage.color = sprite == null ? new Color(0.12f, 0.11f, 0.1f, 1f) : Color.white;
        }

        private void ApplyPendingDraft()
        {
            if (!string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftName))
            {
                currentCharacterName = CampaignGameSession.PendingCharacterDraftName;

                if (nameInput != null)
                {
                    nameInput.text = currentCharacterName;
                }

                CampaignGameSession.PendingCharacterDraftName = null;
            }

            if (!string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftDescription))
            {
                if (descriptionInput != null)
                {
                    descriptionInput.text = CampaignGameSession.PendingCharacterDraftDescription;
                }

                CampaignGameSession.PendingCharacterDraftDescription = null;
            }

            if (!string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterDraftPortraitPath))
            {
                portraitPath = CampaignGameSession.PendingCharacterDraftPortraitPath;
                CampaignGameSession.PendingCharacterDraftPortraitPath = null;
            }
        }

        private void ApplyCreatedToken()
        {
            if (!string.IsNullOrWhiteSpace(CampaignGameSession.PendingCharacterTokenPath))
            {
                tokenPath = CampaignGameSession.PendingCharacterTokenPath;
                CampaignGameSession.PendingCharacterTokenPath = null;
            }
        }

        private void ReloadAvailableAbilities()
        {
            if (abilitiesRoot != null)
            {
                for (var i = abilitiesRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(abilitiesRoot.GetChild(i).gameObject);
                }
            }

            var weapon1Card = FindItemCard(eqWeaponInput != null ? eqWeaponInput.text : "");
            var weapon2Card = FindItemCard(eqWeapon2Input != null ? eqWeapon2Input.text : "");

            var allowedTypes = new System.Collections.Generic.HashSet<RPGTable.Core.AttackType>();
            allowedTypes.Add(RPGTable.Core.AttackType.Defense);

            if (weapon1Card != null)
            {
                allowedTypes.Add(weapon1Card.attackType);
            }
            if (weapon2Card != null)
            {
                allowedTypes.Add(weapon2Card.attackType);
            }

            var cards = Resources.LoadAll<RPGTable.Core.AbilityCard>("AbilityCards");
            var matchingCards = new System.Collections.Generic.List<RPGTable.Core.AbilityCard>();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.title))
                    {
                        if (allowedTypes.Contains(card.attackType))
                        {
                            matchingCards.Add(card);
                        }
                    }
                }
            }

            // Create cards for all available abilities
            foreach (var card in matchingCards)
            {
                CreateAbilityCard(card.title);
            }

            // Pad the remaining slots to exactly 60 cells (20x3 grid)
            var slotFrame = FindSlotFrameSprite();
            int remaining = 60 - matchingCards.Count;
            for (var i = 0; i < remaining; i++)
            {
                var emptyCell = new GameObject("EmptySlot", typeof(RectTransform));
                emptyCell.transform.SetParent(abilitiesRoot, false);
                var rect = emptyCell.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(64f, 64f);

                var img = emptyCell.AddComponent<Image>();
                img.sprite = slotFrame;
                img.color = Color.white;
            }
        }

        private void CreateAbilityCard(string name)
        {
            if (abilitiesRoot == null) return;

            var card = new GameObject(name, typeof(RectTransform));
            card.transform.SetParent(abilitiesRoot, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);

            var image = card.AddComponent<Image>();
            image.sprite = FindSlotFrameSprite();
            image.color = Color.white;

            var abilityCard = FindAbilityCard(name);

            // Icon child
            if (abilityCard != null && abilityCard.icon != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(card.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(6f, 6f);
                iconRect.offsetMax = new Vector2(-6f, -6f);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = abilityCard.icon;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            var drag = card.AddComponent<AbilityDragItem>();
            drag.Initialize(name);

            var trigger = card.AddComponent<ItemTooltipTrigger>();
            trigger.itemName = name;
        }

        private Sprite FindSlotFrameSprite()
        {
            var inputs = UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include);
            foreach (var inp in inputs)
            {
                var drop = inp.GetComponent<ItemDropSlot>();
                if (drop != null && drop.GetComponent<Image>() != null)
                {
                    return drop.GetComponent<Image>().sprite;
                }
            }
            return null;
        }

        private RPGTable.Core.AbilityCard FindAbilityCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.AbilityCard>("AbilityCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, System.StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }

        private void ReloadAvailableItems()
        {
            if (itemsRoot != null)
            {
                for (var i = itemsRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(itemsRoot.GetChild(i).gameObject);
                }
            }

            var cards = Resources.LoadAll<RPGTable.Core.ItemCard>("ItemCards");
            if (cards != null && cards.Length > 0)
            {
                foreach (var card in cards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.title))
                    {
                        CreateItemCard(card.title, card);
                    }
                }
            }
            else
            {
                var defaultItems = new (string name, string type)[] {
                    ("Меч", "Weapon"),
                    ("Щит", "Shield"),
                    ("Шлем", "Helmet"),
                    ("Кольцо", "Ring"),
                    ("Амулет", "Amulet"),
                    ("Сапоги", "Boots"),
                    ("Доспех", "Armor")
                };
                foreach (var item in defaultItems)
                {
                    CreateItemCard(item.name, null);
                }
            }
        }

        private void CreateItemCard(string name, RPGTable.Core.ItemCard cardData)
        {
            if (itemsRoot == null) return;

            var card = new GameObject(name, typeof(RectTransform));
            card.transform.SetParent(itemsRoot, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);

            var image = card.AddComponent<Image>();
            image.color = new Color(0.24f, 0.2f, 0.16f, 1f); // brown-ish item color

            if (cardData != null && cardData.icon != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(card.transform, false);
                Stretch(iconObj);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = cardData.icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            var textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(card.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5f, 5f);
            textRect.offsetMax = new Vector2(-5f, -5f);

            var text = textObj.AddComponent<Text>();
            text.text = name;
            text.alignment = TextAnchor.LowerCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 11;
            text.color = Color.white;
            text.raycastTarget = false;

            var drag = card.AddComponent<ItemDragItem>();
            drag.Initialize(name);
        }

        private void RecalculateEquippedBonuses()
        {
            int bonusStr = 0;
            int bonusAgi = 0;
            int bonusInt = 0;
            int bonusHol = 0;
            int bonusHp = 0;
            int totalArmor = 0;

            var equippedNames = new string[] {
                eqHelmetInput != null ? eqHelmetInput.text : "",
                eqArmorInput != null ? eqArmorInput.text : "",
                eqWeaponInput != null ? eqWeaponInput.text : "",
                eqWeapon2Input != null ? eqWeapon2Input.text : "",
                eqShieldInput != null ? eqShieldInput.text : "",
                eqBootsInput != null ? eqBootsInput.text : "",
                eqAmuletInput != null ? eqAmuletInput.text : "",
                eqRingInput != null ? eqRingInput.text : "",
                eqArtifactInput != null ? eqArtifactInput.text : "",
                eqBeltInput != null ? eqBeltInput.text : ""
            };

            RPGTable.Core.ItemCard weapon1Card = null;
            RPGTable.Core.ItemCard weapon2Card = null;

            foreach (var itemName in equippedNames)
            {
                if (string.IsNullOrWhiteSpace(itemName)) continue;
                var card = FindItemCard(itemName);
                if (card != null)
                {
                    bonusStr += card.bonusStr;
                    bonusAgi += card.bonusAgi;
                    bonusInt += card.bonusInt;
                    bonusHol += card.bonusHol;
                    bonusHp += card.bonusHp;
                    totalArmor += card.armorPoints;

                    if (card.itemType == RPGTable.Core.ItemType.Weapon)
                    {
                        if (itemName == (eqWeaponInput != null ? eqWeaponInput.text : ""))
                        {
                            weapon1Card = card;
                        }
                        else
                        {
                            weapon2Card = card;
                        }
                    }
                }
            }

            // Update UI Labels for Stats
            if (strengthValueLabel != null)
            {
                strengthValueLabel.text = bonusStr > 0 ? $"{strength} (+{bonusStr})" : strength.ToString();
            }
            if (agilityValueLabel != null)
            {
                agilityValueLabel.text = bonusAgi > 0 ? $"{agility} (+{bonusAgi})" : agility.ToString();
            }
            if (intelligenceValueLabel != null)
            {
                intelligenceValueLabel.text = bonusInt > 0 ? $"{intelligence} (+{bonusInt})" : intelligence.ToString();
            }
            if (holinessValueLabel != null)
            {
                holinessValueLabel.text = bonusHol > 0 ? $"{holiness} (+{bonusHol})" : holiness.ToString();
            }

            // Update Max Armor Input
            if (maxArmorInput != null && totalArmor > 0)
            {
                maxArmorInput.text = totalArmor.ToString();
            }

            // Update read-only weapon scaling labels
            UpdateWeaponStatsDisplay(weapon1Card, weapon2Card);

            // Set D6 slots filters based on equipped weapons
            var type1 = weapon1Card != null ? weapon1Card.attackType : RPGTable.Core.AttackType.Melee;
            var type2 = weapon2Card != null ? weapon2Card.attackType : RPGTable.Core.AttackType.Melee;

            if (attackSlots != null)
            {
                foreach (var slot in attackSlots)
                {
                    if (slot != null)
                    {
                        slot.allowedType = type1;
                        if (!string.IsNullOrEmpty(slot.abilityName))
                        {
                            var abilityCard = FindAbilityCard(slot.abilityName);
                            if (abilityCard != null && abilityCard.attackType != type1)
                            {
                                slot.SetAbility(string.Empty);
                                var input = slot.GetComponent<InputField>();
                                if (input != null) input.text = string.Empty;
                            }
                        }
                    }
                }
            }

            if (attack2Slots != null)
            {
                foreach (var slot in attack2Slots)
                {
                    if (slot != null)
                    {
                        slot.allowedType = type2;
                        if (!string.IsNullOrEmpty(slot.abilityName))
                        {
                            var abilityCard = FindAbilityCard(slot.abilityName);
                            if (abilityCard != null && abilityCard.attackType != type2)
                            {
                                slot.SetAbility(string.Empty);
                                var input = slot.GetComponent<InputField>();
                                if (input != null) input.text = string.Empty;
                            }
                        }
                    }
                }
            }

            if (defenseSlots != null)
            {
                foreach (var slot in defenseSlots)
                {
                    if (slot != null)
                    {
                        slot.allowedType = RPGTable.Core.AttackType.Defense;
                    }
                }
            }

            // Dynamically refresh the bottom bank skills based on equipped weapons
            ReloadAvailableAbilities();
        }

        private void UpdateWeaponStatsDisplay(RPGTable.Core.ItemCard weaponCard, RPGTable.Core.ItemCard weaponCard2)
        {
            if (weaponCard != null)
            {
                if (weaponNameLabel != null) weaponNameLabel.text = "Название: " + weaponCard.title;

                var scalingStr = "";
                if (weaponCard.scaleStat1 != "None" && !string.IsNullOrEmpty(weaponCard.scaleStat1))
                {
                    scalingStr += weaponCard.scaleStat1 + " (x" + weaponCard.coef1.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ")";
                }
                if (weaponCard.scaleStat2 != "None" && !string.IsNullOrEmpty(weaponCard.scaleStat2))
                {
                    if (scalingStr.Length > 0) scalingStr += " + ";
                    scalingStr += weaponCard.scaleStat2 + " (x" + weaponCard.coef2.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ")";
                }
                if (string.IsNullOrEmpty(scalingStr)) scalingStr = "Нет скейлинга";
                if (weaponScalingLabel != null) weaponScalingLabel.text = "Скейлинг: " + scalingStr;

                var attrList = new System.Collections.Generic.List<string>();
                if (weaponCard.attributes != null && weaponCard.attributes.Count > 0)
                {
                    foreach (var a in weaponCard.attributes)
                    {
                        if (a != null) attrList.Add(a.attributeName);
                    }
                }
                if (weaponAttributesLabel != null)
                {
                    weaponAttributesLabel.text = "Свойства: " + (attrList.Count > 0 ? string.Join(", ", attrList) : "Нет");
                }
            }
            else
            {
                if (weaponNameLabel != null) weaponNameLabel.text = "Название: Нет оружия 1";
                if (weaponScalingLabel != null) weaponScalingLabel.text = "Скейлинг: -";
                if (weaponAttributesLabel != null) weaponAttributesLabel.text = "Свойства: -";
            }

            if (weaponCard2 != null)
            {
                if (weapon2NameLabel != null) weapon2NameLabel.text = "Название: " + weaponCard2.title;

                var scalingStr = "";
                if (weaponCard2.scaleStat1 != "None" && !string.IsNullOrEmpty(weaponCard2.scaleStat1))
                {
                    scalingStr += weaponCard2.scaleStat1 + " (x" + weaponCard2.coef1.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ")";
                }
                if (weaponCard2.scaleStat2 != "None" && !string.IsNullOrEmpty(weaponCard2.scaleStat2))
                {
                    if (scalingStr.Length > 0) scalingStr += " + ";
                    scalingStr += weaponCard2.scaleStat2 + " (x" + weaponCard2.coef2.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ")";
                }
                if (string.IsNullOrEmpty(scalingStr)) scalingStr = "Нет скейлинга";
                if (weapon2ScalingLabel != null) weapon2ScalingLabel.text = "Скейлинг: " + scalingStr;

                var attrList = new System.Collections.Generic.List<string>();
                if (weaponCard2.attributes != null && weaponCard2.attributes.Count > 0)
                {
                    foreach (var a in weaponCard2.attributes)
                    {
                        if (a != null) attrList.Add(a.attributeName);
                    }
                }
                if (weapon2AttributesLabel != null)
                {
                    weapon2AttributesLabel.text = "Свойства: " + (attrList.Count > 0 ? string.Join(", ", attrList) : "Нет");
                }
            }
            else
            {
                if (weapon2NameLabel != null) weapon2NameLabel.text = "Название: Нет оружия 2";
                if (weapon2ScalingLabel != null) weapon2ScalingLabel.text = "Скейлинг: -";
                if (weapon2AttributesLabel != null) weapon2AttributesLabel.text = "Свойства: -";
            }
        }

        private RPGTable.Core.ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.ItemCard>("ItemCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }

        private static string[] ReadSlots(AbilityDropSlot[] inputs)
        {
            var result = new string[6];

            if (inputs == null)
            {
                return result;
            }

            for (var i = 0; i < result.Length && i < inputs.Length; i++)
            {
                result[i] = inputs[i] == null ? string.Empty : inputs[i].abilityName;
            }

            return result;
        }

        private static void WriteSlots(AbilityDropSlot[] inputs, string[] values)
        {
            if (inputs == null)
            {
                return;
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                var val = values != null && i < values.Length ? values[i] ?? string.Empty : string.Empty;
                inputs[i].SetAbility(val);
            }
        }

        private static string[] ReadBackpack(InputField[] inputs)
        {
            var result = new string[8];
            if (inputs == null) return result;
            for (var i = 0; i < result.Length && i < inputs.Length; i++)
            {
                result[i] = inputs[i] == null ? string.Empty : inputs[i].text;
            }
            return result;
        }

        private static void WriteBackpack(InputField[] inputs, string[] values)
        {
            if (inputs == null) return;
            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i].text = values != null && i < values.Length ? values[i] ?? string.Empty : string.Empty;
            }
        }

        private static void Stretch(GameObject gameObject)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
