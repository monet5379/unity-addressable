using System.Collections.Generic;

namespace AddressableLayout.Resource
{
    internal sealed class LabelReferenceTracker
    {
        private readonly Dictionary<string, int> _refCounts = new();
        private readonly Dictionary<string, int> _assetCounts = new();

        public bool IsAcquired(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            return _refCounts.TryGetValue(label, out int count) && count > 0;
        }

        public void Acquire(string label, int assetCount)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (_refCounts.TryGetValue(label, out int count))
            {
                _refCounts[label] = count + 1;
            }
            else
            {
                _refCounts[label] = 1;
            }

            if (assetCount > 0)
            {
                _assetCounts[label] = assetCount;
            }
        }

        public void RecordAssetCount(string label, int assetCount)
        {
            if (string.IsNullOrEmpty(label) || assetCount <= 0)
            {
                return;
            }

            _assetCounts[label] = assetCount;
        }

        public bool TryRelease(string label, out bool evict)
        {
            evict = false;
            if (!_refCounts.TryGetValue(label, out int count) || count <= 0)
            {
                return false;
            }

            count--;
            if (count > 0)
            {
                _refCounts[label] = count;
                return true;
            }

            _refCounts.Remove(label);
            _assetCounts.Remove(label);
            evict = true;
            return true;
        }

        public void Rollback(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            TryRelease(label, out _);
        }

        public void CopyTo(List<(string Label, int RefCount, int AssetCount)> destination)
        {
            destination.Clear();
            foreach (KeyValuePair<string, int> pair in _refCounts)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                int assetCount = _assetCounts.TryGetValue(pair.Key, out int count) ? count : 0;
                destination.Add((pair.Key, pair.Value, assetCount));
            }

            destination.Sort(CompareLoadedLabels);
        }

        public void Clear()
        {
            _refCounts.Clear();
            _assetCounts.Clear();
        }

        private static int CompareLoadedLabels(
            (string Label, int RefCount, int AssetCount) left,
            (string Label, int RefCount, int AssetCount) right)
        {
            return string.CompareOrdinal(left.Label, right.Label);
        }
    }
}
