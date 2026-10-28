using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AddressableLayout.Resource
{
    public sealed class LoadRequest
    {
        public LoadRequest()
        {
        }

        public LoadRequest(string id, IReadOnlyList<string> labels, LabelLoadMode mode = LabelLoadMode.Async)
        {
            Id = id;
            Labels = labels;
            Mode = mode;
        }

        public string Id { get; set; }

        public IReadOnlyList<string> Labels { get; set; }

        public LabelLoadMode Mode { get; set; } = LabelLoadMode.Async;

        public bool KeepResident { get; set; }

        public bool FailOnEmptyInDev { get; set; }

        public Type AssetType { get; set; } = typeof(UnityEngine.Object);
    }

    public sealed class LoadOperation : CustomYieldInstruction
    {
        private readonly string[] _labels;
        private readonly float[] _labelProgress;
        private readonly Dictionary<string, int> _labelAssetCounts = new();
        private readonly List<string> _acquiredLabels = new();
        private readonly List<string> _failedLabels = new();
        private Action<LoadOperation> _completed;
        private int _totalAssetCount;

        internal LoadOperation(LoadRequest request)
        {
            Id = request.Id;
            Mode = request.Mode;
            KeepResident = request.KeepResident;
            AssetType = AddressableLabelLoadByType.Normalize(request.AssetType);

            IReadOnlyList<string> labels = request.Labels;
            int count = labels != null ? labels.Count : 0;
            _labels = new string[count];
            for (int i = 0; i < count; i++)
            {
                _labels[i] = labels[i];
            }

            _labelProgress = new float[count];
        }

        public string Id { get; }

        public IReadOnlyList<string> Labels => _labels;

        public LabelLoadMode Mode { get; }

        public bool KeepResident { get; }

        internal Type AssetType { get; }

        public float Progress { get; private set; }

        public bool IsDone { get; private set; }

        public bool IsReleased { get; private set; }

        public IReadOnlyDictionary<string, int> LabelAssetCounts => _labelAssetCounts;

        public int TotalAssetCount => _totalAssetCount;

        public override bool keepWaiting => !IsDone;

        public event Action<float> ProgressChanged;

        public event Action<LoadOperation> Completed
        {
            add
            {
                if (value == null)
                {
                    return;
                }

                if (IsDone)
                {
                    value(this);
                    return;
                }

                _completed += value;
            }
            remove => _completed -= value;
        }

        public IReadOnlyList<string> FailedLabels => _failedLabels;

        public Exception Error { get; private set; }

        public bool HasFailed => Error != null || _failedLabels.Count > 0;

        internal IReadOnlyList<string> AcquiredLabels => _acquiredLabels;

        internal bool ReleaseRequested { get; set; }

        internal void SetAssetCounts(IDictionary<string, int> counts)
        {
            _labelAssetCounts.Clear();
            _totalAssetCount = 0;
            for (int i = 0; i < _labels.Length; i++)
            {
                string label = _labels[i];
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                int count = counts != null && counts.TryGetValue(label, out int value) ? Mathf.Max(0, value) : 0;
                _labelAssetCounts[label] = count;
                _totalAssetCount += count;
            }
        }

        internal void SetLabelProgress(int index, float percent)
        {
            if (index < 0 || index >= _labelProgress.Length)
            {
                return;
            }

            _labelProgress[index] = Mathf.Clamp01(percent);
            SetProgress(CalculateWeightedProgress());
        }

        internal void AddFailedLabel(string label)
        {
            _failedLabels.Add(label);
        }

        internal void SetError(Exception error)
        {
            Error ??= error;
        }

        internal void AddAcquiredLabel(string label)
        {
            _acquiredLabels.Add(label);
        }

        internal void Complete()
        {
            if (IsDone)
            {
                return;
            }

            IsDone = true;
            SetProgress(1f);

            Action<LoadOperation> completed = _completed;
            _completed = null;
            completed?.Invoke(this);
        }

        internal void MarkReleased()
        {
            IsReleased = true;
            _acquiredLabels.Clear();
        }

        private float CalculateWeightedProgress()
        {
            if (_totalAssetCount <= 0)
            {
                return 0f;
            }

            float weightedSum = 0f;
            for (int i = 0; i < _labels.Length; i++)
            {
                string label = _labels[i];
                if (!string.IsNullOrEmpty(label) && _labelAssetCounts.TryGetValue(label, out int count))
                {
                    weightedSum += count * _labelProgress[i];
                }
            }

            return Mathf.Clamp01(weightedSum / _totalAssetCount);
        }

        private void SetProgress(float progress)
        {
            if (Mathf.Approximately(Progress, progress))
            {
                return;
            }

            Progress = progress;
            ProgressChanged?.Invoke(progress);
        }
    }

    public sealed class AddressableLabelLoader
    {
        private static AddressableLabelLoader _shared;

        private readonly AddressableAssetManager _assets;
        private readonly Dictionary<string, LoadOperation> _groups = new();

        public AddressableLabelLoader(AddressableAssetManager assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public static AddressableLabelLoader Shared => _shared ??= new AddressableLabelLoader(ResourcesManager.AssetManager);

        public bool IsLoaded(string groupId)
        {
            return TryGetOperation(groupId, out LoadOperation operation) && operation.IsDone;
        }

        public bool TryGetOperation(string groupId, out LoadOperation operation)
        {
            operation = null;
            if (string.IsNullOrEmpty(groupId) || !_groups.TryGetValue(groupId, out LoadOperation found))
            {
                return false;
            }

            if (found.IsDone && !AreAcquiredLabelsLoaded(found))
            {
                _groups.Remove(groupId);
                found.MarkReleased();
                return false;
            }

            operation = found;
            return true;
        }

        public LoadOperation LoadGroup(LoadRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.Id))
            {
                throw new ArgumentException("LoadRequest.Id is empty.", nameof(request));
            }

            if (TryGetOperation(request.Id, out LoadOperation existing))
            {
                return existing;
            }

            LoadOperation operation = new LoadOperation(request);
            _groups[request.Id] = operation;

            LabelLoadMode mode = Application.isPlaying ? request.Mode : LabelLoadMode.Sync;
            IEnumerator routine = RunGroup(operation, mode, request.FailOnEmptyInDev);
            if (mode == LabelLoadMode.Sync)
            {
                try
                {
                    AddressableAssetManager.RunSynchronously(routine);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddressableLayout] 그룹 '{request.Id}' 동기 적재 실패: {ex.Message}");
                    operation.SetError(ex);
                    operation.Complete();
                }
            }
            else
            {
                AddressableLabelLoaderRunner.Run(routine);
            }

            return operation;
        }

        public bool Release(string groupId, bool force = false)
        {
            if (string.IsNullOrEmpty(groupId) || !_groups.TryGetValue(groupId, out LoadOperation operation))
            {
                return false;
            }

            if (operation.KeepResident && !force)
            {
                Debug.LogWarning($"[AddressableLayout] 그룹 '{groupId}'은(는) resident입니다. Release가 무시되었습니다.");
                return false;
            }

            _groups.Remove(groupId);
            if (!operation.IsDone)
            {
                operation.ReleaseRequested = true;
                return true;
            }

            ReleaseOperation(operation);
            return true;
        }

        private IEnumerator RunGroup(LoadOperation operation, LabelLoadMode mode, bool failOnEmptyInDev)
        {
            IReadOnlyList<string> labels = operation.Labels;

            Dictionary<string, int> counts = new Dictionary<string, int>();
            Type assetType = operation.AssetType;
            yield return AddressableLabelLoadByType.CountLabelLocations(
                _assets,
                assetType,
                labels,
                counts,
                mode);
            operation.SetAssetCounts(counts);

            for (int i = 0; i < labels.Count; i++)
            {
                if (operation.ReleaseRequested)
                {
                    break;
                }

                int index = i;
                string label = labels[i];
                yield return AddressableLabelLoadByType.LoadLabel(
                    _assets,
                    assetType,
                    label,
                    mode,
                    percent => operation.SetLabelProgress(index, percent),
                    failOnEmptyInDev,
                    acquired =>
                    {
                        if (acquired)
                        {
                            operation.AddAcquiredLabel(label);
                        }
                        else
                        {
                            operation.AddFailedLabel(label);
                        }
                    });
            }

            operation.Complete();

            if (operation.ReleaseRequested)
            {
                ReleaseOperation(operation);
            }
        }

        private void ReleaseOperation(LoadOperation operation)
        {
            if (operation.IsReleased)
            {
                return;
            }

            IReadOnlyList<string> acquired = operation.AcquiredLabels;
            for (int i = 0; i < acquired.Count; i++)
            {
#pragma warning disable 618
                _assets.ReleaseLabelRef(acquired[i]);
#pragma warning restore 618
            }

            operation.MarkReleased();
        }

        private bool AreAcquiredLabelsLoaded(LoadOperation operation)
        {
            IReadOnlyList<string> acquired = operation.AcquiredLabels;
            for (int i = 0; i < acquired.Count; i++)
            {
                if (!_assets.IsLabelLoaded(acquired[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
