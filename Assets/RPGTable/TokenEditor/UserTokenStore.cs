using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGTable.TokenEditor
{
    [Serializable]
    public sealed class SavedTokenData
    {
        public string name;
        public string framePath;
        public string portraitPath;
        public int footprintSize = 1;
        public bool hasPortraitMaskLayout;
        public Vector2 portraitMaskPositionRatio;
        public Vector2 portraitMaskSizeRatio;
    }

    public static class UserTokenStore
    {
        private const string Extension = ".json";

        private static readonly Dictionary<string, SavedTokenData> tokenCache = new Dictionary<string, SavedTokenData>();
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static string TokensFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, "RPGTable", "Tokens");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string TokenImagesFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, "RPGTable", "TokenImages");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static IReadOnlyList<string> GetTokenPaths()
        {
            Directory.CreateDirectory(TokensFolder);
            var result = new List<string>(Directory.GetFiles(TokensFolder, $"*{Extension}"));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static string SaveToken(string tokenName, SavedTokenData data)
        {
            var safeName = SanitizeName(tokenName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            data.name = safeName;
            var path = Path.Combine(TokensFolder, $"{safeName}{Extension}");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            tokenCache[path] = data;
            return path;
        }

        public static SavedTokenData LoadToken(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            if (tokenCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var data = JsonUtility.FromJson<SavedTokenData>(File.ReadAllText(path));
            if (data != null)
            {
                tokenCache[path] = data;
            }
            return data;
        }

        public static string GetDisplayName(string path)
        {
            var data = LoadToken(path);
            return !string.IsNullOrWhiteSpace(data?.name)
                ? data.name
                : Path.GetFileNameWithoutExtension(path);
        }

        public static string ImportImageWithDialog(string title)
        {
#if UNITY_EDITOR
            var sourcePath = EditorUtility.OpenFilePanel(title, "", "png,jpg,jpeg");

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            return ImportImage(sourcePath);
#else
            Debug.LogWarning("Runtime file picker is not implemented yet.");
            return null;
#endif
        }

        public static string ImportImage(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath) || !IsSupportedImage(sourcePath))
            {
                return null;
            }

            var targetPath = UniquePath(Path.Combine(TokenImagesFolder, Path.GetFileName(sourcePath)));
            File.Copy(sourcePath, targetPath);
            return targetPath;
        }

        /// <summary>
        /// Resolves an "Assets/…" relative path (used for built-in frame sprites) to an
        /// absolute disk path so that File.Exists / File.ReadAllBytes work at runtime.
        /// </summary>
        private static string ResolveAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // Already absolute (user-imported images stored in persistentDataPath)
            if (Path.IsPathRooted(path))
                return path;

            // Asset-relative path like "Assets/RPGTable/Art/TokenFrames/Frame_01.png"
            if (path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", System.StringComparison.OrdinalIgnoreCase))
            {
                // Application.dataPath ends with "/Assets", so strip the leading "Assets" part
                var relativePart = path.Substring("Assets".Length).TrimStart('/', '\\');
                return Path.Combine(Application.dataPath, relativePart);
            }

            return path;
        }

        public static Sprite LoadSprite(string path)
        {
            var resolvedPath = ResolveAbsolutePath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }
            // Normalise the cache key to the original path so callers don't need to care
            path = resolvedPath;

            if (spriteCache.TryGetValue(path, out var cached) && cached != null)
            {
                return cached;
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[path] = sprite;
            return sprite;
        }

        private static bool IsSupportedImage(string path)
        {
            var extension = Path.GetExtension(path);
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static string UniquePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var index = 1;

            while (true)
            {
                var candidate = Path.Combine(directory, $"{name}_{index}{extension}");

                if (!File.Exists(candidate))
                {
                    return candidate;
                }

                index++;
            }
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
