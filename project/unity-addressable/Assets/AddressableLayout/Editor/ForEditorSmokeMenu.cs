using AddressableLayout.Resource;
using UnityEditor;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// Edit Mode *ForEditor 스모크 (Milestone E). Play 캐시를 채우지 않음.
    /// </summary>
    public static class ForEditorSmokeMenu
    {
        private const string BootSamplePath =
            "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";

        [MenuItem("Tools/Addressable Layout/Smoke ForEditor (Edit Mode)", priority = 50)]
        public static void SmokeForEditor()
        {
            DemoBootSampleByPath();
            BootLabelLoad();
            Debug.Log(
                "[AddressableLayout] ForEditor smoke done. " +
                "Edit uses AssetDatabase / settings labels; Play sync LoadResource = cache hit only. " +
                "Folder OK / label missing: path ForEditor may succeed while Play label load fails.");
        }

        private static void DemoBootSampleByPath()
        {
            // Demo SO는 Demo asmdef에 있어 여기선 Object로만 로드.
            Object byPath = ResourcesManagerForEditor.LoadResourceForEditor<Object>(BootSamplePath);
            Debug.Log(
                byPath != null
                    ? $"[AddressableLayout] ForEditor path OK: {BootSamplePath} → {byPath.name}"
                    : $"[AddressableLayout] ForEditor path miss: {BootSamplePath} " +
                      "(run Demo → Register Boot Sample).");
        }

        private static void BootLabelLoad()
        {
            var byLabel =
                ResourcesManagerForEditor.LoadResourcesByLabelForEditor<Object>(AddressableLabels.Boot);
            Debug.Log(
                $"[AddressableLayout] ForEditor label '{AddressableLabels.Boot}' count={byLabel.Count} " +
                "(empty if folder exists but label not assigned).");
        }
    }
}
