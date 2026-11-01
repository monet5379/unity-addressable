using System;
using UnityEngine;

namespace AddressableLayout.Resource
{
    internal static class AddressableLogging
    {
        internal static void LogLoadError(string key, string errorMessage)
        {
            if (errorMessage.IndexOf("Invalid path", StringComparison.Ordinal) >= 0
                || errorMessage.IndexOf("settings.json", StringComparison.Ordinal) >= 0)
            {
                Debug.LogError(
                    $"[AddressableLayout] Addressable 로드 실패: {key}. " +
                    "Addressables 빌드 또는 Play Mode Script를 Use Asset Database로 설정하고, " +
                    "에셋에 Addressables 라벨이 할당되었는지 확인하세요. PathMeta는 레이아웃 도구 → 갱신.");
                return;
            }

            Debug.LogError($"[AddressableLayout] Addressable 오류 ('{key}'): {errorMessage}");
        }

        internal static void LogPreloadEmptyResult(
            string label,
            string typeName,
            string detail,
            bool failOnEmptyInDev)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (failOnEmptyInDev)
            {
                Debug.LogError(
                    $"[AddressableLayout] Boot preload 라벨 '{label}' — {typeName} 0건. {detail}");
                return;
            }
#endif

            Debug.LogWarning($"[AddressableLayout] {detail}");
        }
    }
}
