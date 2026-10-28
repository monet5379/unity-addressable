using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    internal sealed class DirectAssetLoader
    {
        private readonly CacheOwnership _cache;
        private readonly Dictionary<string, AsyncOperationHandle> _directHandles = new();
        private readonly Dictionary<string, Task<UnityEngine.Object>> _loadingTasks = new();
        private int _generation;

        public DirectAssetLoader(CacheOwnership cache)
        {
            _cache = cache;
        }

        public T TryGetCached<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[AddressableLayout] 리소스 키가 비어 있습니다.");
                return null;
            }

            return _cache.TryGet(key, out T typed) ? typed : null;
        }

        public bool IsResourceLoaded(string key)
        {
            return !string.IsNullOrEmpty(key) && _cache.Contains(key);
        }

        public async Task<T> LoadResourceAsync<T>(string assetGuidOrKey) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetGuidOrKey))
            {
                Debug.LogWarning("[AddressableLayout] 리소스 키가 비어 있습니다.");
                return null;
            }

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(assetGuidOrKey, typeof(T));

            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task;
                if (locations == null || locations.Count == 0)
                {
                    Debug.LogWarning($"[AddressableLayout] Addressable location 없음: {assetGuidOrKey}");
                    return null;
                }

                string primaryKey = locations[0].PrimaryKey;
                if (_cache.TryGet<T>(primaryKey, out T hit)
                    && _cache.HasOwner(primaryKey, CacheOwnership.Direct))
                {
                    _cache.AddRequestAlias(assetGuidOrKey, primaryKey);
                    return hit;
                }

                if (!_loadingTasks.TryGetValue(primaryKey, out Task<UnityEngine.Object> loading))
                {
                    loading = LoadAndCacheAsync<T>(locations[0], primaryKey);
                    _loadingTasks[primaryKey] = loading;
                }

                UnityEngine.Object loaded;
                try
                {
                    loaded = await loading;
                }
                finally
                {
                    if (_loadingTasks.TryGetValue(primaryKey, out Task<UnityEngine.Object> current)
                        && current == loading)
                    {
                        _loadingTasks.Remove(primaryKey);
                    }
                }

                if (loaded is not T resource)
                {
                    return null;
                }

                _cache.AddRequestAlias(assetGuidOrKey, primaryKey);
                return resource;
            }
            catch (Exception ex)
            {
                AddressableLogging.LogLoadError(assetGuidOrKey, ex.Message);
                return null;
            }
            finally
            {
                Addressables.Release(locationsHandle);
            }
        }

        private async Task<UnityEngine.Object> LoadAndCacheAsync<T>(IResourceLocation location, string primaryKey)
            where T : UnityEngine.Object
        {
            int capturedGeneration = _generation;
            AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(location);

            T resource = await loadHandle.Task;
            if (capturedGeneration != _generation)
            {
                if (loadHandle.IsValid())
                {
                    Addressables.Release(loadHandle);
                }

                return null;
            }

            if (resource == null)
            {
                Debug.LogError($"[AddressableLayout] Addressable 로드 실패: {primaryKey}");
                if (loadHandle.IsValid())
                {
                    Addressables.Release(loadHandle);
                }

                return null;
            }

            _directHandles[primaryKey] = loadHandle;
            _cache.Add(primaryKey, resource, CacheOwnership.Direct);
            _cache.AddLocationAliasForCache(location, primaryKey);
            return resource;
        }

        internal void ReleaseDirectHandle(string primaryKey)
        {
            if (!_directHandles.TryGetValue(primaryKey, out AsyncOperationHandle handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _directHandles.Remove(primaryKey);
        }

        internal void ReleaseAll()
        {
            foreach (KeyValuePair<string, AsyncOperationHandle> pair in _directHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            _directHandles.Clear();
            _loadingTasks.Clear();
            _generation++;
        }
    }
}
