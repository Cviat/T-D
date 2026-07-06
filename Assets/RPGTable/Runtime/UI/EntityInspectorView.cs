using System;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.TokenEditor;

namespace RPGTable.Runtime
{
    public class EntityInspectorView : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text stateLabel;
        [SerializeField] private Text descLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private InputField hpInput;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;

        public void Setup(
            CampaignRuntimeToken token, 
            SavedTokenData tokenData, 
            RPGTable.CharacterEditor.SavedCharacterData charData,
            Sprite portrait, 
            Action<int> onDamage, 
            Action<int> onHeal)
        {
            if (portraitImage != null && portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
            }

            if (nameLabel != null) nameLabel.text = token.DisplayName;

            if (stateLabel != null)
            {
                stateLabel.text = $"HP: {token.CurrentHp}/{token.MaxHp} " + (token.IsDead ? "[МЕРТВ]" : "[ЖИВ]");
                stateLabel.color = token.IsDead ? Color.red : new Color(0.2f, 0.8f, 0.2f, 1f);
            }

            if (descLabel != null)
            {
                var desc = charData != null ? charData.description : "";
                desc = string.IsNullOrWhiteSpace(desc) ? "Нет описания." : desc;
                if (desc.Length > 60) desc = desc.Substring(0, 57) + "...";
                descLabel.text = desc;
            }

            if (statsLabel != null)
            {
                var footprint = tokenData != null ? tokenData.footprintSize : 1;
                bool melee = charData != null ? charData.melee : false;
                bool magic = charData != null ? charData.magic : false;
                bool ranged = charData != null ? charData.ranged : false;
                bool doubleDmg = charData != null ? charData.doubleDamage : false;
                
                statsLabel.text = $"Размер сетки: {footprint}x{footprint}\n" +
                                     $"Тип атаки: " + (melee ? "Ближний " : "") + (magic ? "Магия " : "") + (ranged ? "Дальний" : "") + "\n" +
                                     (doubleDmg ? "Двойной урон: Да" : "Двойной урон: Нет");
            }

            if (damageButton != null)
            {
                damageButton.onClick.RemoveAllListeners();
                damageButton.onClick.AddListener(() => {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onDamage?.Invoke(val);
                    }
                });
            }

            if (healButton != null)
            {
                healButton.onClick.RemoveAllListeners();
                healButton.onClick.AddListener(() => {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onHeal?.Invoke(val);
                    }
                });
            }
        }
    }
}
