using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// PathMeta 갱신 · ForEditor 스모크 · Demo 등록.
    /// </summary>
    public sealed class AddressableLayoutToolWindow : EditorWindow
    {
        private const string DemoSetupTypeName =
            "AddressableLayout.Demo.DemoAddressableSetup, AddressableLayout.Demo.Editor";

        private int _lastEntryCount = -1;
        private string _lastMessage = "디스크를 스캔해 PathMeta를 갱신하려면 아래 갱신을 누르세요.";

        [MenuItem("Tools/Addressable Layout/레이아웃 도구", priority = 10)]
        public static void Open()
        {
            GetWindow<AddressableLayoutToolWindow>("레이아웃 도구");
        }

        private void OnGUI()
        {
            DrawPathMetaSection();
            DrawForEditorSmokeSection();
            DrawDemoSectionIfAvailable();
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

        private static void DrawForEditorSmokeSection()
        {
            BeginSectionBox();
            EditorGUILayout.LabelField("ForEditor 스모크", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ResourcesManagerForEditor로 path·boot 라벨 로드를 시도합니다.\nPlay Addressables 캐시는 사용하지 않습니다. 결과는 Console을 확인하세요.",
                MessageType.None);

            if (GUILayout.Button("스모크 실행", GUILayout.Height(28)))
            {
                ForEditorSmoke.Run();
            }

            EndSectionBox();
        }

        private void RefreshPathMeta()
        {
            int count = PathMetaRefresh.Refresh();
            _lastEntryCount = count;
            _lastMessage = $"PathMeta 갱신 완료. entries={count} ({DateTime.Now:HH:mm:ss})";
            Debug.Log($"[AddressableLayout] {_lastMessage}");
        }

        private static void DrawDemoSectionIfAvailable()
        {
            Type setupType = Type.GetType(DemoSetupTypeName);
            if (setupType == null)
            {
                return;
            }

            MethodInfo register = setupType.GetMethod(
                "RegisterBootSample",
                BindingFlags.Public | BindingFlags.Static);
            if (register == null)
            {
                return;
            }

            BeginSectionBox();
            EditorGUILayout.LabelField("Demo 샘플 등록", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Package Manager에서 Demo Sample을 import한 뒤, boot·place 라벨과 샘플 에셋을 등록합니다.\n" +
                "완료 시 PathMeta도 갱신됩니다.\n" +
                "Play Mode 계약 검증은 Test Runner → Play Mode → DemoPlayModeTests를 실행하세요.",
                MessageType.None);

            if (GUILayout.Button("부트 샘플 등록", GUILayout.Height(28)))
            {
                register.Invoke(null, null);
            }

            EndSectionBox();
        }

        private static void BeginSectionBox()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        private static void EndSectionBox()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
