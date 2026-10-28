using System.Collections.Generic;
using System.IO;
using System.Text;
using AddressableLayout.Resource;
using UnityEngine;

namespace AddressableLayout.Editor
{
    public static class PathMetaWriter
    {
        public const string AddressableFolderName = "Addressables";
        public const string ResourcesFolderName = "Resources";
        public const string TargetPath = "Resources/Data/PathMetaData.json";

        public static readonly HashSet<string> IgnoreExtensionSet = new()
        {
            ".meta",
            ".shader",
            ".cginc",
            ".text",
        };

        public static readonly HashSet<string> IgnoreDirectorySet = new();

        public static string ToUnixPath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static int Write()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(
                    "[AddressableLayout] PathMeta 갱신은 Play Mode에서 실행할 수 없습니다. Edit Mode에서 다시 시도하세요.");
                return 0;
            }

            var database = new Dictionary<string, string>();

            string resourcesRoot = Path.Combine(Application.dataPath, ResourcesFolderName);
            if (Directory.Exists(resourcesRoot))
            {
                string[] resourcesFilePaths =
                    Directory.GetFiles(resourcesRoot, "*.*", SearchOption.AllDirectories);
                foreach (string filePath in resourcesFilePaths)
                {
                    AddResourcesPath(database, filePath, resourcesRoot);
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
                    AddAddressablePath(database, filePath);
                }
            }

            string text = JsonUtility.ToJson(new StringKeyedMap(database));
            WritePathMetaDataFile(text);
            PathManager.ApplyBuiltDatabase(database);
            Debug.Log($"[AddressableLayout] PathMeta 갱신 완료. entries={database.Count}");
            return database.Count;
        }

        private static void AddResourcesPath(
            Dictionary<string, string> database,
            string filePath,
            string resourcesRoot)
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

            if (database.ContainsKey(fileName))
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

            database.Add(fileName, leafPath);
        }

        private static void AddAddressablePath(Dictionary<string, string> database, string filePath)
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

            if (database.ContainsKey(fileName))
            {
                string existing = database[fileName];
                if (!PathManager.IsAddressablePath(existing))
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
            database.Add(fileName, addressablePath);
        }

        private static bool IsPathMetaDataFile(string filePath)
        {
            return ToUnixPath(filePath)
                .EndsWith("/Resources/Data/PathMetaData.json", System.StringComparison.OrdinalIgnoreCase);
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
