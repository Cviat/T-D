using System;
using System.Collections.Generic;
using System.IO;
using RPGTable.Runtime;
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
        private const string RootFolderName = "RPGTable";
        private const string TokensFolderName = "Tokens";
        private const string TokenImagesFolderName = "TokenImages";

        private static readonly Dictionary<string, SavedTokenData> tokenCache = new Dictionary<string, SavedTokenData>();
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static string RootFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, RootFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string TokensFolder
        {
            get
            {
                var path = Path.Combine(RootFolder, TokensFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string TokenImagesFolder
        {
            get
            {
                var path = Path.Combine(RootFolder, TokenImagesFolderName);
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
            data.framePath = ToPortableUserDataPath(data.framePath, TokenImagesFolderName);
            data.portraitPath = ToPortableUserDataPath(data.portraitPath, TokenImagesFolderName);
            var path = Path.Combine(TokensFolder, $"{safeName}{Extension}");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            var portablePath = ToPortablePath(path);
            tokenCache[ResolveUserDataPath(portablePath, TokensFolderName)] = data;
            return portablePath;
        }

        public static SavedTokenData LoadToken(string path)
        {
            var resolvedPath = ResolveUserDataPath(path, TokensFolderName);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            if (tokenCache.TryGetValue(resolvedPath, out var cached))
            {
                return cached;
            }

            var data = JsonUtility.FromJson<SavedTokenData>(File.ReadAllText(resolvedPath));
            if (data != null)
            {
                tokenCache[resolvedPath] = data;
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
            var sourcePath = StandaloneFileDialog.OpenFilePanel(title, "png,jpg,jpeg");

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            return ImportImage(sourcePath);
#endif
        }

        public static string ImportImage(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath) || !IsSupportedImage(sourcePath))
            {
                return null;
            }

            // If the source image is already inside the TokenImages folder, just return it directly!
            var fullSource = Path.GetFullPath(sourcePath);
            var fullTokenImages = Path.GetFullPath(TokenImagesFolder);
            if (fullSource.StartsWith(fullTokenImages, StringComparison.OrdinalIgnoreCase))
            {
                return ToPortablePath(sourcePath);
            }

            var targetPath = UniquePath(Path.Combine(TokenImagesFolder, Path.GetFileName(sourcePath)));
            File.Copy(sourcePath, targetPath);
            return ToPortablePath(targetPath);
        }

        public static string ToPortablePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                var root = Path.GetFullPath(RootFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return fullPath.Substring(root.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/');
                }
            }
            catch (Exception)
            {
                return path;
            }

            return path;
        }

        public static string ResolveUserDataPath(string path, string fallbackFolderName)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
            {
                var relativePart = path.Substring("Assets".Length).TrimStart('/', '\\');
                return Path.Combine(Application.dataPath, relativePart);
            }

            if (!Path.IsPathRooted(path))
            {
                return Path.Combine(RootFolder, path);
            }

            if (File.Exists(path) || string.IsNullOrWhiteSpace(fallbackFolderName))
            {
                return path;
            }

            var migratedPath = Path.Combine(RootFolder, fallbackFolderName, Path.GetFileName(path));
            return File.Exists(migratedPath) ? migratedPath : path;
        }

        public static string ToPortableUserDataPath(string path, string fallbackFolderName)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return ToPortablePath(ResolveUserDataPath(path, fallbackFolderName));
        }

        /// <summary>
        /// Resolves a built-in asset path, portable RPGTable path, or legacy absolute path
        /// to a disk path so that File.Exists / File.ReadAllBytes work at runtime.
        /// </summary>
        private static string ResolveAbsolutePath(string path)
        {
            var resolvedPath = ResolveUserDataPath(path, TokenImagesFolderName);
            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
            {
                return resolvedPath;
            }

            return ResolveUserDataPath(path, null);
        }

        public static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

#if UNITY_EDITOR
            if (path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", System.StringComparison.OrdinalIgnoreCase))
            {
                var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (editorSprite != null)
                {
                    return editorSprite;
                }
            }
#endif

            var resolvedPath = ResolveAbsolutePath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                // Fallback: If it's a built-in asset path (starting with Assets/) or does not exist on disk,
                // try to load it from Resources/image/ using the filename.
                var filename = Path.GetFileNameWithoutExtension(path);
                var sprites = Resources.LoadAll<Sprite>("image/" + filename);
                if (sprites != null && sprites.Length > 0)
                {
                    return sprites[0];
                }

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
