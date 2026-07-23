using UnityEngine;

namespace RPGTable.Runtime.ActionCards
{
    [CreateAssetMenu(fileName = "NewActionCard", menuName = "RPGTable/Action Card")]
    public class ActionCard : ScriptableObject
    {
        public string id;
        public string title;
        public string description;
        public int manaCost;
        public string effectId; // e.g. "invisibility", "heal_party", "fireball", "restore_armor"
        public Sprite icon;
        
        // Custom variables depending on strategy
        public float power;
        public int radius;
        public float duration;
    }
}
