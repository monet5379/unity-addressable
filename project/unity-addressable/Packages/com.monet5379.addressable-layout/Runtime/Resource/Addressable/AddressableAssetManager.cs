using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    public sealed class AddressableAssetManager
    {
        private readonly CacheOwnership _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle> _directHandles = new();
        private readonly Dictionary<LabelTypeKey, AsyncOperationHandle> _labelHandles = new();
        private readonly LabelReferenceTracker _labelRefs = new();
        private readonly Dictionary<string, Task<UnityEngine.Object>> _loadingTasks = new();
        private readonly Dictionary<LabelTypeKey, object> _labelInflightLoads = new();
        private int _generation;
        private static readonly IEnumerator CompletedRoutine = Array.Empty<object>().GetEnumerator();

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

        public IList<T> PreloadLabelSync<T>(string label, bool failOnEmptyInDev = false) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            if (TryReturnCachedLabelLoad(label, out IList<T> cached))
            {
                return cached;
            }

            return LoadResourcesByLabelWithAcquireSync<T>(label, failOnEmptyInDev);
        }

        public IEnumerator CountLabelLocationsAsync<T>(IReadOnlyList<string> labels, IDictionary<string, int> results)
            where T : UnityEngine.Object
        {
            return CountLabelLocations<T>(labels, results, LabelLoadMode.Async);
        }

        public IEnumerator CountLabelLocations<T>(
            IReadOnlyList<string> labels,
            IDictionary<string, int> results,
            LabelLoadMode mode) where T : UnityEngine.Object
        {
            if (labels == null || results == null)
            {
                yield break;
            }

            AsyncOperationHandle<IList<IResourceLocation>>[] handles =
                new AsyncOperationHandle<IList<IResourceLocation>>[labels.Count];

            try
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    if (!string.IsNullOrEmpty(labels[i]))
                    {
                        handles[i] = Addressables.LoadResourceLocationsAsync(labels[i], typeof(T));
                    }
                }

                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsValid() && !handles[i].IsDone)
                    {
                        yield return WaitForHandle(handles[i], mode, null);
                    }
                }

                for (int i = 0; i < labels.Count; i++)
                {
                    string label = labels[i];
                    if (string.IsNullOrEmpty(label))
                    {
                        continue;
                    }

                    int count = 0;
                    if (handles[i].IsValid()
                        && handles[i].Status == AsyncOperationStatus.Succeeded
                        && handles[i].Result != null)
                    {
                        count = handles[i].Result.Count;
                    }

                    results[label] = count;
                }
            }
            finally
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsValid())
                    {
                        Addressables.Release(handles[i]);
                    }
                }
            }
        }

        public IEnumerator PreloadLabelAsync<T>(
            string label,
            Action<float> onProgress,
            bool failOnEmptyInDev = false) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                onProgress?.Invoke(1f);
                yield break;
            }

            if (TryReturnCachedLabelLoad(label, out IList<T> _))
            {
                onProgress?.Invoke(1f);
                yield break;
            }

            Task<IList<T>> loadTask =
                LoadResourcesByLabelWithAcquireAsync<T>(label, failOnEmptyInDev);

            while (!loadTask.IsCompleted)
            {
                if (TryGetLabelLoadProgress<T>(label, out float progress))
                {
                    onProgress?.Invoke(progress);
                }

                yield return null;
            }

            onProgress?.Invoke(1f);

            if (failOnEmptyInDev && (loadTask.Result == null || loadTask.Result.Count == 0))
            {
                LogPreloadEmptyResult(
                    label,
                    typeof(T).Name,
                    $"라벨 '{label}' preload 결과가 비었습니다.",
                    failOnEmptyInDev: true);
            }
        }

        public IEnumerator LoadLabel<T>(
            string label,
            LabelLoadMode mode,
            Action<float> onProgress,
            bool failOnEmptyInDev,
            Action<bool, IList<T>> onCompleted) where T : UnityEngine.Object
        {
            if (mode == LabelLoadMode.Async)
            {
                return LoadLabelRoutine(label, mode, onProgress, failOnEmptyInDev, onCompleted);
            }

            bool completed = false;
            Action<bool, IList<T>> completedOnce = (acquired, assets) =>
            {
                completed = true;
                onCompleted?.Invoke(acquired, assets);
            };

            try
            {
                RunSynchronously(LoadLabelRoutine(label, mode, onProgress, failOnEmptyInDev, completedOnce));
            }
            catch (InvalidKeyException ex)
            {
                _labelRefs.Rollback(label);
                LogPreloadEmptyResult(
                    label,
                    typeof(T).Name,
                    $"라벨 '{label}' 미등록. {ex.Message}",
                    failOnEmptyInDev);
            }
            catch (Exception ex)
            {
                _labelRefs.Rollback(label);
                LogLoadError(label, ex.Message);
            }

            if (!completed)
            {
                onCompleted?.Invoke(false, Array.Empty<T>());
            }

            return CompletedRoutine;
        }

        public bool IsLabelLoaded(string label)
        {
            if (string.IsNullOrEmpty(label) || !_labelRefs.IsAcquired(label))
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

        public void CopyLoadedLabels(List<(string Label, int RefCount, int AssetCount)> destination)
        {
            if (destination == null)
            {
                return;
            }

            _labelRefs.CopyTo(destination);
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

            if (_labelRefs.TryRelease(label, out bool evict) && evict)
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

            if (!_labelRefs.TryRelease(label, out bool evict))
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
            _labelRefs.Clear();
            _labelInflightLoads.Clear();
            _loadingTasks.Clear();
            _cache.Clear();
            _generation++;
        }

        internal IList<T> LoadResourcesByLabelWithAcquireSync<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            if (TryReturnCachedLabelLoad(label, out IList<T> cached))
            {
                return cached;
            }

            _labelRefs.Acquire(label, 0);
            try
            {
                IList<T> assets = LoadLabelAssetsOnceSync<T>(label, failOnEmptyInDev);
                if (assets == null || assets.Count == 0)
                {
                    _labelRefs.Rollback(label);
                    return Array.Empty<T>();
                }

                _labelRefs.RecordAssetCount(label, assets.Count);
                return assets;
            }
            catch (InvalidKeyException ex)
            {
                _labelRefs.Rollback(label);
                Debug.LogError(
                    $"[AddressableLayout] 라벨 '{label}'을(를) 찾을 수 없습니다 ({typeof(T).Name}). {ex.Message}\n" +
                    "Unity: Tools → Addressable Layout → Demo → 부트 샘플 등록, " +
                    "Addressables Play Mode Script → Use Asset Database (Fast Mode).");
                return Array.Empty<T>();
            }
            catch (Exception ex)
            {
                _labelRefs.Rollback(label);
                LogLoadError(label, ex.Message);
                return Array.Empty<T>();
            }
        }

        internal async Task<IList<T>> LoadResourcesByLabelWithAcquireAsync<T>(
            string label,
            bool failOnEmptyInDev) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return Array.Empty<T>();
            }

            if (TryReturnCachedLabelLoad(label, out IList<T> cached))
            {
                return cached;
            }

            _labelRefs.Acquire(label, 0);
            try
            {
                Task<IList<T>> loadTask = GetOrCreateLabelLoadTask<T>(label, failOnEmptyInDev);
                IList<T> assets = await loadTask;
                if (assets == null || assets.Count == 0)
                {
                    _labelRefs.Rollback(label);
                    return Array.Empty<T>();
                }

                _labelRefs.RecordAssetCount(label, assets.Count);
                return assets;
            }
            catch (InvalidKeyException ex)
            {
                _labelRefs.Rollback(label);
                Debug.LogError(
                    $"[AddressableLayout] 라벨 '{label}'을(를) 찾을 수 없습니다 ({typeof(T).Name}). {ex.Message}\n" +
                    "Unity: Tools → Addressable Layout → Demo → 부트 샘플 등록, " +
                    "Addressables Play Mode Script → Use Asset Database (Fast Mode).");
                return Array.Empty<T>();
            }
            catch (Exception ex)
            {
                _labelRefs.Rollback(label);
                LogLoadError(label, ex.Message);
                return Array.Empty<T>();
            }
        }

        internal bool TryGetLabelLoadProgress<T>(string label, out float progress) where T : UnityEngine.Object
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

        internal static void RunSynchronously(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator top = stack.Peek();
                    if (!top.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }

                    if (top.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }

                    Debug.LogError(
                        "[AddressableLayout] sync load routine이 프레임 대기를 yield했습니다. 동기 실행을 중단합니다.");
                    break;
                }
            }
            finally
            {
                while (stack.Count > 0)
                {
                    (stack.Pop() as IDisposable)?.Dispose();
                }
            }
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
            _labelRefs.Acquire(label, loaded.Count);
            return true;
        }

        private Task<IList<T>> GetOrCreateLabelLoadTask<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            LabelTypeKey labelKey = new(label, typeof(T));
            if (_labelInflightLoads.TryGetValue(labelKey, out object existing))
            {
                return (Task<IList<T>>)existing;
            }

            Task<IList<T>> task = LoadLabelAssetsOnceAsync<T>(label, failOnEmptyInDev);
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

        private IList<T> LoadLabelAssetsOnceSync<T>(string label, bool failOnEmptyInDev) where T : UnityEngine.Object
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
                    LogPreloadEmptyResult(
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
                    LogPreloadEmptyResult(
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

        private async Task<IList<T>> LoadLabelAssetsOnceAsync<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(T));

            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task;
                if (locations == null || locations.Count == 0)
                {
                    LogPreloadEmptyResult(
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
                if (assets == null || assets.Count == 0)
                {
                    LogPreloadEmptyResult(
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

        private IEnumerator LoadLabelRoutine<T>(
            string label,
            LabelLoadMode mode,
            Action<float> onProgress,
            bool failOnEmptyInDev,
            Action<bool, IList<T>> onCompleted) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(false, Array.Empty<T>());
                yield break;
            }

            if (TryReturnCachedLabelLoad(label, out IList<T> cached))
            {
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(true, cached);
                yield break;
            }

            if (mode == LabelLoadMode.Sync)
            {
                IList<T> assets = LoadResourcesByLabelWithAcquireSync<T>(label, failOnEmptyInDev);
                onProgress?.Invoke(1f);
                bool acquired = assets != null && assets.Count > 0;
                onCompleted?.Invoke(acquired, acquired ? assets : Array.Empty<T>());
                yield break;
            }

            yield return PreloadLabelAsync<T>(label, onProgress, failOnEmptyInDev);

            if (TryGetLoadedLabelAssets(label, out IList<T> loaded))
            {
                onCompleted?.Invoke(true, loaded);
            }
            else
            {
                onCompleted?.Invoke(false, Array.Empty<T>());
            }
        }

        private bool TryGetLoadedLabelAssets<T>(string label, out IList<T> assets) where T : UnityEngine.Object
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

        private static IEnumerator WaitForHandle(
            AsyncOperationHandle handle,
            LabelLoadMode mode,
            Action<float> onProgress)
        {
            if (!handle.IsValid() || handle.IsDone)
            {
                yield break;
            }

            if (mode == LabelLoadMode.Sync)
            {
                handle.WaitForCompletion();
                yield break;
            }

            while (handle.IsValid() && !handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                yield return null;
            }
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

        private static void LogPreloadEmptyResult(
            string label,
            string typeName,
            string detail,
            bool failOnEmptyInDev)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (failOnEmptyInDev)
            {
                Debug.LogError(
                    $"[AddressableLayout] Boot preload 라벨 '{label}' — {typeName} 0건. {detail}");
                return;
            }
#endif

            Debug.LogWarning($"[AddressableLayout] {detail}");
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
