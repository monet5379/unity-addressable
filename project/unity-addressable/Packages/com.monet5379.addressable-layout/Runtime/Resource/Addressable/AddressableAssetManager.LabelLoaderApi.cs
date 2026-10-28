using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableLayout.Resource
{
    public sealed partial class AddressableAssetManager
    {
        private static readonly IEnumerator CompletedRoutine = Array.Empty<object>().GetEnumerator();

        public bool IsLabelLoaded(string label)
        {
            if (string.IsNullOrEmpty(label)
                || !_labelRefCounts.TryGetValue(label, out int count)
                || count <= 0)
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
                RollbackOneLabelReference(label);
                LogPreloadEmptyResult(
                    label,
                    typeof(T).Name,
                    $"라벨 '{label}' 미등록. {ex.Message}",
                    failOnEmptyInDev);
            }
            catch (Exception ex)
            {
                RollbackOneLabelReference(label);
                LogLoadError(label, ex.Message);
            }

            if (!completed)
            {
                onCompleted?.Invoke(false, Array.Empty<T>());
            }

            return CompletedRoutine;
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
    }
}
