using UnityEditor;

namespace AddressableLayout.Editor
{
    public static class PathMetaRefresh
    {
        public static int Refresh()
        {
            int count = PathMetaWriter.Write();
            AssetDatabase.Refresh();
            return count;
        }
    }
}
