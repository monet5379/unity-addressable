using System;
using System.Collections.Generic;
using AddressableLayout.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressableLayout.Resource
{
    public static class ResourcesManagerForEditor
    {
        public static T LoadResourceForEditor<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] 리소스 경로가 비어 있습니다 (ForEditor).");
                return null;
            }

            if (!PathManager.IsAddressablePath(pathOrAddress))
            {
                return UnityEngine.Resources.Load<T>(pathOrAddress);
            }

            return AssetDatabase.LoadAssetAtPath<T>(pathOrAddress);
        }

        public static void ForEachLabeledAsset(string label, Action<string, UnityEngine.Object> onAsset)
        {
            if (string.IsNullOrEmpty(label) || onAsset == null)
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning(
                    "[AddressableLayout] AddressableAssetSettings 없음 (ForEditor 라벨 로드).");
                return;
            }

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || !entry.labels.Contains(label))
                    {
                        continue;
                    }

                    string assetPath = entry.AssetPath;
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset == null)
                    {
                        continue;
                    }

                    onAsset(PathMetaWriter.ToUnixPath(assetPath), asset);
                }
            }
        }

        public static IList<T> LoadResourcesByLabelForEditor<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            List<T> results = new();
            ForEachLabeledAsset(label, (_, asset) =>
            {
                if (asset is T typed)
                {
                    results.Add(typed);
                }
            });

            return results;
        }
    }
}
