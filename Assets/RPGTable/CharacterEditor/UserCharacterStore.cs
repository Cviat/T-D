using System;
using System.Collections.Generic;
using System.IO;
using RPGTable.TokenEditor;
using UnityEngine;

namespace RPGTable.CharacterEditor
{
    [Serializable]
    public sealed class SavedCharacterData
    {
        public string name;
        public string description;
        public string portraitPath;
        public string tokenPath;
        public int maxHp = 10;
        public string[] attackSlots = new string[6];
        public string[] attack2Slots = new string[6];
        public string[] defenseSlots = new string[6];
        public string[] abilityImagePaths = Array.Empty<string>();

        // New stats & level parameters
        public int level = 1;
        public int xp = 0;
        public string characterClass = "Воин";
        public int skillPoints = 0;
        public int strength = 10;
        public int agility = 10;
        public int intelligence = 10;
        public int holiness = 10;

        // Armor
        public int maxArmor = 0;

        // Equipment Slots
        public string eqHelmet = "";
        public string eqArmor = "";
        public string eqWeapon = "";
        public string eqWeapon2 = "";
        public string eqShield = "";
        public string eqBoots = "";
        public string eqAmulet = "";
        public string eqRing = "";
        public string eqArtifact = "";
        public string eqBelt = "";

        // Backpack Inventory (8 slots)
        public string[] backpackSlots = new string[8];
    }

    public static class UserCharacterStore
    {
        private const string Extension = ".json";

        public static string CharactersFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, "RPGTable", "Characters");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static IReadOnlyList<string> GetCharacterPaths()
        {
            Directory.CreateDirectory(CharactersFolder);
            var result = new List<string>(Directory.GetFiles(CharactersFolder, $"*{Extension}"));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static string SaveCharacter(string characterName, SavedCharacterData data)
        {
            var safeName = SanitizeName(characterName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            data.name = safeName;
            var path = Path.Combine(CharactersFolder, $"{safeName}{Extension}");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return path;
        }

        public static SavedCharacterData LoadCharacter(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SavedCharacterData>(File.ReadAllText(path));
        }

        public static string GetDisplayName(string path)
        {
            var data = LoadCharacter(path);
            return !string.IsNullOrWhiteSpace(data?.name)
                ? data.name
                : Path.GetFileNameWithoutExtension(path);
        }

        public static string ImportPortraitWithDialog()
        {
            return UserTokenStore.ImportImageWithDialog("Import character portrait");
        }

        public static Sprite LoadSprite(string path)
        {
            return UserTokenStore.LoadSprite(path);
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var result = name.Trim();

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '_');
            }

            return result;
        }
    }
}
