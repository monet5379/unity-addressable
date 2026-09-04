using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// 파일명 → Addressables 주소 Lookup. PathMeta는 Resources/Data/PathMetaData.json.
    /// B: Addressables만 스캔. Resources 이중 스캔은 F.
    /// </summary>
    public static class PathManager
    {
        public const string AddressableFolderName = "Addressables";
        public const string TargetPath = "Resources/Data/PathMetaData.json";

        public static readonly HashSet<string> IgnoreExtensionSet = new()
        {
            ".meta",
            ".shader",
            ".cginc",
            ".text",
        };

        public static readonly HashSet<string> IgnoreDirectorySet = new();

        private static Dictionary<string, string> _database = new();

        public static int LoadedEntryCount => _database.Count;

        public static string ToUnixPath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string Lookup(string fileNameWithExtension)
        {
            if (string.IsNullOrEmpty(fileNameWithExtension))
            {
                return null;
            }

            if (_database.Count == 0)
            {
                Load();
            }

            if (_database.TryGetValue(fileNameWithExtension, out string path))
            {
                return path;
            }

            Debug.LogWarning(
                $"[AddressableLayout] PathMeta miss: '{fileNameWithExtension}'. " +
                "Run Tools → Addressable Layout → Refresh Paths.");
            return null;
        }

        public static void Load()
        {
            TextAsset textAsset = UnityEngine.Resources.Load<TextAsset>("Data/PathMetaData");
            if (textAsset == null)
            {
                Debug.LogError(
                    "[AddressableLayout] PathMetaData.json missing under Resources/Data/. " +
                    "Run Tools → Addressable Layout → Refresh Paths.");
                _database = new Dictionary<string, string>();
                return;
            }

            try
            {
                _database = JsonUtility.FromJson<StringKeyedMap>(textAsset.text).ToDictionary()
                            ?? new Dictionary<string, string>();
                if (_database.Count > 0)
                {
                    Debug.Log($"[AddressableLayout] PathMeta loaded. entries={_database.Count}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLayout] PathMeta parse failed: {e}");
                _database = new Dictionary<string, string>();
            }
        }

        public static bool CheckLoaded()
        {
            return _database != null && _database.Count > 0 && HasPathMetaDataFile();
        }

        public static bool IsAddressablePath(string path)
        {
            return !string.IsNullOrEmpty(path)
                   && path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Assets/Addressables 스캔 후 PathMetaData.json 갱신. 중복 파일명은 경고하고 첫 path만 유지.
        /// </summary>
        public static int UpdatePathMetadata()
        {
            _database.Clear();

            string addressableRoot = Path.Combine(Application.dataPath, AddressableFolderName);
            if (!Directory.Exists(addressableRoot))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] Missing folder: Assets/{AddressableFolderName}. PathMeta will be empty.");
            }
            else
            {
                string[] addressableFilePaths =
                    Directory.GetFiles(addressableRoot, "*.*", SearchOption.AllDirectories);
                foreach (string filePath in addressableFilePaths)
                {
                    AddAddressablePath(filePath);
                }
            }

            string text = JsonUtility.ToJson(new StringKeyedMap(_database));
            WritePathMetaDataFile(text);
            Debug.Log($"[AddressableLayout] PathMeta refreshed. entries={_database.Count}");
            return _database.Count;
        }

        public static string FindPrefabPath(string name) => FindPathWithExtension(name, ".prefab");

        public static string FindAtlasPath(string name) => FindPathWithExtension(name, ".spriteatlas");

        public static string FindImagePath(string name) => FindPathWithExtension(name, ".png");

        public static string FindMaterialPath(string name) => FindPathWithExtension(name, ".mat");

        public static string FindAssetPath(string name) => FindPathWithExtension(name, ".asset");

        public static string FindVideoPath(string name) => FindPathWithExtension(name, ".mp4");

        private static string FindPathWithExtension(string name, string extension)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(extension))
            {
                return string.Empty;
            }

            string normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";
            string cleanName = name.EndsWith(normalizedExtension, StringComparison.OrdinalIgnoreCase)
                ? name[..^normalizedExtension.Length]
                : name;

            return Lookup($"{cleanName}{normalizedExtension}") ?? string.Empty;
        }

        private static void AddAddressablePath(string filePath)
        {
            if (CheckIgnore(filePath))
            {
                return;
            }

            string fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("."))
            {
                return;
            }

            if (_database.ContainsKey(fileName))
            {
                // ponytail: 첫 path 유지. F에서 Resources 우선으로 확장.
                Debug.LogWarning(
                    $"[AddressableLayout] Duplicate filename in PathMeta; keeping first path. name={fileName}");
                return;
            }

            string assetsRoot = ToUnixPath(Application.dataPath);
            string normalizedFilePath = ToUnixPath(filePath);
            string addressablePath = $"Assets{normalizedFilePath[assetsRoot.Length..]}";
            _database.Add(fileName, addressablePath);
        }

        private static bool CheckIgnore(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (IgnoreExtensionSet.Contains(extension))
            {
                return true;
            }

            foreach (string ignoreDirectory in IgnoreDirectorySet)
            {
                if (filePath.Contains(ignoreDirectory))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPathMetaDataFile()
        {
            return File.Exists(Path.Combine(Application.dataPath, TargetPath));
        }

        private static void WritePathMetaDataFile(string text)
        {
            string resourcesPath = Path.Combine(Application.dataPath, TargetPath);
            string resourcesDirectory = Path.GetDirectoryName(resourcesPath);
            if (!string.IsNullOrEmpty(resourcesDirectory) && !Directory.Exists(resourcesDirectory))
            {
                Directory.CreateDirectory(resourcesDirectory);
            }

            File.WriteAllText(resourcesPath, text, Encoding.UTF8);
        }
    }
}
