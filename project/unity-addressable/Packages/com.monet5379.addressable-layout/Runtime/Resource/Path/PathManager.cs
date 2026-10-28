using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AddressableLayout.Resource
{
    public static class PathManager
    {
        public const string AddressableFolderName = "Addressables";
        public const string ResourcesFolderName = "Resources";
        public const string TargetPath = "Resources/Data/PathMetaData.json";

        private const string RuntimeLoadKey = "Data/PathMetaData";

        public static readonly HashSet<string> IgnoreExtensionSet = new()
        {
            ".meta",
            ".shader",
            ".cginc",
            ".text",
        };

        public static readonly HashSet<string> IgnoreDirectorySet = new();

        private static Dictionary<string, string> _database = new();
        private static bool _isLoaded;

        public static int LoadedEntryCount => _database.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _database = new Dictionary<string, string>();
            _isLoaded = false;
        }

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

            if (TryLookup(fileNameWithExtension, out string path))
            {
                return path;
            }

            Debug.LogWarning(
                $"[AddressableLayout] PathMeta 조회 실패: '{fileNameWithExtension}'. " +
                "Tools → Addressable Layout → 레이아웃 도구에서 갱신 실행.");
            return null;
        }

        public static bool TryLookup(string fileNameWithExtension, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(fileNameWithExtension))
            {
                return false;
            }

            EnsureLoaded();
            return _database.TryGetValue(fileNameWithExtension, out path);
        }

        public static void Load()
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;

            TextAsset textAsset = UnityEngine.Resources.Load<TextAsset>(RuntimeLoadKey);
            if (textAsset == null)
            {
                Debug.LogError(
                    "[AddressableLayout] Resources/Data/ 아래 PathMetaData.json 없음. " +
                    "Tools → Addressable Layout → 레이아웃 도구에서 갱신 실행.");
                _database = new Dictionary<string, string>();
                return;
            }

            _database = ParseDatabase(textAsset.text);
            UnityEngine.Resources.UnloadAsset(textAsset);

            if (_database.Count == 0)
            {
                Debug.LogWarning(
                    "[AddressableLayout] PathMeta 항목이 0개이거나 파싱에 실패했습니다. " +
                    "Tools → Addressable Layout → 레이아웃 도구에서 갱신 실행.");
                return;
            }

            Debug.Log($"[AddressableLayout] PathMeta 로드 완료. entries={_database.Count}");
        }

        internal static Dictionary<string, string> ParseDatabase(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                StringKeyedMap map = JsonUtility.FromJson<StringKeyedMap>(json);
                return map?.ToDictionary() ?? new Dictionary<string, string>();
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                Load();
            }
        }

        public static bool CheckLoaded()
        {
            return _isLoaded && _database != null && _database.Count > 0;
        }

        public static bool IsAddressablePath(string path)
        {
            return !string.IsNullOrEmpty(path)
                   && path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        public static int UpdatePathMetadata()
        {
            _database.Clear();

            string resourcesRoot = Path.Combine(Application.dataPath, ResourcesFolderName);
            if (Directory.Exists(resourcesRoot))
            {
                string[] resourcesFilePaths =
                    Directory.GetFiles(resourcesRoot, "*.*", SearchOption.AllDirectories);
                foreach (string filePath in resourcesFilePaths)
                {
                    AddResourcesPath(filePath, resourcesRoot);
                }
            }

            string addressableRoot = Path.Combine(Application.dataPath, AddressableFolderName);
            if (!Directory.Exists(addressableRoot))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] 폴더 없음: Assets/{AddressableFolderName}.");
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
            _isLoaded = true;
            Debug.Log($"[AddressableLayout] PathMeta 갱신 완료. entries={_database.Count}");
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

            return TryLookup($"{cleanName}{normalizedExtension}", out string path)
                ? path
                : string.Empty;
        }

        private static void AddResourcesPath(string filePath, string resourcesRoot)
        {
            if (CheckIgnore(filePath) || IsPathMetaDataFile(filePath))
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
                Debug.LogWarning(
                    $"[AddressableLayout] PathMeta 파일명 중복; 첫 path 유지. name={fileName}");
                return;
            }

            string normalizedRoot = ToUnixPath(resourcesRoot);
            string normalizedFilePath = ToUnixPath(filePath);
            string relative = normalizedFilePath[normalizedRoot.Length..].TrimStart('/');
            string extension = Path.GetExtension(relative);
            string leafPath = string.IsNullOrEmpty(extension)
                ? relative
                : relative[..^extension.Length];

            _database.Add(fileName, leafPath);
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
                string existing = _database[fileName];
                if (!IsAddressablePath(existing))
                {
                    Debug.LogWarning(
                        $"[AddressableLayout] PathMeta 파일명 중복; Resources 우선. name={fileName}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[AddressableLayout] PathMeta 파일명 중복; 첫 path 유지. name={fileName}");
                }

                return;
            }

            string assetsRoot = ToUnixPath(Application.dataPath);
            string normalizedFilePath = ToUnixPath(filePath);
            string addressablePath = $"Assets{normalizedFilePath[assetsRoot.Length..]}";
            _database.Add(fileName, addressablePath);
        }

        private static bool IsPathMetaDataFile(string filePath)
        {
            return ToUnixPath(filePath)
                .EndsWith("/Resources/Data/PathMetaData.json", StringComparison.OrdinalIgnoreCase);
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
