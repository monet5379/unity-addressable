using AddressableLayout.Demo;
using UnityEditor;
using UnityEngine;

public static class AddressableLayoutDevVerify
{
    /// <summary>repo dev verify: Addressables 라벨·Fast Mode·PathMeta (DemoAddressableSetup.RegisterBootSample).</summary>
    public static void PrepareTestCatalog()
    {
        try
        {
            DemoAddressableSetup.RegisterBootSample();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }
}
