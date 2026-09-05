using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// Tools → Refresh Paths (B/F). 로직은 PathMetaRefresh와 Path Settings가 공유.
    /// </summary>
    public static class PathMetaRefreshMenu
    {
        [MenuItem("Tools/Addressable Layout/Refresh Paths", priority = 10)]
        public static void RefreshPaths()
        {
            int count = PathMetaRefresh.Refresh();
            Debug.Log($"[AddressableLayout] Refresh Paths done. entries={count}");
        }
    }
}
