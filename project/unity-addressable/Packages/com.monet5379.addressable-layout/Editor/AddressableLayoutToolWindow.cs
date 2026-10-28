using System;
using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    public sealed class AddressableLayoutToolWindow : EditorWindow
    {
        private int _lastEntryCount = -1;
        private string _lastMessage = "디스크를 스캔해 PathMeta를 갱신하려면 아래 갱신을 누르세요.";

        [MenuItem("Tools/Addressable Layout/레이아웃 도구", priority = 10)]
        public static void Open()
        {
            GetWindow<AddressableLayoutToolWindow>("레이아웃 도구");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            DrawPathMetaSection();
        }

        private void DrawPathMetaSection()
        {
            BeginSectionBox();
            EditorGUILayout.LabelField("PathMeta 갱신", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assets/Resources와 Assets/Addressables를 스캔한 뒤 Assets/Resources/Data/PathMetaData.json에 기록합니다.",
                MessageType.None);

            if (GUILayout.Button("갱신", GUILayout.Height(28)))
            {
                RefreshPathMeta();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "엔트리",
                _lastEntryCount < 0 ? "—" : _lastEntryCount.ToString());
            EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
            EndSectionBox();
        }

        private void RefreshPathMeta()
        {
            int count = PathMetaRefresh.Refresh();
            _lastEntryCount = count;
            _lastMessage = $"PathMeta 갱신 완료. entries={count} ({DateTime.Now:HH:mm:ss})";
            Debug.Log($"[AddressableLayout] {_lastMessage}");
        }

        private static void BeginSectionBox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        private static void EndSectionBox()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
