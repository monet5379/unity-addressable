using AddressableLayout.Resource;
using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// Resources + Addressables 이중 스캔 → PathMetaData.json 갱신 (Milestone F).
    /// </summary>
    public static class PathMetaRefreshMenu
    {
        [MenuItem("Tools/Addressable Layout/Refresh Paths", priority = 10)]
        public static void RefreshPaths()
        {
            int count = PathManager.UpdatePathMetadata();
            AssetDatabase.Refresh();
            Debug.Log($"[AddressableLayout] Refresh Paths done. entries={count}");
        }
    }
}
