using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableLayout.Resource;
using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// A: boot 라벨 · B: PathMeta · C: sync hit-only + place · D: Spawn/Despawn · F: dual-scan Lookup.
    /// </summary>
    public sealed class DemoAddressableHost : MonoBehaviour
    {
        private const string BootFileName = "DemoBootSample.asset";
        private const string BootAddress = "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";
        private const string PlaceAAddress = "Assets/Addressables/Scriptable/Demo/DemoPlaceASample.asset";
        private const string PlaceBFileName = "DemoPlaceBSample.asset";
        private const string SpawnPrefabName = "DemoSpawnSample";

        private const string ResourcesOnlyFileName = "DemoResourcesOnly.asset";
        private const string ResourcesOnlyLeaf = "Demo/DemoResourcesOnly";
        private const string CollisionFileName = "DemoCollisionSample.asset";
        private const string CollisionResourcesLeaf = "Demo/DemoCollisionSample";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapPlayground()
        {
            if (FindFirstObjectByType<DemoAddressableHost>() != null)
            {
                return;
            }

            var go = new GameObject("AddressableLayoutDemo");
            DontDestroyOnLoad(go);
            go.AddComponent<DemoAddressableHost>();
        }

        private async void Start()
        {
            RunMilestoneB();
            RunMilestoneF();
            await RunMilestoneAAndCAsync();
        }

        private static void RunMilestoneB()
        {
            PathManager.Load();
            AssertPathRoundTrip(BootFileName, BootAddress, expectAddressable: true);
            Debug.Log(
                $"[DemoAddressableHost] PathMeta round-trip OK: '{BootFileName}' → '{BootAddress}' " +
                "(milestone B).");
        }

        private static void RunMilestoneF()
        {
            AssertPathRoundTrip(ResourcesOnlyFileName, ResourcesOnlyLeaf, expectAddressable: false);
            Debug.Log(
                $"[DemoAddressableHost] Resources-only Lookup OK: '{ResourcesOnlyFileName}' → '{ResourcesOnlyLeaf}' " +
                "(milestone F).");

            AssertPathRoundTrip(CollisionFileName, CollisionResourcesLeaf, expectAddressable: false);
            Debug.Log(
                $"[DemoAddressableHost] Collision Lookup Resources wins: '{CollisionFileName}' → '{CollisionResourcesLeaf}' " +
                "(milestone F).");
        }

        private static async Task RunMilestoneAAndCAsync()
        {
            // C: preload 전 sync = miss
            DemoBootSample beforePreload = ResourcesManager.LoadResource<DemoBootSample>(BootAddress);
            if (beforePreload != null)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Sync load before preload must be null (cache hit-only).");
                return;
            }

            Debug.Log("[DemoAddressableHost] Sync miss before preload OK.");

            IList<DemoBootSample> bootLoaded =
                await ResourcesManager.LoadResourcesByLabelAsync<DemoBootSample>(AddressableLabels.Boot);

            if (bootLoaded == null || bootLoaded.Count == 0)
            {
                Debug.LogError(
                    "[DemoAddressableHost] No DemoBootSample under label 'boot'. " +
                    "Unity: Tools → Addressable Layout → Demo → Register Boot Sample. " +
                    "Addressables Play Mode Script → Use Asset Database (Fast Mode).");
                return;
            }

            DemoBootSample sample = bootLoaded[0];
            Debug.Log($"[DemoAddressableHost] Loaded sample markerId={sample.MarkerId}");

            // A: address async after label still works
            DemoBootSample again = await ResourcesManager.LoadResourceAsync<DemoBootSample>(BootAddress);
            Debug.Assert(again != null, "Address load after label preload must succeed.");
            if (again != null && ReferenceEquals(sample, again))
            {
                Debug.Log("[DemoAddressableHost] Cache hit on address reload (same instance).");
            }

            // C: sync hit after preload
            DemoBootSample syncHit = ResourcesManager.LoadResource<DemoBootSample>(BootAddress);
            if (syncHit == null)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Sync load after boot preload must hit cache. " +
                    "Check PrimaryKey/address alias.");
                return;
            }

            DemoBootSample byName = ResourcesManager.LoadResourceByName<DemoBootSample>(BootFileName);
            Debug.Assert(byName != null, "LoadResourceByName after preload must hit.");

            Debug.Log(
                $"[DemoAddressableHost] Sync hit after preload OK markerId={syncHit.MarkerId} " +
                "(milestone C preload).");

            await RunMilestoneDAsync();

            await RunPlaceEnterLeaveAsync();

            ResourcesManager.ReleaseAll();
            Debug.Log("[DemoAddressableHost] ReleaseAll done (milestone A/C).");
        }

        private static async Task RunMilestoneDAsync()
        {
            IList<GameObject> bootPrefabs =
                await ResourcesManager.LoadResourcesByLabelAsync<GameObject>(AddressableLabels.Boot);
            if (bootPrefabs == null || bootPrefabs.Count == 0)
            {
                Debug.LogError(
                    "[DemoAddressableHost] No GameObject under label 'boot'. " +
                    "Unity: Tools → Addressable Layout → Demo → Register Boot Sample.");
                return;
            }

            GameObject spawned = ResourcesManager.SpawnPrefab(SpawnPrefabName);
            if (spawned == null)
            {
                Debug.LogError(
                    $"[DemoAddressableHost] SpawnPrefab('{SpawnPrefabName}') failed after boot preload.");
                return;
            }

            Debug.Log(
                $"[DemoAddressableHost] SpawnPrefab OK name={SpawnPrefabName} " +
                $"(milestone D).");

            ResourcesManager.Despawn(spawned);
            Debug.Log("[DemoAddressableHost] Despawn OK (milestone D).");
        }

        private static async Task RunPlaceEnterLeaveAsync()
        {
            DemoBootSample placeABefore =
                ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            if (placeABefore != null)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Place A must miss before enter (not on boot label).");
                return;
            }

            IList<DemoBootSample> areaA =
                await ResourcesManager.EnterPlaceAsync<DemoBootSample>(AddressableLabels.DemoAreaA);
            if (areaA == null || areaA.Count == 0)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Place A enter failed. Re-run Register Boot Sample.");
                return;
            }

            DemoBootSample placeAHit = ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            Debug.Assert(placeAHit != null, "Place A sync hit after enter.");
            Debug.Log(
                $"[DemoAddressableHost] Enter {AddressableLabels.DemoAreaA} OK " +
                $"markerId={placeAHit.MarkerId}");

            ResourcesManager.LeavePlace(AddressableLabels.DemoAreaA);
            DemoBootSample placeAAfterLeave =
                ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            if (placeAAfterLeave != null)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Place A must miss after leave (ReleaseLabel).");
                return;
            }

            Debug.Log($"[DemoAddressableHost] Leave {AddressableLabels.DemoAreaA} OK.");

            IList<DemoBootSample> areaB =
                await ResourcesManager.EnterPlaceAsync<DemoBootSample>(AddressableLabels.DemoAreaB);
            if (areaB == null || areaB.Count == 0)
            {
                Debug.LogError(
                    "[DemoAddressableHost] Place B enter failed. Re-run Register Boot Sample.");
                return;
            }

            DemoBootSample placeBHit = ResourcesManager.LoadResourceByName<DemoBootSample>(PlaceBFileName);
            Debug.Assert(placeBHit != null, "Place B sync hit after enter.");
            Debug.Log(
                $"[DemoAddressableHost] Enter {AddressableLabels.DemoAreaB} OK " +
                $"markerId={placeBHit.MarkerId} (milestone C).");

            ResourcesManager.LeavePlace(AddressableLabels.DemoAreaB);
        }

        private static void AssertPathRoundTrip(
            string fileName,
            string expectedPath,
            bool expectAddressable)
        {
            string lookedUp = PathManager.Lookup(fileName);
            if (lookedUp == null)
            {
                Debug.LogError(
                    "[DemoAddressableHost] PathMeta Lookup failed. " +
                    "Tools → Addressable Layout → Demo → Register Boot Sample " +
                    "(or Refresh Paths).");
                return;
            }

            Debug.Assert(
                lookedUp == expectedPath,
                $"PathMeta round-trip expected '{expectedPath}', got '{lookedUp}'.");

            Debug.Assert(
                PathManager.IsAddressablePath(lookedUp) == expectAddressable,
                expectAddressable
                    ? "Lookup result must be an Addressables Assets/ path."
                    : "Lookup result must be a Resources.Load leaf (not Assets/).");
        }
    }
}
