#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using RPGTable.Core;
using System.Collections.Generic;

namespace RPGTable.Editor
{
    public static class RPGTableAbilityCardGenerator
    {
        [MenuItem("RPG Table/Generate Ability Cards")]
        public static void GenerateAbilityCards()
        {
            var attribDir = "Assets/RPGTable/Resources/CombatAttributes";
            if (!Directory.Exists(attribDir))
            {
                Directory.CreateDirectory(attribDir);
            }

            var abilityDir = "Assets/RPGTable/Resources/AbilityCards";
            if (!Directory.Exists(abilityDir))
            {
                Directory.CreateDirectory(abilityDir);
            }

            // Load Icons
            var stunIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_01_nobg.png") 
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_01.png");
            var shotIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_02_nobg.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_02.png");
            var fireIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_03_nobg.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_03.png");
            var healIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_04_nobg.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Icons/skill_icon_04.png");

            // Create Combat Attributes
            var stunAttr = CreateOrUpdateAttribute("Stun", "Оглушение", stunIcon, "Rolls", -999, 1, false, "Цель оглушена и пропускает свой следующий ход.");
            var burnAttr = CreateOrUpdateAttribute("Burn", "Поджог", fireIcon, "HP", -3, 3, false, "Цель горит и получает периодический урон от огня в начале своего хода.");
            var poisonAttr = CreateOrUpdateAttribute("Poison", "Отравление", shotIcon, "HP", -4, 3, false, "Цель отравлена и получает урон ядом в конце каждого своего хода.");
            var shieldAttr = CreateOrUpdateAttribute("ShieldBuff", "Эгида", healIcon, "Armor", 0, 1, true, "Дарует щит, блокирующий следующий входящий урон.");

            // Create 15 Distinct Ability Cards
            CreateOrUpdateAbility("HeavyStrike", "Сильный удар", "Наносит мощный рубящий удар с размаху, оглушая цель.", heavyStrike =>
            {
                heavyStrike.icon = stunIcon;
                heavyStrike.cost = 2;
                heavyStrike.range = 1;
                heavyStrike.targetType = AbilityTargetType.Enemy;
                heavyStrike.effectType = AbilityEffectType.Damage;
                heavyStrike.effectValue = 5;
                heavyStrike.multiplier = 1.5f;
                heavyStrike.attributes = new List<CombatAttribute> { stunAttr };
                heavyStrike.attackType = AttackType.Melee;
            });

            CreateOrUpdateAbility("QuickShot", "Быстрый выстрел", "Стремительный выстрел из лука на дальнюю дистанцию.", quickShot =>
            {
                quickShot.icon = shotIcon;
                quickShot.cost = 1;
                quickShot.range = 4;
                quickShot.targetType = AbilityTargetType.Enemy;
                quickShot.effectType = AbilityEffectType.Damage;
                quickShot.effectValue = 3;
                quickShot.multiplier = 1.2f;
                quickShot.attributes = new List<CombatAttribute>();
                quickShot.attackType = AttackType.Ranged;
            });

            CreateOrUpdateAbility("Fireball", "Огненный шар", "Выпускает взрывающийся сгусток пламени, поджигающий врага.", fireball =>
            {
                fireball.icon = fireIcon;
                fireball.cost = 3;
                fireball.range = 3;
                fireball.targetType = AbilityTargetType.Enemy;
                fireball.effectType = AbilityEffectType.Damage;
                fireball.effectValue = 8;
                fireball.multiplier = 2.0f;
                fireball.attributes = new List<CombatAttribute> { burnAttr };
                fireball.attackType = AttackType.Magic;
            });

            CreateOrUpdateAbility("LesserHeal", "Малое лечение", "Призывает божественный свет для исцеления раненого союзника.", lesserHeal =>
            {
                lesserHeal.icon = healIcon;
                lesserHeal.cost = 2;
                lesserHeal.range = 2;
                lesserHeal.targetType = AbilityTargetType.Ally;
                lesserHeal.effectType = AbilityEffectType.Heal;
                lesserHeal.effectValue = 6;
                lesserHeal.multiplier = 1.5f;
                lesserHeal.attributes = new List<CombatAttribute>();
                lesserHeal.attackType = AttackType.Defense;
            });

            CreateOrUpdateAbility("ShieldOfFaith", "Щит веры", "Накладывает на союзника благословенный щит.", faithShield =>
            {
                faithShield.icon = healIcon;
                faithShield.cost = 2;
                faithShield.range = 3;
                faithShield.targetType = AbilityTargetType.Ally;
                faithShield.effectType = AbilityEffectType.Status;
                faithShield.effectValue = 0;
                faithShield.multiplier = 1.0f;
                faithShield.attributes = new List<CombatAttribute> { shieldAttr };
                faithShield.attackType = AttackType.Defense;
            });

            CreateOrUpdateAbility("PoisonDart", "Отравленный дротик", "Стреляет отравленным дротиком, заражающим врага ядом.", poisonDart =>
            {
                poisonDart.icon = shotIcon;
                poisonDart.cost = 2;
                poisonDart.range = 3;
                poisonDart.targetType = AbilityTargetType.Enemy;
                poisonDart.effectType = AbilityEffectType.Damage;
                poisonDart.effectValue = 2;
                poisonDart.multiplier = 1.0f;
                poisonDart.attributes = new List<CombatAttribute> { poisonAttr };
                poisonDart.attackType = AttackType.Ranged;
            });

            CreateOrUpdateAbility("LightningBolt", "Разряд молнии", "Поражает противника электрической стрелой с неба.", lightning =>
            {
                lightning.icon = fireIcon;
                lightning.cost = 3;
                lightning.range = 4;
                lightning.targetType = AbilityTargetType.Enemy;
                lightning.effectType = AbilityEffectType.Damage;
                lightning.effectValue = 7;
                lightning.multiplier = 1.8f;
                lightning.attributes = new List<CombatAttribute>();
                lightning.attackType = AttackType.Magic;
            });

            CreateOrUpdateAbility("VampiricTouch", "Прикосновение вампира", "Похищает жизненную силу врага, восстанавливая здоровье.", vamp =>
            {
                vamp.icon = stunIcon;
                vamp.cost = 3;
                vamp.range = 1;
                vamp.targetType = AbilityTargetType.Enemy;
                vamp.effectType = AbilityEffectType.Heal;
                vamp.effectValue = 4;
                vamp.multiplier = 1.3f;
                vamp.attributes = new List<CombatAttribute>();
                vamp.attackType = AttackType.Melee;
            });

            CreateOrUpdateAbility("HolyRadiance", "Святое сияние", "Исцеляет всех союзников вокруг волной света.", radiance =>
            {
                radiance.icon = healIcon;
                radiance.cost = 4;
                radiance.range = 2;
                radiance.targetType = AbilityTargetType.Area;
                radiance.effectType = AbilityEffectType.Heal;
                radiance.effectValue = 5;
                radiance.multiplier = 1.4f;
                radiance.attributes = new List<CombatAttribute>();
                radiance.attackType = AttackType.Defense;
            });

            CreateOrUpdateAbility("IceArrow", "Ледяная стрела", "Выстреливает ледяной стрелой, замедляющей или оглушающей врага.", iceArrow =>
            {
                iceArrow.icon = shotIcon;
                iceArrow.cost = 2;
                iceArrow.range = 4;
                iceArrow.targetType = AbilityTargetType.Enemy;
                iceArrow.effectType = AbilityEffectType.Damage;
                iceArrow.effectValue = 4;
                iceArrow.multiplier = 1.1f;
                iceArrow.attributes = new List<CombatAttribute> { stunAttr };
                iceArrow.attackType = AttackType.Ranged;
            });

            CreateOrUpdateAbility("FlameBurst", "Вспышка пламени", "Концентрированный взрыв огня на близком расстоянии.", flame =>
            {
                flame.icon = fireIcon;
                flame.cost = 2;
                flame.range = 2;
                flame.targetType = AbilityTargetType.Enemy;
                flame.effectType = AbilityEffectType.Damage;
                flame.effectValue = 5;
                flame.multiplier = 1.4f;
                flame.attributes = new List<CombatAttribute> { burnAttr };
                flame.attackType = AttackType.Magic;
            });

            CreateOrUpdateAbility("StoneSkin", "Каменная кожа", "Временно увеличивает сопротивление урону союзника.", stone =>
            {
                stone.icon = healIcon;
                stone.cost = 1;
                stone.range = 1;
                stone.targetType = AbilityTargetType.Self;
                stone.effectType = AbilityEffectType.Status;
                stone.effectValue = 3;
                stone.multiplier = 1.0f;
                stone.attributes = new List<CombatAttribute>();
                stone.attackType = AttackType.Defense;
            });

            CreateOrUpdateAbility("BladeVortex", "Вихрь клинков", "Круговая атака ближнего боя по площади вокруг себя.", vortex =>
            {
                vortex.icon = stunIcon;
                vortex.cost = 3;
                vortex.range = 1;
                vortex.targetType = AbilityTargetType.Area;
                vortex.effectType = AbilityEffectType.Damage;
                vortex.effectValue = 4;
                vortex.multiplier = 1.3f;
                vortex.attributes = new List<CombatAttribute>();
                vortex.attackType = AttackType.Melee;
            });

            CreateOrUpdateAbility("Dash", "Рывок", "Быстрое перемещение персонажа на две клетки вперед.", dash =>
            {
                dash.icon = shotIcon;
                dash.cost = 1;
                dash.range = 2;
                dash.targetType = AbilityTargetType.Area;
                dash.effectType = AbilityEffectType.Move;
                dash.effectValue = 2;
                dash.multiplier = 1.0f;
                dash.attributes = new List<CombatAttribute>();
                dash.attackType = AttackType.Defense;
            });

            CreateOrUpdateAbility("Purify", "Очищение", "Снимает все вредоносные эффекты состояния с дружественной цели.", purify =>
            {
                purify.icon = healIcon;
                purify.cost = 1;
                purify.range = 2;
                purify.targetType = AbilityTargetType.Ally;
                purify.effectType = AbilityEffectType.Status;
                purify.effectValue = 0;
                purify.multiplier = 1.0f;
                purify.attributes = new List<CombatAttribute>();
                purify.attackType = AttackType.Defense;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("15 distinct Ability Cards and Combat Attributes successfully generated inside Resources.");
        }

        private static CombatAttribute CreateOrUpdateAttribute(
            string assetName, 
            string displayName, 
            Sprite icon, 
            string affectedStat, 
            int value, 
            int durationTurns, 
            bool appliedToSelf, 
            string desc)
        {
            var path = $"Assets/RPGTable/Resources/CombatAttributes/{assetName}.asset";
            var attr = AssetDatabase.LoadAssetAtPath<CombatAttribute>(path);
            if (attr == null)
            {
                attr = ScriptableObject.CreateInstance<CombatAttribute>();
                AssetDatabase.CreateAsset(attr, path);
            }

            attr.attributeName = displayName;
            attr.affectedStat = affectedStat;
            attr.icon = icon;
            attr.value = value;
            attr.durationTurns = durationTurns;
            attr.appliedToSelf = appliedToSelf;
            attr.description = desc;

            EditorUtility.SetDirty(attr);
            return attr;
        }

        private static void CreateOrUpdateAbility(string assetName, string displayName, string desc, System.Action<AbilityCard> setup)
        {
            var path = $"Assets/RPGTable/Resources/AbilityCards/{assetName}.asset";
            var ability = AssetDatabase.LoadAssetAtPath<AbilityCard>(path);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<AbilityCard>();
                AssetDatabase.CreateAsset(ability, path);
            }

            ability.title = displayName;
            ability.description = desc;
            setup?.Invoke(ability);

            EditorUtility.SetDirty(ability);
        }
    }
}
#endif
