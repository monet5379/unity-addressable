using AddressableLayout.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressableLayout.Demo
{
    public static class DemoAddressableSetup
    {
        private const string SampleFolder = "Assets/Addressables/Scriptable/Demo";
        private const string PrefabFolder = "Assets/Addressables/Prefabs/Demo";
        private const string BootPath = SampleFolder + "/DemoBootSample.asset";
        private const string PlaceAPath = SampleFolder + "/DemoPlaceASample.asset";
        private const string PlaceBPath = SampleFolder + "/DemoPlaceBSample.asset";
        private const string SpawnPrefabPath = PrefabFolder + "/DemoSpawnSample.prefab";
        private const string CollisionAddressablesPath = SampleFolder + "/DemoCollisionSample.asset";

        private const string ResourcesDemoFolder = "Assets/Resources/Demo";
        private const string ResourcesOnlyPath = ResourcesDemoFolder + "/DemoResourcesOnly.asset";
        private const string CollisionResourcesPath = ResourcesDemoFolder + "/DemoCollisionSample.asset";

        /// <summary>repo dev verify·Editor에서 RegisterBootSample() 직접 호출.</summary>
        public static void RegisterBootSample()
        {
            EnsureFolder("Assets/Addressables");
            EnsureFolder("Assets/Addressables/Scriptable");
            EnsureFolder(SampleFolder);
            EnsureFolder("Assets/Addressables/Prefabs");
            EnsureFolder(PrefabFolder);
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

            settings.AddLabel(DevTestLabels.Boot);
            settings.AddLabel(DevTestLabels.Default);
            settings.AddLabel(DevTestLabels.PlaceA);
            settings.AddLabel(DevTestLabels.PlaceB);

            RegisterSample(settings, BootPath, "boot-sample", DevTestLabels.Boot, DevTestLabels.Default);
            RegisterSample(settings, PlaceAPath, "place-a", DevTestLabels.PlaceA);
            RegisterSample(settings, PlaceBPath, "place-b", DevTestLabels.PlaceB);
            RegisterSpawnPrefab(settings, SpawnPrefabPath, DevTestLabels.Boot);

            EnsureSampleAsset(CollisionAddressablesPath, "collision-addressables");
            EnsureSampleAsset(ResourcesOnlyPath, "resources-only");
            EnsureSampleAsset(CollisionResourcesPath, "collision-resources");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            int pathCount = PathMetaWriter.Write();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[DemoAddressableSetup] Registered boot + place + spawn prefab + F dual-scan samples " +
                $"({DevTestLabels.Boot}, {DevTestLabels.PlaceA}, {DevTestLabels.PlaceB}). " +
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

        private static void RegisterSpawnPrefab(
            AddressableAssetSettings settings,
            string prefabPath,
            params string[] labels)
        {
            EnsureSpawnPrefab(prefabPath);

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(prefabPath);
            for (int i = 0; i < labels.Length; i++)
            {
                entry.SetLabel(labels[i], true, true);
            }
        }

        private static void EnsureSpawnPrefab(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            var temp = new GameObject("DemoSpawnSample");
            PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            UnityEngine.Object.DestroyImmediate(temp);
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
