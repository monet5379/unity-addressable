using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// PathManager + AddressableAssetManager Play facade.
    /// </summary>
    public static class ResourcesManager
    {
        private static readonly AddressableAssetManager Addressables = new();

        private static IPrefabInstanceProvider _instanceProvider = InstantiateProvider.Instance;

        public static AddressableAssetManager AssetManager => Addressables;

        public static void SetInstanceProvider(IPrefabInstanceProvider provider)
        {
            _instanceProvider = provider ?? InstantiateProvider.Instance;
        }

        public static bool IsResourceLoaded(string pathOrAddress)
        {
            return Addressables.IsResourceLoaded(pathOrAddress);
        }

        public static T LoadResource<T>(string pathOrAddress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(pathOrAddress))
            {
                Debug.LogWarning("[AddressableLayout] 리소스 경로가 비어 있습니다.");
                return null;
            }

            if (!PathManager.IsAddressablePath(pathOrAddress))
            {
                return UnityEngine.Resources.Load<T>(pathOrAddress);
            }

            return Addressables.TryGetCached<T>(pathOrAddress);
        }

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
                Debug.LogWarning("[AddressableLayout] 리소스 경로가 비어 있습니다.");
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
                    $"[AddressableLayout] LoadResourceAsync 실패. path={pathOrAddress}, {ex.Message}");
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
                    $"[AddressableLayout] 라벨 로드 실패: {label}, {ex.Message}");
                return Array.Empty<T>();
            }
        }

        public static void Release(string key) => Addressables.Release(key);

        public static void ReleaseLabel(string label) => Addressables.ReleaseLabel(label);

        [Obsolete("Use ReleaseLabel instead. ReleaseLabelRef will be removed in a future release.")]
        public static bool ReleaseLabelRef(string label) => Addressables.ReleaseLabelRef(label);

        public static void ReleaseAll() => Addressables.ReleaseAll();

        public static void PreloadTypeLabels(params string[] labels)
        {
            PreloadTypeLabels(labels, failOnEmptyInDev: false);
        }

        public static void PreloadTypeLabels(string[] labels, bool failOnEmptyInDev)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                Addressables.PreloadLabelSync<UnityEngine.Object>(label, failOnEmptyInDev);
            }
        }

        public static Task<IList<T>> EnterPlaceAsync<T>(string placeLabel) where T : UnityEngine.Object
        {
            return LoadResourcesByLabelAsync<T>(placeLabel);
        }

        public static void LeavePlace(string placeLabel) => ReleaseLabel(placeLabel);

        public static GameObject SpawnPrefab(string prefabName, Transform parent = null)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogWarning("[AddressableLayout] SpawnPrefab: 프리팹 이름이 비어 있습니다.");
                return null;
            }

            string path = PathManager.FindPrefabPath(prefabName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning(
                    $"[AddressableLayout] SpawnPrefab: PathMeta 조회 실패 '{prefabName}'. " +
                    "Tools → Addressable Layout → 레이아웃 도구에서 갱신 실행.");
                return null;
            }

            GameObject prefab = LoadResource<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[AddressableLayout] SpawnPrefab: 동기 miss '{prefabName}' path={path}. " +
                    "라벨 preload(Addressables) 또는 Resources leaf 존재 여부를 확인하세요.");
                return null;
            }

            return _instanceProvider.Spawn(prefab, parent);
        }

        public static void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            _instanceProvider.Despawn(instance);
        }

        private sealed class InstantiateProvider : IPrefabInstanceProvider
        {
            internal static readonly InstantiateProvider Instance = new();

            public GameObject Spawn(GameObject prefab, Transform parent) =>
                parent != null
                    ? UnityEngine.Object.Instantiate(prefab, parent)
                    : UnityEngine.Object.Instantiate(prefab);

            public void Despawn(GameObject instance) => UnityEngine.Object.Destroy(instance);
        }
    }
}
