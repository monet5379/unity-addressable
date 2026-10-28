using System;
using System.Collections.Generic;
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

        public static IList<T> LoadResourcesByLabelForEditor<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning(
                    "[AddressableLayout] AddressableAssetSettings 없음 (ForEditor 라벨 로드).");
                return Array.Empty<T>();
            }

            List<T> results = new();
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

                    T asset = AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath);
                    if (asset != null)
                    {
                        results.Add(asset);
                    }
                }
            }

            return results;
        }
    }
}
