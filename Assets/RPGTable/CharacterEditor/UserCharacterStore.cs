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

        public static Sprite LoadPortraitSprite(string path)
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
