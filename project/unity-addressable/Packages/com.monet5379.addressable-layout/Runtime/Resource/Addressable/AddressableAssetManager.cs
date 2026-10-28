using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    public sealed partial class AddressableAssetManager
    {
        private readonly CacheOwnership _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle> _directHandles = new();
        private readonly Dictionary<LabelTypeKey, AsyncOperationHandle> _labelHandles = new();
        private readonly Dictionary<string, int> _labelRefCounts = new();
        private readonly Dictionary<string, int> _labelAssetCounts = new();
        private readonly Dictionary<string, Task<UnityEngine.Object>> _loadingTasks = new();
        private int _generation;

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
                    AddRequestAlias(assetGuidOrKey, primaryKey);
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

                AddRequestAlias(assetGuidOrKey, primaryKey);
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

        public Task<IList<T>> LoadResourcesByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            return LoadResourcesByLabelWithAcquireAsync<T>(label, failOnEmptyInDev: false);
        }

        public void Release(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.TryResolve(key, out string primaryKey))
            {
                return;
            }

            if (!_cache.Release(primaryKey, CacheOwnership.Direct))
            {
                return;
            }

            ReleaseDirectHandle(primaryKey);
        }

        public void ReleaseLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (TryDecrementLabelReference(label, out bool evict) && evict)
            {
                EvictLabelHandlesAndOwner(label);
            }
        }

        [Obsolete("Use ReleaseLabel instead. ReleaseLabelRef will be removed in a future release.")]
        public bool ReleaseLabelRef(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            if (!TryDecrementLabelReference(label, out bool evict))
            {
                return false;
            }

            if (evict)
            {
                EvictLabelHandlesAndOwner(label);
            }

            return evict;
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<string, AsyncOperationHandle> pair in _directHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            foreach (KeyValuePair<LabelTypeKey, AsyncOperationHandle> pair in _labelHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            _directHandles.Clear();
            _labelHandles.Clear();
            _labelRefCounts.Clear();
            _labelAssetCounts.Clear();
            _labelInflightLoads.Clear();
            _loadingTasks.Clear();
            _cache.Clear();
            _generation++;
        }

        private void AcquireLabelReference(string label, int assetCount)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (_labelRefCounts.TryGetValue(label, out int count))
            {
                _labelRefCounts[label] = count + 1;
            }
            else
            {
                _labelRefCounts[label] = 1;
            }

            if (assetCount > 0)
            {
                _labelAssetCounts[label] = assetCount;
            }
        }

        private bool TryDecrementLabelReference(string label, out bool evict)
        {
            evict = false;
            if (!_labelRefCounts.TryGetValue(label, out int count) || count <= 0)
            {
                return false;
            }

            count--;
            if (count > 0)
            {
                _labelRefCounts[label] = count;
                return true;
            }

            _labelRefCounts.Remove(label);
            _labelAssetCounts.Remove(label);
            evict = true;
            return true;
        }

        private void EvictLabelHandlesAndOwner(string label)
        {
            var keysToRemove = new List<LabelTypeKey>();
            foreach (KeyValuePair<LabelTypeKey, AsyncOperationHandle> pair in _labelHandles)
            {
                if (!string.Equals(pair.Key.Label, label, StringComparison.Ordinal))
                {
                    continue;
                }

                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }

                keysToRemove.Add(pair.Key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _labelHandles.Remove(keysToRemove[i]);
            }

            _cache.ReleaseOwner(label);
        }

        private bool TryReturnCachedLabelLoad<T>(string label, out IList<T> assets) where T : UnityEngine.Object
        {
            assets = null;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            LabelTypeKey labelKey = new(label, typeof(T));
            if (!_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle handle) || !handle.IsValid())
            {
                return false;
            }

            if (!handle.IsDone)
            {
                return false;
            }

            IList<T> loaded = handle.Convert<IList<T>>().Result;
            if (loaded == null || loaded.Count == 0)
            {
                return false;
            }

            assets = loaded;
            AcquireLabelReference(label, loaded.Count);
            return true;
        }

        private void CacheLabelAssets<T>(
            string label,
            IList<T> assets,
            IList<IResourceLocation> locations) where T : UnityEngine.Object
        {
            int count = Mathf.Min(assets.Count, locations.Count);
            for (int i = 0; i < count; i++)
            {
                string primaryKey = locations[i].PrimaryKey;
                T asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                _cache.Add(primaryKey, asset, label);
                AddLocationAliasForCache(locations[i], primaryKey);
            }

            Debug.Log(
                $"[AddressableLayout] 라벨 '{label}' 캐시 완료: {assets.Count}개 {typeof(T).Name} 에셋.");
        }

        private void AddRequestAlias(string requestKey, string primaryKey)
        {
            if (string.IsNullOrEmpty(requestKey)
                || string.Equals(requestKey, primaryKey, StringComparison.Ordinal))
            {
                return;
            }

            _cache.AddAlias(primaryKey, requestKey);
        }

        private void AddLocationAliasForCache(IResourceLocation location, string primaryKey)
        {
            if (location == null)
            {
                return;
            }

            string internalId = location.InternalId;
            if (string.IsNullOrEmpty(internalId)
                || string.Equals(internalId, primaryKey, StringComparison.Ordinal))
            {
                return;
            }

            _cache.AddAlias(primaryKey, internalId);
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
            AddLocationAliasForCache(location, primaryKey);
            return resource;
        }

        private void ReleaseDirectHandle(string primaryKey)
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

        private static void LogLoadError(string key, string errorMessage)
        {
            if (errorMessage.IndexOf("Invalid path", StringComparison.Ordinal) >= 0
                || errorMessage.IndexOf("settings.json", StringComparison.Ordinal) >= 0)
            {
                Debug.LogError(
                    $"[AddressableLayout] Addressable 로드 실패: {key}. " +
                    "Addressables 빌드 또는 Play Mode Script를 Use Asset Database로 설정하세요. " +
                    "Tools → Addressable Layout → Demo → 부트 샘플 등록.");
                return;
            }

            Debug.LogError($"[AddressableLayout] Addressable 오류 ('{key}'): {errorMessage}");
        }

        private readonly struct LabelTypeKey : IEquatable<LabelTypeKey>
        {
            public readonly string Label;
            public readonly Type AssetType;

            public LabelTypeKey(string label, Type assetType)
            {
                Label = label;
                AssetType = assetType;
            }

            public bool Equals(LabelTypeKey other)
            {
                return string.Equals(Label, other.Label, StringComparison.Ordinal)
                    && AssetType == other.AssetType;
            }

            public override bool Equals(object obj) => obj is LabelTypeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(Label ?? string.Empty) * 397)
                        ^ (AssetType != null ? AssetType.GetHashCode() : 0);
                }
            }
        }
    }
}
