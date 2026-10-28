using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    internal sealed class LabelAssetLoader
    {
        private readonly CacheOwnership _cache;
        private readonly Dictionary<LabelTypeKey, AsyncOperationHandle> _labelHandles = new();
        private readonly Dictionary<LabelTypeKey, object> _labelInflightLoads = new();
        private int _generation;

        public LabelAssetLoader(CacheOwnership cache)
        {
            _cache = cache;
        }

        internal bool TryGetLoaded<T>(string label, out IList<T> assets) where T : UnityEngine.Object
        {
            assets = null;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            LabelTypeKey labelKey = new(label, typeof(T));
            if (!_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle handle)
                || !handle.IsValid()
                || !handle.IsDone)
            {
                return false;
            }

            IList<T> loaded = handle.Convert<IList<T>>().Result;
            if (loaded == null || loaded.Count == 0)
            {
                return false;
            }

            assets = loaded;
            return true;
        }

        internal bool TryGetProgress<T>(string label, out float progress) where T : UnityEngine.Object
        {
            progress = 0f;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            LabelTypeKey labelKey = new(label, typeof(T));
            if (!_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle handle) || !handle.IsValid())
            {
                return false;
            }

            progress = handle.PercentComplete;
            return true;
        }

        internal bool IsLabelLoaded(string label, bool isAcquired)
        {
            if (string.IsNullOrEmpty(label) || !isAcquired)
            {
                return false;
            }

            foreach (KeyValuePair<LabelTypeKey, AsyncOperationHandle> pair in _labelHandles)
            {
                if (!string.Equals(pair.Key.Label, label, StringComparison.Ordinal))
                {
                    continue;
                }

                if (pair.Value.IsValid() && pair.Value.IsDone)
                {
                    return true;
                }
            }

            return false;
        }

        internal Task<IList<T>> GetOrCreateLoadTask<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            LabelTypeKey labelKey = new(label, typeof(T));
            if (_labelInflightLoads.TryGetValue(labelKey, out object existing))
            {
                return (Task<IList<T>>)existing;
            }

            Task<IList<T>> task = LoadAssetsOnceAsync<T>(label, failOnEmptyInDev);
            _labelInflightLoads[labelKey] = task;
            task.ContinueWith(
                completed =>
                {
                    if (_labelInflightLoads.TryGetValue(labelKey, out object current) && ReferenceEquals(current, task))
                    {
                        _labelInflightLoads.Remove(labelKey);
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext());
            return task;
        }

        internal IList<T> LoadAssetsOnceSync<T>(string label, bool failOnEmptyInDev) where T : UnityEngine.Object
        {
            LabelTypeKey labelKey = new(label, typeof(T));
            if (_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle existingHandle)
                && existingHandle.IsValid())
            {
                if (!existingHandle.IsDone)
                {
                    existingHandle.WaitForCompletion();
                }

                IList<T> doneAssets = existingHandle.Convert<IList<T>>().Result;
                if (doneAssets != null && doneAssets.Count > 0)
                {
                    return doneAssets;
                }
            }

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(T));

            try
            {
                locationsHandle.WaitForCompletion();
                IList<IResourceLocation> locations = locationsHandle.Status == AsyncOperationStatus.Succeeded
                    ? locationsHandle.Result
                    : null;

                if (locations == null || locations.Count == 0)
                {
                    AddressableLogging.LogPreloadEmptyResult(
                        label,
                        typeof(T).Name,
                        $"라벨 '{label}'에 대한 {typeof(T).Name} location 없음.",
                        failOnEmptyInDev);
                    return Array.Empty<T>();
                }

                if (!_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle handle) || !handle.IsValid())
                {
                    AsyncOperationHandle<IList<T>> loadHandle =
                        Addressables.LoadAssetsAsync<T>(locations, null);
                    handle = loadHandle;
                    _labelHandles[labelKey] = handle;
                }

                handle.WaitForCompletion();
                IList<T> assets = handle.Convert<IList<T>>().Result;
                if (assets == null || assets.Count == 0)
                {
                    AddressableLogging.LogPreloadEmptyResult(
                        label,
                        typeof(T).Name,
                        $"라벨 '{label}' 로드 결과 {typeof(T).Name} 목록이 비었습니다.",
                        failOnEmptyInDev);
                    return Array.Empty<T>();
                }

                CacheLabelAssets(label, assets, locations);
                return assets;
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
        }

        private async Task<IList<T>> LoadAssetsOnceAsync<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            int capturedGeneration = _generation;

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(T));

            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task;
                if (capturedGeneration != _generation)
                {
                    return Array.Empty<T>();
                }

                if (locations == null || locations.Count == 0)
                {
                    AddressableLogging.LogPreloadEmptyResult(
                        label,
                        typeof(T).Name,
                        $"라벨 '{label}'에 대한 {typeof(T).Name} location 없음.",
                        failOnEmptyInDev);
                    return Array.Empty<T>();
                }

                LabelTypeKey labelKey = new(label, typeof(T));
                if (!_labelHandles.TryGetValue(labelKey, out AsyncOperationHandle handle) || !handle.IsValid())
                {
                    AsyncOperationHandle<IList<T>> loadHandle =
                        Addressables.LoadAssetsAsync<T>(locations, null);
                    handle = loadHandle;
                    _labelHandles[labelKey] = handle;
                }

                IList<T> assets = await handle.Convert<IList<T>>().Task;
                if (capturedGeneration != _generation)
                {
                    return Array.Empty<T>();
                }

                if (assets == null || assets.Count == 0)
                {
                    AddressableLogging.LogPreloadEmptyResult(
                        label,
                        typeof(T).Name,
                        $"라벨 '{label}' 로드 결과 {typeof(T).Name} 목록이 비었습니다.",
                        failOnEmptyInDev);
                    return Array.Empty<T>();
                }

                CacheLabelAssets(label, assets, locations);
                return assets;
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
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
                _cache.AddLocationAliasForCache(locations[i], primaryKey);
            }

            Debug.Log(
                $"[AddressableLayout] 라벨 '{label}' 캐시 완료: {assets.Count}개 {typeof(T).Name} 에셋.");
        }

        internal void Evict(string label)
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

        internal void ReleaseAll()
        {
            foreach (KeyValuePair<LabelTypeKey, AsyncOperationHandle> pair in _labelHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            _labelHandles.Clear();
            _labelInflightLoads.Clear();
            _generation++;
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
