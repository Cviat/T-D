using System;
using System.Collections.Generic;
using System.IO;
using RPGTable.TokenEditor;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGTable.MapEditor
{
    public static class UserElementAssetStore
    {
        private const string UserElementsFolderName = "UserElements";
        private static readonly string[] ImageFallbackFolders = { UserElementsFolderName, "Maps", "CampaignCovers", "TokenImages" };
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static string ElementsFolder
        {
            get
            {
                var path = Path.Combine(UserTokenStore.RootFolder, UserElementsFolderName);
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
            return UserTokenStore.ToPortablePath(targetPath);
        }

        public static Sprite LoadSprite(string path)
        {
            var resolvedPath = ResolveImagePath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            if (spriteCache.TryGetValue(resolvedPath, out var cached) && cached != null)
            {
                return cached;
            }

            var bytes = File.ReadAllBytes(resolvedPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(resolvedPath);
            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[resolvedPath] = sprite;
            return sprite;
        }

        public static bool DeleteImage(string path)
        {
            var resolvedPath = ResolveImagePath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath) || !IsSupportedImage(resolvedPath))
            {
                return false;
            }

            File.Delete(resolvedPath);
            spriteCache.Remove(resolvedPath);
            return true;
        }

        public static string ToPortableImagePath(string path)
        {
            return UserTokenStore.ToPortableUserDataPath(path, UserElementsFolderName);
        }

        private static string ResolveImagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var resolvedPath = UserTokenStore.ResolveUserDataPath(path, UserElementsFolderName);

            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
            {
                return resolvedPath;
            }

            if (!Path.IsPathRooted(path))
            {
                return resolvedPath;
            }

            foreach (var folder in ImageFallbackFolders)
            {
                resolvedPath = UserTokenStore.ResolveUserDataPath(path, folder);
                if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
                {
                    return resolvedPath;
                }
            }

            return resolvedPath;
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

    [Serializable]
    public sealed class SavedMapData
    {
        public string name;
        public string previewImagePath;
        public SavedMapElementData[] elements;
        public SavedMapExitPointData[] exitPoints;
        public SavedMapSpawnZoneData[] spawnZones;
    }

    [Serializable]
    public sealed class SavedMapElementData
    {
        public string imagePath;
        public Vector3 position;
        public Vector3 scale;
    }

    [Serializable]
    public sealed class SavedMapExitPointData
    {
        public string id;
        public string name;
        public Vector3 position;
        public Vector2 size;
    }

    [Serializable]
    public sealed class SavedMapSpawnZoneData
    {
        public string id;
        public string name;
        public Vector3 position;
        public Vector2 size;
    }

    public static class UserMapStore
    {
        private const string Extension = ".json";
        private const string MapsFolderName = "Maps";

        public static string MapsFolder
        {
            get
            {
                var path = Path.Combine(UserTokenStore.RootFolder, MapsFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static IReadOnlyList<string> GetMapPaths()
        {
            Directory.CreateDirectory(MapsFolder);
            var result = new List<string>(Directory.GetFiles(MapsFolder, $"*{Extension}"));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static string SaveMap(
            string mapName,
            IReadOnlyList<PlacedMapElement> elements,
            IReadOnlyList<MapExitPoint> exitPoints,
            IReadOnlyList<MapSpawnZone> spawnZones)
        {
            var safeName = SanitizeName(mapName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            var data = new SavedMapData
            {
                name = safeName,
                previewImagePath = SavePreviewImage(safeName, elements),
                elements = new SavedMapElementData[elements.Count],
                exitPoints = new SavedMapExitPointData[exitPoints.Count],
                spawnZones = new SavedMapSpawnZoneData[spawnZones.Count]
            };

            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                data.elements[i] = new SavedMapElementData
                {
                    imagePath = UserElementAssetStore.ToPortableImagePath(element.SourceImagePath),
                    position = element.transform.position,
                    scale = element.transform.localScale
                };
            }

            for (var i = 0; i < exitPoints.Count; i++)
            {
                var exitPoint = exitPoints[i];
                data.exitPoints[i] = new SavedMapExitPointData
                {
                    id = exitPoint.Id,
                    name = exitPoint.DisplayName,
                    position = exitPoint.transform.position,
                    size = exitPoint.Size
                };
            }

            for (var i = 0; i < spawnZones.Count; i++)
            {
                var spawnZone = spawnZones[i];
                data.spawnZones[i] = new SavedMapSpawnZoneData
                {
                    id = spawnZone.Id,
                    name = spawnZone.DisplayName,
                    position = spawnZone.transform.position,
                    size = spawnZone.Size
                };
            }

            var path = Path.Combine(MapsFolder, $"{safeName}{Extension}");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return UserTokenStore.ToPortablePath(path);
        }

        public static SavedMapData LoadMap(string path)
        {
            var resolvedPath = UserTokenStore.ResolveUserDataPath(path, MapsFolderName);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            return JsonUtility.FromJson<SavedMapData>(File.ReadAllText(resolvedPath));
        }

        public static string GetDisplayName(string path)
        {
            var data = LoadMap(path);
            return !string.IsNullOrWhiteSpace(data?.name)
                ? data.name
                : Path.GetFileNameWithoutExtension(path);
        }

        public static Sprite LoadPreviewSprite(string mapPath)
        {
            var data = LoadMap(mapPath);

            if (!string.IsNullOrWhiteSpace(data?.previewImagePath))
            {
                var preview = UserElementAssetStore.LoadSprite(data.previewImagePath);

                if (preview != null)
                {
                    return preview;
                }
            }

            return null;
        }

        private static string SavePreviewImage(string safeName, IReadOnlyList<PlacedMapElement> elements)
        {
            if (elements.Count == 0)
            {
                return null;
            }

            if (!TryGetMapBounds(elements, out var bounds))
            {
                return null;
            }

            const int maxSize = 512;
            const int minSize = 180;
            var aspect = Mathf.Max(0.1f, bounds.size.x / Mathf.Max(bounds.size.y, 0.1f));
            var width = aspect >= 1f ? maxSize : Mathf.RoundToInt(maxSize * aspect);
            var height = aspect >= 1f ? Mathf.RoundToInt(maxSize / aspect) : maxSize;
            width = Mathf.Clamp(width, minSize, maxSize);
            height = Mathf.Clamp(height, minSize, maxSize);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var background = new Color32(30, 28, 24, 255);
            var pixels = new Color32[width * height];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            texture.SetPixels32(pixels);

            foreach (var element in elements)
            {
                DrawElementPreview(texture, bounds, element);
            }

            texture.Apply();

            var path = Path.Combine(MapsFolder, $"{safeName}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            return UserTokenStore.ToPortablePath(path);
        }

        private static bool TryGetMapBounds(IReadOnlyList<PlacedMapElement> elements, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            foreach (var element in elements)
            {
                var renderer = element.GetComponent<SpriteRenderer>();

                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            var padding = Mathf.Max(bounds.size.x, bounds.size.y) * 0.06f;
            bounds.Expand(new Vector3(padding, padding, 0f));
            return true;
        }

        private static void DrawElementPreview(Texture2D target, Bounds mapBounds, PlacedMapElement element)
        {
            var renderer = element.GetComponent<SpriteRenderer>();

            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            var sprite = renderer.sprite;
            var source = sprite.texture;
            var rect = sprite.textureRect;
            var elementBounds = renderer.bounds;
            var min = WorldToPixel(target, mapBounds, elementBounds.min);
            var max = WorldToPixel(target, mapBounds, elementBounds.max);
            var startX = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, target.width - 1);
            var endX = Mathf.Clamp(Mathf.CeilToInt(max.x), 0, target.width - 1);
            var startY = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, target.height - 1);
            var endY = Mathf.Clamp(Mathf.CeilToInt(max.y), 0, target.height - 1);

            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    var u = Mathf.InverseLerp(min.x, max.x, x);
                    var v = Mathf.InverseLerp(min.y, max.y, y);
                    var sourceX = Mathf.Clamp(Mathf.FloorToInt(rect.x + rect.width * u), 0, source.width - 1);
                    var sourceY = Mathf.Clamp(Mathf.FloorToInt(rect.y + rect.height * v), 0, source.height - 1);
                    var color = source.GetPixel(sourceX, sourceY);

                    if (color.a <= 0.01f)
                    {
                        continue;
                    }

                    var current = target.GetPixel(x, y);
                    target.SetPixel(x, y, Color.Lerp(current, color, color.a));
                }
            }
        }

        private static Vector2 WorldToPixel(Texture2D texture, Bounds mapBounds, Vector3 world)
        {
            return new Vector2(
                Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, world.x) * (texture.width - 1),
                Mathf.InverseLerp(mapBounds.min.y, mapBounds.max.y, world.y) * (texture.height - 1));
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

    [Serializable]
    public sealed class SavedCampaignData
    {
        public string name;
        public string description;
        public string coverImagePath;
        public string startMapId;
        public SavedCampaignMapNodeData[] maps;
        public SavedCampaignLinkData[] links;
    }

    [Serializable]
    public sealed class SavedCampaignTokenData
    {
        public string displayName;
        public string characterPath;
        public string tokenPath;
        public Vector2 worldPosition;
        public RPGTable.Core.TokenTeam team;
        public bool visibleToPlayers;
    }

    [Serializable]
    public sealed class SavedCampaignMapNodeData
    {
        public string id;
        public string mapPath;
        public Vector2 boardPosition;
        public SavedCampaignTokenData[] presetTokens;
    }

    [Serializable]
    public sealed class SavedCampaignLinkData
    {
        public string fromMapId;
        public string fromExitId;
        public string toMapId;
        public string toExitId;
    }

    public static class UserCampaignStore
    {
        private const string Extension = ".json";
        private const string CampaignsFolderName = "Campaigns";
        private const string CampaignCoversFolderName = "CampaignCovers";

        public static string CampaignsFolder
        {
            get
            {
                var path = Path.Combine(UserTokenStore.RootFolder, CampaignsFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CampaignCoversFolder
        {
            get
            {
                var path = Path.Combine(UserTokenStore.RootFolder, CampaignCoversFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static IReadOnlyList<string> GetCampaignPaths()
        {
            Directory.CreateDirectory(CampaignsFolder);
            var result = new List<string>(Directory.GetFiles(CampaignsFolder, $"*{Extension}"));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static string SaveCampaign(string campaignName, SavedCampaignData data)
        {
            var safeName = SanitizeName(campaignName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            data.name = safeName;
            data.coverImagePath = UserTokenStore.ToPortableUserDataPath(data.coverImagePath, CampaignCoversFolderName);
            NormalizeCampaignMapPaths(data);
            var path = Path.Combine(CampaignsFolder, $"{safeName}{Extension}");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return UserTokenStore.ToPortablePath(path);
        }

        public static string ImportCoverImageWithDialog()
        {
#if UNITY_EDITOR
            var sourcePath = EditorUtility.OpenFilePanel("Import campaign cover", "", "png,jpg,jpeg");

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            return ImportCoverImage(sourcePath);
#else
            Debug.LogWarning("Runtime file picker is not implemented yet.");
            return null;
#endif
        }

        public static string ImportCoverImage(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            var extension = Path.GetExtension(sourcePath);

            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var targetPath = UniquePath(Path.Combine(CampaignCoversFolder, Path.GetFileName(sourcePath)));
            File.Copy(sourcePath, targetPath);
            return UserTokenStore.ToPortablePath(targetPath);
        }

        public static Sprite LoadCoverSprite(string path)
        {
            return UserElementAssetStore.LoadSprite(path);
        }

        public static SavedCampaignData LoadCampaign(string path)
        {
            var resolvedPath = UserTokenStore.ResolveUserDataPath(path, CampaignsFolderName);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            return JsonUtility.FromJson<SavedCampaignData>(File.ReadAllText(resolvedPath));
        }

        public static string GetDisplayName(string path)
        {
            var data = LoadCampaign(path);
            return !string.IsNullOrWhiteSpace(data?.name)
                ? data.name
                : Path.GetFileNameWithoutExtension(path);
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

        private static void NormalizeCampaignMapPaths(SavedCampaignData data)
        {
            if (data?.maps == null)
            {
                return;
            }

            foreach (var map in data.maps)
            {
                if (map == null)
                {
                    continue;
                }

                map.mapPath = UserTokenStore.ToPortableUserDataPath(map.mapPath, "Maps");

                if (map.presetTokens == null)
                {
                    continue;
                }

                foreach (var presetToken in map.presetTokens)
                {
                    if (presetToken == null)
                    {
                        continue;
                    }

                    presetToken.characterPath = UserTokenStore.ToPortableUserDataPath(presetToken.characterPath, "Characters");
                    presetToken.tokenPath = UserTokenStore.ToPortableUserDataPath(presetToken.tokenPath, "Tokens");
                }
            }
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
