using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// Addressables 로드·Dictionary 캐시·릴리즈. 캐시 키는 PrimaryKey(대개 GUID).
    /// </summary>
    public sealed class AddressableAssetManager
    {
        // ponytail: PrimaryKey + 요청 키·InternalId alias. sync hit-only는 이 맵만 본다.
        private readonly Dictionary<string, object> _resourcesCache = new();
        private readonly Dictionary<string, AsyncOperationHandle> _asyncOperationHandles = new();
        private readonly Dictionary<string, List<string>> _labelToCacheKeys = new();

        public T TryGetCached<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[AddressableLayout] Resource key is empty.");
                return null;
            }

            if (_resourcesCache.TryGetValue(key, out object cached) && cached is T typed)
            {
                return typed;
            }

            return null;
        }

        public bool IsResourceLoaded(string key)
        {
            return !string.IsNullOrEmpty(key) && _resourcesCache.ContainsKey(key);
        }

        public async Task<T> LoadResourceAsync<T>(string assetGuidOrKey) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetGuidOrKey))
            {
                Debug.LogWarning("[AddressableLayout] Resource key is empty.");
                return null;
            }

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(assetGuidOrKey, typeof(T));

            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task;
                if (locations == null || locations.Count == 0)
                {
                    Debug.LogWarning($"[AddressableLayout] No Addressable location for: {assetGuidOrKey}");
                    return null;
                }

                string cacheKey = locations[0].PrimaryKey;
                if (_resourcesCache.TryGetValue(cacheKey, out object cached) && cached is T hit)
                {
                    CacheLookupAlias(assetGuidOrKey, cacheKey, hit);
                    return hit;
                }

                AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(locations[0]);
                _asyncOperationHandles[cacheKey] = loadHandle;

                T resource = await loadHandle.Task;
                if (resource == null)
                {
                    Debug.LogError($"[AddressableLayout] Addressable load failed: {cacheKey}");
                    ReleaseHandle(cacheKey);
                    return null;
                }

                _resourcesCache[cacheKey] = resource;
                CacheLookupAlias(assetGuidOrKey, cacheKey, resource);
                CacheLocationAliases(locations[0], cacheKey, resource, null);
                return resource;
            }
            catch (Exception ex)
            {
                LogLoadError(assetGuidOrKey, ex.Message);
                return null;
            }
            finally
            {
                Addressables.Release(locationsHandle);
            }
        }

        public async Task<IList<T>> LoadResourcesByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            try
            {
                AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                    Addressables.LoadResourceLocationsAsync(label, typeof(T));

                try
                {
                    IList<IResourceLocation> locations = await locationsHandle.Task;
                    if (locations == null || locations.Count == 0)
                    {
                        Debug.LogWarning(
                            $"[AddressableLayout] No {typeof(T).Name} locations for label '{label}'.");
                        return Array.Empty<T>();
                    }

                    AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(label, null);
                    _asyncOperationHandles[label] = handle;

                    IList<T> assets = await handle.Task;
                    if (assets == null || assets.Count == 0)
                    {
                        Debug.LogWarning(
                            $"[AddressableLayout] Label '{label}' loaded empty {typeof(T).Name} list.");
                        return Array.Empty<T>();
                    }

                    CacheLabelAssets(label, assets, locations);
                    return assets;
                }
                finally
                {
                    Addressables.Release(locationsHandle);
                }
            }
            catch (InvalidKeyException ex)
            {
                Debug.LogError(
                    $"[AddressableLayout] Label '{label}' not found for {typeof(T).Name}. {ex.Message}\n" +
                    "Run Tools → Addressable Layout → Demo → Register Boot Sample, and set Play Mode Script to Use Asset Database.");
                return Array.Empty<T>();
            }
            catch (Exception ex)
            {
                LogLoadError(label, ex.Message);
                return Array.Empty<T>();
            }
        }

        public void Release(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            ReleaseHandle(key);
            _resourcesCache.Remove(key);

            foreach (KeyValuePair<string, List<string>> pair in _labelToCacheKeys)
            {
                pair.Value.Remove(key);
            }
        }

        public void ReleaseLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (_labelToCacheKeys.TryGetValue(label, out List<string> keys))
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    string cacheKey = keys[i];
                    ReleaseHandle(cacheKey);
                    _resourcesCache.Remove(cacheKey);
                }

                _labelToCacheKeys.Remove(label);
            }

            ReleaseHandle(label);
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<string, AsyncOperationHandle> pair in _asyncOperationHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            _asyncOperationHandles.Clear();
            _resourcesCache.Clear();
            _labelToCacheKeys.Clear();
        }

        private void CacheLabelAssets<T>(
            string label,
            IList<T> assets,
            IList<IResourceLocation> locations) where T : UnityEngine.Object
        {
            if (!_labelToCacheKeys.TryGetValue(label, out List<string> keys))
            {
                keys = new List<string>();
                _labelToCacheKeys[label] = keys;
            }

            int count = Mathf.Min(assets.Count, locations.Count);
            for (int i = 0; i < count; i++)
            {
                string cacheKey = locations[i].PrimaryKey;
                T asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                if (!_resourcesCache.ContainsKey(cacheKey))
                {
                    _resourcesCache[cacheKey] = asset;
                }

                TrackLabelKey(keys, cacheKey);
                CacheLocationAliases(locations[i], cacheKey, asset, keys);
            }

            Debug.Log(
                $"[AddressableLayout] Label '{label}' cached {assets.Count} {typeof(T).Name} asset(s).");
        }

        // PathMeta 주소와 PrimaryKey(GUID 등)가 어긋나도 sync hit-only 조회가 되게 한다.
        private void CacheLookupAlias(string requestKey, string primaryKey, object resource)
        {
            if (string.IsNullOrEmpty(requestKey)
                || string.Equals(requestKey, primaryKey, StringComparison.Ordinal))
            {
                return;
            }

            _resourcesCache[requestKey] = resource;
        }

        private void CacheLocationAliases(
            IResourceLocation location,
            string primaryKey,
            object resource,
            List<string> labelKeys)
        {
            if (location == null || resource == null)
            {
                return;
            }

            string internalId = location.InternalId;
            if (string.IsNullOrEmpty(internalId)
                || string.Equals(internalId, primaryKey, StringComparison.Ordinal))
            {
                return;
            }

            _resourcesCache[internalId] = resource;
            TrackLabelKey(labelKeys, internalId);
        }

        private static void TrackLabelKey(List<string> keys, string key)
        {
            if (keys == null || string.IsNullOrEmpty(key) || keys.Contains(key))
            {
                return;
            }

            keys.Add(key);
        }

        private void ReleaseHandle(string key)
        {
            if (!_asyncOperationHandles.TryGetValue(key, out AsyncOperationHandle handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _asyncOperationHandles.Remove(key);
        }

        private static void LogLoadError(string key, string errorMessage)
        {
            if (errorMessage.IndexOf("Invalid path", StringComparison.Ordinal) >= 0
                || errorMessage.IndexOf("settings.json", StringComparison.Ordinal) >= 0)
            {
                Debug.LogError(
                    $"[AddressableLayout] Addressable load failed: {key}. " +
                    "Build Addressables or set Play Mode Script to Use Asset Database. " +
                    "Tools → Addressable Layout → Demo → Register Boot Sample.");
                return;
            }

            Debug.LogError($"[AddressableLayout] Addressable error for '{key}': {errorMessage}");
        }
    }
}
