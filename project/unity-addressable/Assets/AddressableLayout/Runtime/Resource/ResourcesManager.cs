using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// PathManager + AddressableAssetManager Play facade.
    /// 동기 LoadResource = 캐시 hit만. miss는 null → 라벨 preload 또는 LoadResourceAsync.
    /// </summary>
    public static class ResourcesManager
    {
        private static readonly AddressableAssetManager Addressables = new();

        public static AddressableAssetManager AssetManager => Addressables;

        public static bool IsResourceLoaded(string pathOrAddress)
        {
            return Addressables.IsResourceLoaded(pathOrAddress);
        }

        /// <summary>
        /// Play 동기 로드: 캐시 hit만. Addressables sync fetch 하지 않음.
        /// </summary>
        public static T LoadResource<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] Resource path is empty.");
                return null;
            }

            return Addressables.TryGetCached<T>(pathOrAddress);
        }

        /// <summary>
        /// 파일명으로 PathMeta Lookup 후 캐시 hit만 반환.
        /// </summary>
        public static T LoadResourceByName<T>(string fileNameWithExtension) where T : UnityEngine.Object
        {
            string path = PathManager.Lookup(fileNameWithExtension);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return LoadResource<T>(path);
        }

        public static async Task<T> LoadResourceAsync<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] Resource path is empty.");
                return null;
            }

            try
            {
                return await Addressables.LoadResourceAsync<T>(pathOrAddress);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[AddressableLayout] LoadResourceAsync failed. path={pathOrAddress}, {ex.Message}");
                return null;
            }
        }

        public static async Task<IList<T>> LoadResourcesByLabelAsync<T>(string label)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            try
            {
                return await Addressables.LoadResourcesByLabelAsync<T>(label);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[AddressableLayout] Label load failed: {label}, {ex.Message}");
                return Array.Empty<T>();
            }
        }

        public static void Release(string key) => Addressables.Release(key);

        public static void ReleaseLabel(string label) => Addressables.ReleaseLabel(label);

        public static void ReleaseAll() => Addressables.ReleaseAll();

        /// <summary>
        /// 지역(place) 라벨 preload. 퇴장은 LeavePlace.
        /// </summary>
        public static Task<IList<T>> EnterPlaceAsync<T>(string placeLabel) where T : UnityEngine.Object
        {
            return LoadResourcesByLabelAsync<T>(placeLabel);
        }

        public static void LeavePlace(string placeLabel) => ReleaseLabel(placeLabel);
    }
}
