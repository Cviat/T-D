using UnityEngine;
using UnityEditor;

namespace RPGTable.Runtime.ActionCards
{
    public static class ActionCardAssetGenerator
    {
#if UNITY_EDITOR
        [MenuItem("RPGTable/Generate Test Action Cards")]
        public static void GenerateCards()
        {
            // Ensure folders exist
            string folderPath = "Assets/RPGTable/Resources/ActionCards";
            if (!AssetDatabase.IsValidFolder("Assets/RPGTable/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/RPGTable"))
                {
                    AssetDatabase.CreateFolder("Assets", "RPGTable");
                }
                AssetDatabase.CreateFolder("Assets/RPGTable", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/RPGTable/Resources", "ActionCards");
            }

            // Invisibility Card
            var invisibility = ScriptableObject.CreateInstance<ActionCard>();
            invisibility.id = "invisibility_card";
            invisibility.title = "Невидимость";
            invisibility.description = "Делает токен персонажа полупрозрачным и скрытным на 5 секунд.";
            invisibility.manaCost = 2;
            invisibility.effectId = "invisibility";
            invisibility.duration = 5f;
            string invisIconPath = AssetDatabase.GUIDToAssetPath("26128b33321d29341b5bc92da7321350");
            if (!string.IsNullOrEmpty(invisIconPath)) invisibility.icon = AssetDatabase.LoadAssetAtPath<Sprite>(invisIconPath);
            AssetDatabase.CreateAsset(invisibility, $"{folderPath}/invisibility_card.asset");

            // Heal Party Card
            var heal = ScriptableObject.CreateInstance<ActionCard>();
            heal.id = "heal_party_card";
            heal.title = "Общее лечение";
            heal.description = "Восполняет 15 единиц здоровья всему отряду.";
            heal.manaCost = 3;
            heal.effectId = "heal_party";
            heal.power = 15f;
            string healIconPath = AssetDatabase.GUIDToAssetPath("4637953d65a24364aab3372a525b9ed2");
            if (!string.IsNullOrEmpty(healIconPath)) heal.icon = AssetDatabase.LoadAssetAtPath<Sprite>(healIconPath);
            AssetDatabase.CreateAsset(heal, $"{folderPath}/heal_party_card.asset");

            // Restore Armor Card
            var armor = ScriptableObject.CreateInstance<ActionCard>();
            armor.id = "restore_armor_card";
            armor.title = "Восстановление брони";
            armor.description = "Восстанавливает 10 единиц брони всему отряду.";
            armor.manaCost = 3;
            armor.effectId = "restore_armor";
            armor.power = 10f;
            string armorIconPath = AssetDatabase.GUIDToAssetPath("04f5897dc41fbce4eb4bbe94e9edaff2");
            if (!string.IsNullOrEmpty(armorIconPath)) armor.icon = AssetDatabase.LoadAssetAtPath<Sprite>(armorIconPath);
            AssetDatabase.CreateAsset(armor, $"{folderPath}/restore_armor_card.asset");

            // Fireball Card
            var fireball = ScriptableObject.CreateInstance<ActionCard>();
            fireball.id = "fireball_card";
            fireball.title = "Огненный шар";
            fireball.description = "Наносит 20 единиц урона всем противникам в радиусе 2 клеток.";
            fireball.manaCost = 4;
            fireball.effectId = "fireball";
            fireball.power = 20f;
            fireball.radius = 2;
            string fireballIconPath = AssetDatabase.GUIDToAssetPath("5182be4db3c5be946af47fb1ede1abbc");
            if (!string.IsNullOrEmpty(fireballIconPath)) fireball.icon = AssetDatabase.LoadAssetAtPath<Sprite>(fireballIconPath);
            AssetDatabase.CreateAsset(fireball, $"{folderPath}/fireball_card.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Action Cards generated successfully under Assets/Resources/ActionCards!");
        }
#endif
    }
}
