using AddressableLayout.Resource;
using UnityEditor;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// PathMeta 디스크 스캔 공유 로직 (메뉴 · Path Settings 윈도우).
    /// </summary>
    public static class PathMetaRefresh
    {
        /// <summary>
        /// Resources + Addressables 이중 스캔 → PathMetaData.json 갱신 후 AssetDatabase.Refresh.
        /// </summary>
        public static int Refresh()
        {
            int count = PathManager.UpdatePathMetadata();
            AssetDatabase.Refresh();
            return count;
        }
    }
}
