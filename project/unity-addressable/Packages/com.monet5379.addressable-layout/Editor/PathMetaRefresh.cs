using AddressableLayout.Resource;
using UnityEditor;

namespace AddressableLayout.Editor
{
    public static class PathMetaRefresh
    {
        public static int Refresh()
        {
            int count = PathManager.UpdatePathMetadata();
            AssetDatabase.Refresh();
            return count;
        }
    }
}
