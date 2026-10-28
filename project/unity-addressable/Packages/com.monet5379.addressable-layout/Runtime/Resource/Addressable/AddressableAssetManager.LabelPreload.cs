using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    public sealed partial class AddressableAssetManager
    {
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

            System.Threading.Tasks.Task<IList<T>> loadTask =
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
    }
}
