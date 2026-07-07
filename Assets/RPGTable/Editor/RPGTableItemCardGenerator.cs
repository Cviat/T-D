using UnityEditor;
using UnityEngine;
using RPGTable.Core;
using System.IO;

namespace RPGTable.Editor
{
    public static class RPGTableItemCardGenerator
    {
        [MenuItem("RPG Table/Generate Item Cards from Pack")]
        public static void GenerateItemCards()
        {
            var targetFolder = "Assets/RPGTable/Resources/ItemCards";
            Directory.CreateDirectory(targetFolder);

            // 1. Generate Weapons
            var weaponIconsFolder = "Assets/Basic_RPG_Icons/Items/Weapons";
            int weaponsCount = 0;
            if (Directory.Exists(weaponIconsFolder))
            {
                var files = Directory.GetFiles(weaponIconsFolder, "*.png");
                foreach (var file in files)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                    if (sprite == null) continue;

                    var itemName = Path.GetFileNameWithoutExtension(file);
                    if (itemName.Length > 3 && char.IsDigit(itemName[0]) && itemName[2] == '_')
                    {
                        itemName = itemName.Substring(3);
                    }
                    itemName = itemName.Replace("_", " ");

                    var assetPath = $"{targetFolder}/{itemName}.asset";
                    var card = AssetDatabase.LoadAssetAtPath<ItemCard>(assetPath);
                    bool isNew = false;
                    if (card == null)
                    {
                        card = ScriptableObject.CreateInstance<ItemCard>();
                        isNew = true;
                    }

                    card.title = itemName;
                    card.icon = sprite;
                    card.itemType = ItemType.Weapon;

                    // Set default stats based on item name
                    var stunAttr = AssetDatabase.LoadAssetAtPath<CombatAttribute>("Assets/RPGTable/Resources/CombatAttributes/Stun.asset");
                    card.attributes = new System.Collections.Generic.List<CombatAttribute>();

                    if (itemName.Contains("Bow"))
                    {
                        card.description = "Быстрый охотничий лук.";
                        card.scaleStat1 = "AGI";
                        card.coef1 = 0.8f;
                        card.attribute = "Дальний бой";
                        card.attackType = AttackType.Ranged;
                    }
                    else if (itemName.Contains("Sword") || itemName.Contains("Longsword"))
                    {
                        card.description = "Острый стальной меч.";
                        card.scaleStat1 = "STR";
                        card.coef1 = 0.6f;
                        card.scaleStat2 = "AGI";
                        card.coef2 = 0.2f;
                        card.attackType = AttackType.Melee;
                    }
                    else if (itemName.Contains("Greataxe") || itemName.Contains("One Handed Axe"))
                    {
                        card.description = "Тяжелый боевой топор.";
                        card.scaleStat1 = "STR";
                        card.coef1 = 1.0f;
                        card.attribute = "Оглушение";
                        if (stunAttr != null) card.attributes.Add(stunAttr);
                        card.attackType = AttackType.Melee;
                    }
                    else if (itemName.Contains("Mace"))
                    {
                        card.description = "Тяжелая шипованная булава.";
                        card.scaleStat1 = "STR";
                        card.coef1 = 0.9f;
                        card.attribute = "Оглушение";
                        if (stunAttr != null) card.attributes.Add(stunAttr);
                        card.attackType = AttackType.Melee;
                    }
                    else if (itemName.Contains("Staff"))
                    {
                        card.description = "Магический посох.";
                        card.scaleStat1 = "INT";
                        card.coef1 = 0.8f;
                        card.attribute = "Магия";
                        card.attackType = AttackType.Magic;
                    }
                    else if (itemName.Contains("Shield"))
                    {
                        card.description = "Крепкий щит.";
                        card.itemType = ItemType.Shield;
                        card.armorPoints = 4;
                    }

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(card, assetPath);
                    }
                    else
                    {
                        EditorUtility.SetDirty(card);
                    }
                    weaponsCount++;
                }
            }

            // 2. Generate Armors
            var armorIconsFolder = "Assets/Basic_RPG_Icons/Items/Armor/Common";
            int armorCount = 0;
            if (Directory.Exists(armorIconsFolder))
            {
                var files = Directory.GetFiles(armorIconsFolder, "*.png");
                foreach (var file in files)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                    if (sprite == null) continue;

                    var itemName = Path.GetFileNameWithoutExtension(file);
                    if (itemName.Length > 3 && char.IsDigit(itemName[0]) && itemName[2] == '_')
                    {
                        itemName = itemName.Substring(3);
                    }
                    itemName = itemName.Replace("_", " ");

                    var assetPath = $"{targetFolder}/{itemName}.asset";
                    var card = AssetDatabase.LoadAssetAtPath<ItemCard>(assetPath);
                    bool isNew = false;
                    if (card == null)
                    {
                        card = ScriptableObject.CreateInstance<ItemCard>();
                        isNew = true;
                    }

                    card.title = itemName;
                    card.icon = sprite;

                    // Set Type and Stats
                    if (itemName.Contains("helm"))
                    {
                        card.itemType = ItemType.Helmet;
                        card.armorPoints = 2;
                        card.description = "Надежный шлем.";
                    }
                    else if (itemName.Contains("chest"))
                    {
                        card.itemType = ItemType.Armor;
                        card.armorPoints = 6;
                        card.description = "Нагрудный доспех.";
                    }
                    else if (itemName.Contains("boots"))
                    {
                        card.itemType = ItemType.Boots;
                        card.armorPoints = 2;
                        card.description = "Кожаные сапоги.";
                    }
                    else if (itemName.Contains("gloves"))
                    {
                        card.itemType = ItemType.General;
                        card.description = "Перчатки.";
                    }
                    else
                    {
                        card.itemType = ItemType.General;
                        card.description = "Элемент экипировки.";
                    }

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(card, assetPath);
                    }
                    else
                    {
                        EditorUtility.SetDirty(card);
                    }
                    armorCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Item Cards Generated: {weaponsCount} weapons, {armorCount} armors.");
        }
    }
}
