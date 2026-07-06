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
        [SerializeField] private AbilityDropSlot[] defenseSlots;
        [SerializeField] private Toggle meleeToggle;
        [SerializeField] private Toggle magicToggle;
        [SerializeField] private Toggle rangedToggle;
        [SerializeField] private Toggle doubleDamageToggle;
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

        [SerializeField] private InputField weaponNameInput;
        [SerializeField] private InputField weaponCoef1Input;
        [SerializeField] private InputField weaponCoef2Input;
        [SerializeField] private InputField weaponAttributeInput;
        [SerializeField] private Text weaponScaleStat1Label;
        [SerializeField] private Text weaponScaleStat2Label;

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
        [SerializeField] private Button scaleStat1Button;
        [SerializeField] private Button scaleStat2Button;

        // Equipment & Inventory Slots
        [SerializeField] private InputField eqHelmetInput;
        [SerializeField] private InputField eqArmorInput;
        [SerializeField] private InputField eqWeaponInput;
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

        private string weaponScaleStat1 = "None";
        private string weaponScaleStat2 = "None";
        private int customAbilityCount = 1;

        public void Initialize(
            InputField nameField,
            InputField descriptionField,
            Image portrait,
            Text selectedTokenLabel,
            InputField hpField,
            AbilityDropSlot[] attackInputs,
            AbilityDropSlot[] defenseInputs,
            Toggle melee,
            Toggle magic,
            Toggle ranged,
            Toggle doubleDamage,
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

            InputField weaponNameField,
            InputField weaponCoef1Field,
            InputField weaponCoef2Field,
            InputField weaponAttributeField,
            Text scaleStat1Text, Button scaleStat1Btn,
            Text scaleStat2Text, Button scaleStat2Btn,

            Button charTabBtn, Button stTabBtn,
            GameObject charTabPanelObj, GameObject stTabPanelObj,
            Image tkPortraitPreview, Image tkFramePreview,

            InputField helmetField, InputField armorEquipField, InputField weaponEquipField, InputField shieldField,
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
            defenseSlots = defenseInputs;
            meleeToggle = melee;
            magicToggle = magic;
            rangedToggle = ranged;
            doubleDamageToggle = doubleDamage;
            abilitiesRoot = abilities;

            classInput = classField;
            levelInput = levelField;
            xpInput = xpField;
            maxArmorInput = armorField;

            strengthValueLabel = strVal;
            agilityValueLabel = agiVal;
            intelligenceValueLabel = intVal;
            holinessValueLabel = holVal;

            weaponNameInput = weaponNameField;
            weaponCoef1Input = weaponCoef1Field;
            weaponCoef2Input = weaponCoef2Field;
            weaponAttributeInput = weaponAttributeField;
            weaponScaleStat1Label = scaleStat1Text;
            weaponScaleStat2Label = scaleStat2Text;

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
            scaleStat1Button = scaleStat1Btn;
            scaleStat2Button = scaleStat2Btn;

            eqHelmetInput = helmetField;
            eqArmorInput = armorEquipField;
            eqWeaponInput = weaponEquipField;
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

            if (scaleStat1Button != null) scaleStat1Button.onClick.AddListener(() => CycleScaleStat(1));
            if (scaleStat2Button != null) scaleStat2Button.onClick.AddListener(() => CycleScaleStat(2));

            if (characterTabButton != null) characterTabButton.onClick.AddListener(() => SwitchTab(true));
            if (statsTabButton != null) statsTabButton.onClick.AddListener(() => SwitchTab(false));

            // Set up dynamic equipment listeners
            if (eqHelmetInput != null) eqHelmetInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqArmorInput != null) eqArmorInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
            if (eqWeaponInput != null) eqWeaponInput.onEndEdit.AddListener((val) => RecalculateEquippedBonuses());
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

        private void CycleScaleStat(int index)
        {
            if (index == 1)
            {
                weaponScaleStat1 = NextScaleStat(weaponScaleStat1);
                if (weaponScaleStat1Label != null) weaponScaleStat1Label.text = weaponScaleStat1;
            }
            else
            {
                weaponScaleStat2 = NextScaleStat(weaponScaleStat2);
                if (weaponScaleStat2Label != null) weaponScaleStat2Label.text = weaponScaleStat2;
            }
        }

        private string NextScaleStat(string current)
        {
            if (current == "None") return "STR";
            if (current == "STR") return "AGI";
            if (current == "AGI") return "INT";
            if (current == "INT") return "HOL";
            return "None";
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
                    ? new Color(0.26f, 0.22f, 0.17f, 1f)
                    : new Color(0.12f, 0.11f, 0.1f, 1f);
            }
            if (statsTabButton != null)
            {
                statsTabButton.GetComponent<Image>().color = !isCharacter
                    ? new Color(0.26f, 0.22f, 0.17f, 1f)
                    : new Color(0.12f, 0.11f, 0.1f, 1f);
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

            float coef1 = 0.6f;
            if (weaponCoef1Input != null && float.TryParse(weaponCoef1Input.text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var c1))
            {
                coef1 = c1;
            }

            float coef2 = 0.0f;
            if (weaponCoef2Input != null && float.TryParse(weaponCoef2Input.text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var c2))
            {
                coef2 = c2;
            }

            var data = new SavedCharacterData
            {
                description = descriptionInput == null ? string.Empty : descriptionInput.text,
                portraitPath = portraitPath,
                tokenPath = tokenPath,
                maxHp = maxHpVal,
                attackSlots = ReadSlots(attackSlots),
                defenseSlots = ReadSlots(defenseSlots),
                melee = meleeToggle != null && meleeToggle.isOn,
                magic = magicToggle != null && magicToggle.isOn,
                ranged = rangedToggle != null && rangedToggle.isOn,
                doubleDamage = doubleDamageToggle != null && doubleDamageToggle.isOn,
                abilityImagePaths = Array.Empty<string>(),

                // Save Stats & Level
                level = lvlVal,
                xp = xpVal,
                characterClass = classInput == null ? "Воин" : classInput.text,
                strength = strength,
                agility = agility,
                intelligence = intelligence,
                holiness = holiness,

                // Save Armor & Weapon
                maxArmor = armorVal,
                weaponName = weaponNameInput == null ? "" : weaponNameInput.text,
                weaponScaleStat1 = weaponScaleStat1,
                weaponCoef1 = coef1,
                weaponScaleStat2 = weaponScaleStat2,
                weaponCoef2 = coef2,
                weaponAttribute = weaponAttributeInput == null ? "" : weaponAttributeInput.text,

                // Save Equipment Slots
                eqHelmet = eqHelmetInput == null ? "" : eqHelmetInput.text,
                eqArmor = eqArmorInput == null ? "" : eqArmorInput.text,
                eqWeapon = eqWeaponInput == null ? "" : eqWeaponInput.text,
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

            // Load Weapon Scaling
            if (weaponNameInput != null) weaponNameInput.text = data.weaponName ?? "";
            weaponScaleStat1 = string.IsNullOrEmpty(data.weaponScaleStat1) ? "None" : data.weaponScaleStat1;
            weaponScaleStat2 = string.IsNullOrEmpty(data.weaponScaleStat2) ? "None" : data.weaponScaleStat2;
            if (weaponScaleStat1Label != null) weaponScaleStat1Label.text = weaponScaleStat1;
            if (weaponScaleStat2Label != null) weaponScaleStat2Label.text = weaponScaleStat2;

            if (weaponCoef1Input != null) weaponCoef1Input.text = data.weaponCoef1.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (weaponCoef2Input != null) weaponCoef2Input.text = data.weaponCoef2.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (weaponAttributeInput != null) weaponAttributeInput.text = data.weaponAttribute ?? "";

            // Load Equipment Slots
            if (eqHelmetInput != null) eqHelmetInput.text = data.eqHelmet ?? "";
            if (eqArmorInput != null) eqArmorInput.text = data.eqArmor ?? "";
            if (eqWeaponInput != null) eqWeaponInput.text = data.eqWeapon ?? "";
            if (eqShieldInput != null) eqShieldInput.text = data.eqShield ?? "";
            if (eqBootsInput != null) eqBootsInput.text = data.eqBoots ?? "";
            if (eqAmuletInput != null) eqAmuletInput.text = data.eqAmulet ?? "";
            if (eqRingInput != null) eqRingInput.text = data.eqRing ?? "";
            if (eqArtifactInput != null) eqArtifactInput.text = data.eqArtifact ?? "";
            if (eqBeltInput != null) eqBeltInput.text = data.eqBelt ?? "";

            // Load Backpack Slots
            WriteBackpack(backpackInputSlots, data.backpackSlots);

            WriteSlots(attackSlots, data.attackSlots);
            WriteSlots(defenseSlots, data.defenseSlots);

            if (meleeToggle != null) meleeToggle.isOn = data.melee;
            if (magicToggle != null) magicToggle.isOn = data.magic;
            if (rangedToggle != null) rangedToggle.isOn = data.ranged;
            if (doubleDamageToggle != null) doubleDamageToggle.isOn = data.doubleDamage;

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

            var cards = Resources.LoadAll<RPGTable.Core.AbilityCard>("AbilityCards");
            if (cards != null && cards.Length > 0)
            {
                foreach (var card in cards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.title))
                    {
                        CreateAbilityCard(card.title);
                    }
                }
            }
            else
            {
                var defaultAbilities = new string[] { "Удар", "Крит", "Промах", "Блок", "Уворот", "Контратака" };
                foreach (var name in defaultAbilities)
                {
                    CreateAbilityCard(name);
                }
            }
        }

        private void CreateAbilityCard(string name)
        {
            if (abilitiesRoot == null) return;

            var card = new GameObject(name, typeof(RectTransform));
            card.transform.SetParent(abilitiesRoot, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 110f);

            var image = card.AddComponent<Image>();
            image.color = new Color(0.18f, 0.16f, 0.14f, 1f);

            var textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(card.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5f, 5f);
            textRect.offsetMax = new Vector2(-5f, -5f);

            var text = textObj.AddComponent<Text>();
            text.text = name;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 14;
            text.color = Color.white;

            var drag = card.AddComponent<AbilityDragItem>();
            drag.Initialize(name);
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
                eqShieldInput != null ? eqShieldInput.text : "",
                eqBootsInput != null ? eqBootsInput.text : "",
                eqAmuletInput != null ? eqAmuletInput.text : "",
                eqRingInput != null ? eqRingInput.text : "",
                eqArtifactInput != null ? eqArtifactInput.text : "",
                eqBeltInput != null ? eqBeltInput.text : ""
            };

            RPGTable.Core.ItemCard weaponCard = null;

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
                        weaponCard = card;
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

            // Update Weapon Stats if a weapon item is equipped!
            if (weaponCard != null)
            {
                if (weaponNameInput != null) weaponNameInput.text = weaponCard.title;
                weaponScaleStat1 = weaponCard.scaleStat1;
                if (weaponScaleStat1Label != null) weaponScaleStat1Label.text = weaponScaleStat1;
                if (weaponCoef1Input != null) weaponCoef1Input.text = weaponCard.coef1.ToString(System.Globalization.CultureInfo.InvariantCulture);

                weaponScaleStat2 = weaponCard.scaleStat2;
                if (weaponScaleStat2Label != null) weaponScaleStat2Label.text = weaponScaleStat2;
                if (weaponCoef2Input != null) weaponCoef2Input.text = weaponCard.coef2.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (weaponAttributeInput != null) weaponAttributeInput.text = weaponCard.attribute;
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
