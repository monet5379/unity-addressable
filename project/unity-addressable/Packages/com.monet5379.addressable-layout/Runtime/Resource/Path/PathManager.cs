using System;
using System.Collections.Generic;
using UnityEngine;

namespace AddressableLayout.Resource
{
    public static class PathManager
    {
        public const string PathMetaResourcesLoadKey = "Data/PathMetaData";

        private static Dictionary<string, string> _database = new();
        private static bool _isLoaded;

        public static int LoadedEntryCount => _database.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _database = new Dictionary<string, string>();
            _isLoaded = false;
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

            TextAsset textAsset = UnityEngine.Resources.Load<TextAsset>(PathMetaResourcesLoadKey);
            if (textAsset == null)
            {
                Debug.LogError(
                    $"[AddressableLayout] Resources/{PathMetaResourcesLoadKey}.json 없음. " +
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

        internal static void ApplyBuiltDatabase(Dictionary<string, string> database)
        {
            _database = database ?? new Dictionary<string, string>();
            _isLoaded = true;
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
    }
}
