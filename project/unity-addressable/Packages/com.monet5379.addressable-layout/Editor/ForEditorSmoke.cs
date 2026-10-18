using AddressableLayout.Resource;
using UnityEngine;

namespace AddressableLayout.Editor
{
    /// <summary>
    /// Edit Mode *ForEditor 스모크. Play 캐시를 채우지 않음.
    /// </summary>
    public static class ForEditorSmoke
    {
        private const string BootSamplePath =
            "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";

        public static void Run()
        {
            DemoBootSampleByPath();
            BootLabelLoad();
            Debug.Log(
                "[AddressableLayout] ForEditor 스모크 완료. " +
                "Edit는 AssetDatabase/settings 라벨, Play 동기 LoadResource는 캐시 hit만. " +
                "폴더만 있고 라벨 없음: ForEditor path는 성공해도 Play 라벨 로드는 실패할 수 있음.");
        }

        private static void DemoBootSampleByPath()
        {
            Object byPath = ResourcesManagerForEditor.LoadResourceForEditor<Object>(BootSamplePath);
            Debug.Log(
                byPath != null
                    ? $"[AddressableLayout] ForEditor path OK: {BootSamplePath} → {byPath.name}"
                    : $"[AddressableLayout] ForEditor path miss: {BootSamplePath} " +
                      "(레이아웃 도구 → 부트 샘플 등록).");
        }

        private static void BootLabelLoad()
        {
            var byLabel =
                ResourcesManagerForEditor.LoadResourcesByLabelForEditor<Object>(AddressableLabels.Boot);
            Debug.Log(
                $"[AddressableLayout] ForEditor 라벨 '{AddressableLabels.Boot}' count={byLabel.Count} " +
                "(폴더만 있고 라벨 미할당이면 0).");
        }
    }
}
