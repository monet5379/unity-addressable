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
        private readonly Dictionary<LabelTypeKey, object> _labelInflightLoads = new();

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

            AcquireLabelReference(label, 0);
            try
            {
                Task<IList<T>> loadTask = GetOrCreateLabelLoadTask<T>(label, failOnEmptyInDev);
                IList<T> assets = await loadTask.ConfigureAwait(true);
                if (assets == null || assets.Count == 0)
                {
                    RollbackOneLabelReference(label);
                    return Array.Empty<T>();
                }

                RecordLabelAssetCount(label, assets.Count);
                return assets;
            }
            catch (InvalidKeyException ex)
            {
                RollbackOneLabelReference(label);
                Debug.LogError(
                    $"[AddressableLayout] 라벨 '{label}'을(를) 찾을 수 없습니다 ({typeof(T).Name}). {ex.Message}\n" +
                    "Unity: Tools → Addressable Layout → Demo → 부트 샘플 등록, " +
                    "Addressables Play Mode Script → Use Asset Database (Fast Mode).");
                return Array.Empty<T>();
            }
            catch (Exception ex)
            {
                RollbackOneLabelReference(label);
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
                TaskScheduler.Default);
            return task;
        }

        private async Task<IList<T>> LoadLabelAssetsOnceAsync<T>(string label, bool failOnEmptyInDev)
            where T : UnityEngine.Object
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(T));

            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task.ConfigureAwait(true);
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

                IList<T> assets = await handle.Convert<IList<T>>().Task.ConfigureAwait(true);
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

        private void RecordLabelAssetCount(string label, int assetCount)
        {
            if (string.IsNullOrEmpty(label) || assetCount <= 0)
            {
                return;
            }

            _labelAssetCounts[label] = assetCount;
        }

        private void RollbackOneLabelReference(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (!_labelRefCounts.TryGetValue(label, out int count) || count <= 0)
            {
                return;
            }

            count--;
            if (count > 0)
            {
                _labelRefCounts[label] = count;
                return;
            }

            _labelRefCounts.Remove(label);
            _labelAssetCounts.Remove(label);
        }
    }
}
