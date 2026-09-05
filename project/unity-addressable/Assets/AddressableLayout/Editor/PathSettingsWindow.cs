using System;
using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// PathMeta Refresh + 엔트리 수 (Milestone E). 과한 설정 UI 없음.
    /// </summary>
    public sealed class PathSettingsWindow : EditorWindow
    {
        private int _lastEntryCount = -1;
        private string _lastMessage = "Refresh Paths to rebuild PathMeta from disk.";

        [MenuItem("Tools/Addressable Layout/Path Settings", priority = 11)]
        public static void Open()
        {
            GetWindow<PathSettingsWindow>("Path Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("PathMeta", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans Assets/Resources then Assets/Addressables and writes Resources/Data/PathMetaData.json.",
                MessageType.None);

            if (GUILayout.Button("Refresh Paths", GUILayout.Height(28)))
            {
                int count = PathMetaRefresh.Refresh();
                _lastEntryCount = count;
                _lastMessage = $"PathMeta refreshed. entries={count} ({DateTime.Now:HH:mm:ss})";
                Debug.Log($"[AddressableLayout] {_lastMessage}");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "Entries",
                _lastEntryCount < 0 ? "—" : _lastEntryCount.ToString());
            EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
        }
    }
}
