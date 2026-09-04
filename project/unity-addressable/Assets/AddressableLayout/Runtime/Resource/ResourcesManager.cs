using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// PathManager + AddressableAssetManager Play facade.
    /// Addressables 경로: 동기 LoadResource = 캐시 hit만.
    /// Resources leaf: Resources.Load (이관 중 이중 경로).
    /// SpawnPrefab / Despawn: 이름 → Path → Instantiate / Destroy.
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
        /// Play 동기 로드. Addressables = 캐시 hit만. Resources leaf = Resources.Load.
        /// </summary>
        public static T LoadResource<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] Resource path is empty.");
                return null;
            }

            if (!PathManager.IsAddressablePath(pathOrAddress))
            {
                return UnityEngine.Resources.Load<T>(pathOrAddress);
            }

            return Addressables.TryGetCached<T>(pathOrAddress);
        }

        /// <summary>
        /// 파일명으로 PathMeta Lookup 후 로드.
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

            if (!PathManager.IsAddressablePath(pathOrAddress))
            {
                return await Task.FromResult(UnityEngine.Resources.Load<T>(pathOrAddress));
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

        /// <summary>
        /// 프리팹 이름 → PathMeta → sync Load → Instantiate.
        /// Addressables 경로는 캐시 hit 전제 (miss면 null).
        /// </summary>
        public static GameObject SpawnPrefab(string prefabName, Transform parent = null)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogWarning("[AddressableLayout] SpawnPrefab: prefab name is empty.");
                return null;
            }

            string path = PathManager.FindPrefabPath(prefabName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] SpawnPrefab: PathMeta miss for '{prefabName}'. " +
                    "Run Tools → Addressable Layout → Refresh Paths.");
                return null;
            }

            GameObject prefab = LoadResource<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[AddressableLayout] SpawnPrefab: sync miss for '{prefabName}' path={path}. " +
                    "Preload by label (Addressables) or ensure Resources leaf exists.");
                return null;
            }

            return parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
        }

        /// <summary>
        /// SpawnPrefab으로 만든 인스턴스 Destroy (풀링 없음).
        /// </summary>
        public static void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }
    }
}
