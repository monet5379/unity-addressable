using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using AddressableLayout.Resource;
using UnityEngine;

[assembly: InternalsVisibleTo("AddressableLayout.EditorTests")]

namespace AddressableLayout.Editor
{
    public static class PathMetaWriter
    {
        public const string AddressableFolderName = "Addressables";
        public const string ResourcesFolderName = "Resources";
        public const string TargetPath =
            ResourcesFolderName + "/" + PathManager.PathMetaResourcesLoadKey + ".json";

        private static readonly HashSet<string> IgnoreExtensionSet = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ".meta",
            ".shader",
            ".cginc",
            ".text",
        };

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

            var database = BuildDatabase(Application.dataPath);

            string text = JsonUtility.ToJson(new StringKeyedMap(database));
            WritePathMetaDataFile(text);
            PathManager.ApplyBuiltDatabase(database);
            Debug.Log($"[AddressableLayout] PathMeta 갱신 완료. entries={database.Count}");
            return database.Count;
        }

        internal static Dictionary<string, string> BuildDatabase(string assetsRoot)
        {
            var database = new Dictionary<string, string>();

            string resourcesRoot = Path.Combine(assetsRoot, ResourcesFolderName);
            if (Directory.Exists(resourcesRoot))
            {
                string[] resourcesFilePaths =
                    Directory.GetFiles(resourcesRoot, "*.*", SearchOption.AllDirectories);
                SortPathsUnix(resourcesFilePaths);
                string normalizedResourcesRoot = ToUnixPath(resourcesRoot);
                foreach (string filePath in resourcesFilePaths)
                {
                    RegisterResourcesFile(database, filePath, normalizedResourcesRoot);
                }
            }

            string addressableRoot = Path.Combine(assetsRoot, AddressableFolderName);
            if (!Directory.Exists(addressableRoot))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] 폴더 없음: Assets/{AddressableFolderName}.");
            }
            else
            {
                string[] addressableFilePaths =
                    Directory.GetFiles(addressableRoot, "*.*", SearchOption.AllDirectories);
                SortPathsUnix(addressableFilePaths);
                string normalizedAssetsRoot = ToUnixPath(assetsRoot);
                foreach (string filePath in addressableFilePaths)
                {
                    RegisterAddressableFile(database, filePath, normalizedAssetsRoot);
                }
            }

            return database;
        }

        private static void SortPathsUnix(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = ToUnixPath(paths[i]);
            }

            System.Array.Sort(paths, System.StringComparer.Ordinal);
        }

        private static void RegisterResourcesFile(
            Dictionary<string, string> database,
            string unixFilePath,
            string normalizedResourcesRoot)
        {
            if (ShouldIgnoreFile(unixFilePath) || IsPathMetaDataFile(unixFilePath))
            {
                return;
            }

            string fileName = Path.GetFileName(unixFilePath);
            if (fileName.StartsWith("."))
            {
                return;
            }

            string relative = unixFilePath[normalizedResourcesRoot.Length..].TrimStart('/');
            string extension = Path.GetExtension(relative);
            string leafPath = string.IsNullOrEmpty(extension)
                ? relative
                : relative[..^extension.Length];

            TryRegister(database, fileName, leafPath);
        }

        private static void RegisterAddressableFile(
            Dictionary<string, string> database,
            string unixFilePath,
            string normalizedAssetsRoot)
        {
            if (ShouldIgnoreFile(unixFilePath))
            {
                return;
            }

            string fileName = Path.GetFileName(unixFilePath);
            if (fileName.StartsWith("."))
            {
                return;
            }

            string addressablePath = $"Assets{unixFilePath[normalizedAssetsRoot.Length..]}";
            TryRegister(database, fileName, addressablePath);
        }

        private static void TryRegister(Dictionary<string, string> database, string fileName, string path)
        {
            if (database.TryGetValue(fileName, out string existing))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] PathMeta 파일명 중복; kept={existing}, skipped={path}");
                return;
            }

            database.Add(fileName, path);
        }

        private static bool IsPathMetaDataFile(string filePath)
        {
            return ToUnixPath(filePath)
                .EndsWith("/" + TargetPath, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIgnoreFile(string unixFilePath)
        {
            return IgnoreExtensionSet.Contains(Path.GetExtension(unixFilePath));
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
