using AddressableLayout.Resource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// Demo 샘플 SO + boot/place 라벨 등록 + PathMeta 이중 스캔 (A/B/C/F Exit).
    /// </summary>
    public static class DemoAddressableSetup
    {
        private const string SampleFolder = "Assets/Addressables/Scriptable/Demo";
        private const string BootPath = SampleFolder + "/DemoBootSample.asset";
        private const string PlaceAPath = SampleFolder + "/DemoPlaceASample.asset";
        private const string PlaceBPath = SampleFolder + "/DemoPlaceBSample.asset";
        private const string CollisionAddressablesPath = SampleFolder + "/DemoCollisionSample.asset";

        private const string ResourcesDemoFolder = "Assets/Resources/Demo";
        private const string ResourcesOnlyPath = ResourcesDemoFolder + "/DemoResourcesOnly.asset";
        private const string CollisionResourcesPath = ResourcesDemoFolder + "/DemoCollisionSample.asset";

        [MenuItem("Tools/Addressable Layout/Demo/Register Boot Sample")]
        public static void RegisterBootSample()
        {
            EnsureFolder("Assets/Addressables");
            EnsureFolder("Assets/Addressables/Scriptable");
            EnsureFolder(SampleFolder);
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResourcesDemoFolder);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[DemoAddressableSetup] Could not create AddressableAssetSettings.");
                return;
            }

            // Play Mode: Use Asset Database (BuildScriptFastMode).
            settings.ActivePlayModeDataBuilderIndex = IndexOfFastModeBuilder(settings);

            settings.AddLabel(AddressableLabels.Boot);
            settings.AddLabel(AddressableLabels.Default);
            settings.AddLabel(AddressableLabels.DemoAreaA);
            settings.AddLabel(AddressableLabels.DemoAreaB);

            RegisterSample(settings, BootPath, "boot-sample", AddressableLabels.Boot, AddressableLabels.Default);
            RegisterSample(settings, PlaceAPath, "place-a", AddressableLabels.DemoAreaA);
            RegisterSample(settings, PlaceBPath, "place-b", AddressableLabels.DemoAreaB);

            // F: Addressables 쪽 충돌 파일은 디스크만 (라벨/그룹 없음). Resources-only도 Addressables 미등록.
            EnsureSampleAsset(CollisionAddressablesPath, "collision-addressables");
            EnsureSampleAsset(ResourcesOnlyPath, "resources-only");
            EnsureSampleAsset(CollisionResourcesPath, "collision-resources");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            int pathCount = PathManager.UpdatePathMetadata();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[DemoAddressableSetup] Registered boot + place + F dual-scan samples " +
                $"({AddressableLabels.Boot}, {AddressableLabels.DemoAreaA}, {AddressableLabels.DemoAreaB}). " +
                $"PathMeta entries={pathCount}. Enter Play to run Demo.");
        }

        private static void RegisterSample(
            AddressableAssetSettings settings,
            string assetPath,
            string markerId,
            params string[] labels)
        {
            DemoBootSample sample = EnsureSampleAsset(assetPath, markerId);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(assetPath);
            for (int i = 0; i < labels.Length; i++)
            {
                entry.SetLabel(labels[i], true, true);
            }

            EditorUtility.SetDirty(sample);
        }

        private static DemoBootSample EnsureSampleAsset(string assetPath, string markerId)
        {
            DemoBootSample sample = AssetDatabase.LoadAssetAtPath<DemoBootSample>(assetPath);
            if (sample == null)
            {
                sample = ScriptableObject.CreateInstance<DemoBootSample>();
                AssetDatabase.CreateAsset(sample, assetPath);
            }

            SerializedObject so = new SerializedObject(sample);
            so.FindProperty("markerId").stringValue = markerId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sample);
            return sample;
        }

        private static int IndexOfFastModeBuilder(AddressableAssetSettings settings)
        {
            for (int i = 0; i < settings.DataBuilders.Count; i++)
            {
                var builder = settings.DataBuilders[i];
                if (builder != null && builder.GetType().Name.Contains("FastMode"))
                {
                    return i;
                }
            }

            return settings.ActivePlayModeDataBuilderIndex;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
