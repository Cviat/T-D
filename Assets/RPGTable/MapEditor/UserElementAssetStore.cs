using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGTable.MapEditor
{
    public static class UserElementAssetStore
    {
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg" };

        public static string ElementsFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, "RPGTable", "UserElements");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static IReadOnlyList<string> GetImagePaths()
        {
            Directory.CreateDirectory(ElementsFolder);
            var result = new List<string>();

            foreach (var file in Directory.GetFiles(ElementsFolder))
            {
                if (IsSupportedImage(file))
                {
                    result.Add(file);
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static string ImportImageWithDialog()
        {
#if UNITY_EDITOR
            var sourcePath = EditorUtility.OpenFilePanel("Import element image", "", "png,jpg,jpeg");

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            return ImportImage(sourcePath);
#else
            Debug.LogWarning("Runtime file picker is not implemented yet. Add a standalone file browser plugin for builds.");
            return null;
#endif
        }

        public static string ImportImage(string sourcePath)
        {
            if (!File.Exists(sourcePath) || !IsSupportedImage(sourcePath))
            {
                Debug.LogWarning($"Unsupported image: {sourcePath}");
                return null;
            }

            var fileName = Path.GetFileName(sourcePath);
            var targetPath = UniquePath(Path.Combine(ElementsFolder, fileName));
            File.Copy(sourcePath, targetPath);
            return targetPath;
        }

        public static Sprite LoadSprite(string path)
        {
            if (!File.Exists(path))
            {
                return null;
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
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool IsSupportedImage(string path)
        {
            var extension = Path.GetExtension(path);

            foreach (var supported in SupportedExtensions)
            {
                if (string.Equals(extension, supported, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
    }
}
