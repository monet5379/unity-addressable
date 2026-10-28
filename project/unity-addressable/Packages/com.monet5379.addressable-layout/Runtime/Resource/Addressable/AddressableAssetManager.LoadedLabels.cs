using System.Collections.Generic;

namespace AddressableLayout.Resource
{
    public sealed partial class AddressableAssetManager
    {
        public void CopyLoadedLabels(List<(string Label, int RefCount, int AssetCount)> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            foreach (KeyValuePair<string, int> pair in _labelRefCounts)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                int assetCount = _labelAssetCounts.TryGetValue(pair.Key, out int count) ? count : 0;
                destination.Add((pair.Key, pair.Value, assetCount));
            }

            destination.Sort(CompareLoadedLabels);
        }

        private static int CompareLoadedLabels(
            (string Label, int RefCount, int AssetCount) left,
            (string Label, int RefCount, int AssetCount) right)
        {
            return string.CompareOrdinal(left.Label, right.Label);
        }
    }
}
