using System.Collections.Generic;
using System.IO;
using AddressableLayout.Resource;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressableLayout.Editor
{
    public static class PathMetaValidation
    {
        public static void FindDiskViolations(List<string> violations)
        {
            if (violations == null)
            {
                return;
            }

            string addressableRoot = Path.Combine(Application.dataPath, PathMetaWriter.AddressableFolderName);
            if (!Directory.Exists(addressableRoot))
            {
                violations.Add($"Missing folder Assets/{PathMetaWriter.AddressableFolderName}.");
                return;
            }

            string assetsRootUnix = PathMetaWriter.ToUnixPath(Application.dataPath);
            string[] filePaths = Directory.GetFiles(addressableRoot, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < filePaths.Length; i++)
            {
                string filePath = filePaths[i];
                if (ShouldSkipAddressablesFile(filePath))
                {
                    continue;
                }

                string fileName = Path.GetFileName(filePath);
                string expectedPath =
                    $"Assets{PathMetaWriter.ToUnixPath(filePath)[assetsRootUnix.Length..]}";

                if (!PathManager.TryLookup(fileName, out string lookupPath)
                    || string.IsNullOrEmpty(lookupPath))
                {
                    violations.Add($"{fileName}: PathMeta miss (expected {expectedPath})");
                    continue;
                }

                if (!PathManager.IsAddressablePath(lookupPath))
                {
                    violations.Add($"{fileName}: Resources wins shadow ({lookupPath}, expected {expectedPath})");
                    continue;
                }

                if (!string.Equals(lookupPath, expectedPath, System.StringComparison.Ordinal))
                {
                    violations.Add($"{fileName}: stale PathMeta ({lookupPath}, expected {expectedPath})");
                }
            }
        }

        public static void FindLabelsWithoutEntries(IReadOnlyList<string> labels, List<string> violations)
        {
            if (labels == null || violations == null)
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    string label = labels[i];
                    if (!string.IsNullOrEmpty(label))
                    {
                        violations.Add($"라벨 '{label}': AddressableAssetSettings 없음");
                    }
                }

                return;
            }

            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                if (CountLabelEntries(settings, label) == 0)
                {
                    violations.Add($"라벨 '{label}': Addressables 엔트리 0건");
                }
            }
        }

        private static int CountLabelEntries(AddressableAssetSettings settings, string label)
        {
            int count = 0;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry != null && entry.labels.Contains(label))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool ShouldSkipAddressablesFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("."))
            {
                return true;
            }

            return PathMetaWriter.ShouldIgnoreFile(PathMetaWriter.ToUnixPath(filePath));
        }
    }
}
