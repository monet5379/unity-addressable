using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode 검증 진입점 (<c>-executeMethod AddressableLayoutVerify.PrepareDemo</c>).
/// Demo Editor asmdef에 직접 참조하지 않고 리플렉션으로 <see cref="RegisterBootSample"/> 호출.
/// </summary>
public static class AddressableLayoutVerify
{
    private const string DemoSetupTypeName =
        "AddressableLayout.Demo.DemoAddressableSetup, AddressableLayout.Demo.Editor";

    public static void PrepareDemo()
    {
        Type setupType = Type.GetType(DemoSetupTypeName);
        if (setupType == null)
        {
            Debug.LogError(
                $"[AddressableLayoutVerify] Demo 설정 타입을 찾을 수 없습니다 ({DemoSetupTypeName}). " +
                "Package Manager → Samples에서 Demo 샘플을 import하거나 Assets/Samples가 있는 프로젝트를 사용하세요.");
            EditorApplication.Exit(1);
            return;
        }

        MethodInfo register = setupType.GetMethod(
            "RegisterBootSample",
            BindingFlags.Public | BindingFlags.Static);
        if (register == null)
        {
            Debug.LogError(
                "[AddressableLayoutVerify] DemoAddressableSetup에서 RegisterBootSample 메서드를 찾을 수 없습니다.");
            EditorApplication.Exit(1);
            return;
        }

        try
        {
            register.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }
}
