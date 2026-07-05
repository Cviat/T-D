using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public class InitiativeRowView : MonoBehaviour
    {
        [SerializeField] private Text textLabel;
        [SerializeField] private Image background;

        public void Setup(CampaignRuntimeToken token)
        {
            if (textLabel != null)
            {
                var text = $"{token.DisplayName} " + (token.IsDead ? "(Мертв)" : $"({token.CurrentHp}/{token.MaxHp} HP)");
                textLabel.text = text;
                
                if (token.IsDead)
                {
                    textLabel.color = new Color(1f, 1f, 1f, 0.4f);
                }
                else
                {
                    textLabel.color = Color.white;
                }
            }

            if (background != null)
            {
                if (token.IsDead)
                {
                    background.color = new Color(0.25f, 0.1f, 0.1f, 0.4f);
                }
                else
                {
                    background.color = new Color(0.18f, 0.18f, 0.18f, 0.5f);
                }
            }
        }
    }
}
