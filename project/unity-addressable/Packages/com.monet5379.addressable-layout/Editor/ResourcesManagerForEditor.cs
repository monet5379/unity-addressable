using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// Edit Mode 로드 — AssetDatabase / Addressable settings.
    /// Play <see cref="AddressableAssetManager"/> 캐시를 채우지 않음.
    /// </summary>
    public static class ResourcesManagerForEditor
    {
        /// <summary>
        /// Edit Mode 단일 로드. Addressables 주소 = AssetDatabase; Resources leaf = Resources.Load.
        /// Play sync hit-only와 무관. 폴더에 에셋만 있으면 라벨 없어도 성공할 수 있음.
        /// </summary>
        public static T LoadResourceForEditor<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] Resource path is empty (ForEditor).");
                return null;
            }

            if (!PathManager.IsAddressablePath(pathOrAddress))
            {
                return UnityEngine.Resources.Load<T>(pathOrAddress);
            }

            return AssetDatabase.LoadAssetAtPath<T>(pathOrAddress);
        }

        /// <summary>
        /// Edit Mode 라벨 로드. AddressableAssetSettings 엔트리만 본다 (Play 캐시 미사용).
        /// 폴더는 있어도 라벨이 없으면 빈 목록 — Play 라벨 로드와 같은 gotcha.
        /// </summary>
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
                    "[AddressableLayout] No AddressableAssetSettings (ForEditor label load).");
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
