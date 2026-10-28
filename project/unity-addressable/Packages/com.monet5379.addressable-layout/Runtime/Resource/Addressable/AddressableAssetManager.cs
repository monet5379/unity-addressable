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
        private readonly DirectAssetLoader _direct;
        private readonly LabelAssetLoader _labels;
        private readonly LabelReferenceTracker _labelRefs = new();
        private static readonly IEnumerator CompletedRoutine = Array.Empty<object>().GetEnumerator();

        public AddressableAssetManager()
        {
            _direct = new DirectAssetLoader(_cache);
            _labels = new LabelAssetLoader(_cache);
        }

        public T TryGetCached<T>(string key) where T : UnityEngine.Object
        {
            return _direct.TryGetCached<T>(key);
        }

        public bool IsResourceLoaded(string key)
        {
            return _direct.IsResourceLoaded(key);
        }

        public Task<T> LoadResourceAsync<T>(string assetGuidOrKey) where T : UnityEngine.Object
        {
            return _direct.LoadResourceAsync<T>(assetGuidOrKey);
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
                if (_labels.TryGetProgress<T>(label, out float progress))
                {
                    onProgress?.Invoke(progress);
                }

                yield return null;
            }

            onProgress?.Invoke(1f);

            if (failOnEmptyInDev && (loadTask.Result == null || loadTask.Result.Count == 0))
            {
                AddressableLogging.LogPreloadEmptyResult(
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
                AddressableLogging.LogPreloadEmptyResult(
                    label,
                    typeof(T).Name,
                    $"라벨 '{label}' 미등록. {ex.Message}",
                    failOnEmptyInDev);
            }
            catch (Exception ex)
            {
                _labelRefs.Rollback(label);
                AddressableLogging.LogLoadError(label, ex.Message);
            }

            if (!completed)
            {
                onCompleted?.Invoke(false, Array.Empty<T>());
            }

            return CompletedRoutine;
        }

        public bool IsLabelLoaded(string label)
        {
            return !string.IsNullOrEmpty(label) && _labels.IsLabelLoaded(label, _labelRefs.IsAcquired(label));
        }

        public void CopyLoadedLabels(List<(string Label, int RefCount, int AssetCount)> destination)
        {
            if (destination == null)
            {
                return;
            }

            _labelRefs.CopyTo(destination);
        }

        #region Release

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

            _direct.ReleaseDirectHandle(primaryKey);
        }

        public void ReleaseLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (_labelRefs.TryRelease(label, out bool evict) && evict)
            {
                _labels.Evict(label);
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
                _labels.Evict(label);
            }

            return evict;
        }

        public void ReleaseAll()
        {
            _direct.ReleaseAll();
            _labels.ReleaseAll();
            _labelRefs.Clear();
            _cache.Clear();
        }

        #endregion

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
                IList<T> assets = _labels.LoadAssetsOnceSync<T>(label, failOnEmptyInDev);
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
                AddressableLogging.LogLoadError(label, ex.Message);
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
                Task<IList<T>> loadTask = _labels.GetOrCreateLoadTask<T>(label, failOnEmptyInDev);
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
                AddressableLogging.LogLoadError(label, ex.Message);
                return Array.Empty<T>();
            }
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

        private bool TryReturnCachedLabelLoad<T>(string label, out IList<T> assets) where T : UnityEngine.Object
        {
            if (!_labels.TryGetLoaded(label, out assets))
            {
                return false;
            }

            _labelRefs.Acquire(label, assets.Count);
            return true;
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

            if (_labels.TryGetLoaded(label, out IList<T> loaded))
            {
                onCompleted?.Invoke(true, loaded);
            }
            else
            {
                onCompleted?.Invoke(false, Array.Empty<T>());
            }
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
    }
}
