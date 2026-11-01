namespace AddressableLayout.Resource
{
    public static class EditModeSyncCacheSeed
    {
        public static void Seed(params string[] labels)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                ResourcesManagerForEditor.ForEachLabeledAsset(
                    label,
                    (unixPath, asset) => ResourcesManager.RegisterSyncCacheHit(unixPath, asset));
            }
        }
    }
}
