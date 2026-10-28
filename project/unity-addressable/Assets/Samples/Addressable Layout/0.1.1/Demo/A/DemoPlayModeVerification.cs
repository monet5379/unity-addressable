using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableLayout.Resource;
using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// Demo milestones A–F (Play Mode). 실패 시 <see cref="InvalidOperationException"/>.
    /// </summary>
    public static class DemoPlayModeVerification
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

        /// <summary>Play Mode 테스트 중 <see cref="DemoAddressableHost"/> 자동 부트스트랩 억제.</summary>
        public static bool DisableAutoBootstrap { get; set; }

        public static async Task RunAllAsync()
        {
            RunMilestoneB();
            RunMilestoneF();
            await RunMilestoneAAndCAsync();
        }

        private static void RunMilestoneB()
        {
            PathManager.Load();
            AssertPathRoundTrip(BootFileName, BootAddress, expectAddressable: true);
        }

        private static void RunMilestoneF()
        {
            AssertPathRoundTrip(ResourcesOnlyFileName, ResourcesOnlyLeaf, expectAddressable: false);
            AssertPathRoundTrip(CollisionFileName, CollisionResourcesLeaf, expectAddressable: false);
        }

        private static async Task RunMilestoneAAndCAsync()
        {
            DemoBootSample beforePreload = ResourcesManager.LoadResource<DemoBootSample>(BootAddress);
            if (beforePreload != null)
            {
                throw new InvalidOperationException(
                    "Sync load before preload must be null (cache hit-only).");
            }

            IList<DemoBootSample> bootLoaded =
                await ResourcesManager.LoadResourcesByLabelAsync<DemoBootSample>(AddressableLabels.Boot)
;

            if (bootLoaded == null || bootLoaded.Count == 0)
            {
                throw new InvalidOperationException(
                    "No DemoBootSample under label 'boot'.\nDemo → 부트 샘플 등록 및 Addressables Fast Mode를 확인하세요.");
            }

            DemoBootSample again = await ResourcesManager.LoadResourceAsync<DemoBootSample>(BootAddress)
;
            if (again == null)
            {
                throw new InvalidOperationException(
                    "Address load after label preload must succeed.");
            }

            DemoBootSample syncHit = ResourcesManager.LoadResource<DemoBootSample>(BootAddress);
            if (syncHit == null)
            {
                throw new InvalidOperationException(
                    "Sync load after boot preload must hit cache.");
            }

            DemoBootSample byName = ResourcesManager.LoadResourceByName<DemoBootSample>(BootFileName);
            if (byName == null)
            {
                throw new InvalidOperationException(
                    "LoadResourceByName after preload must hit.");
            }

            await RunMilestoneDAsync();
            await RunPlaceEnterLeaveAsync();

            ResourcesManager.ReleaseAll();
        }

        private static async Task RunMilestoneDAsync()
        {
            IList<GameObject> bootPrefabs =
                await ResourcesManager.LoadResourcesByLabelAsync<GameObject>(AddressableLabels.Boot)
;
            if (bootPrefabs == null || bootPrefabs.Count == 0)
            {
                throw new InvalidOperationException(
                    "No GameObject under label 'boot'.");
            }

            GameObject spawned = ResourcesManager.SpawnPrefab(SpawnPrefabName);
            if (spawned == null)
            {
                throw new InvalidOperationException(
                    $"SpawnPrefab('{SpawnPrefabName}') failed after boot preload.");
            }

            ResourcesManager.Despawn(spawned);
        }

        private static async Task RunPlaceEnterLeaveAsync()
        {
            DemoBootSample placeABefore =
                ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            if (placeABefore != null)
            {
                throw new InvalidOperationException(
                    "Place A must miss before enter (not on boot label).");
            }

            IList<DemoBootSample> areaA =
                await ResourcesManager.EnterPlaceAsync<DemoBootSample>(AddressableLabels.DemoAreaA)
;
            if (areaA == null || areaA.Count == 0)
            {
                throw new InvalidOperationException("Place A enter failed.");
            }

            DemoBootSample placeAHit = ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            if (placeAHit == null)
            {
                throw new InvalidOperationException("Place A sync hit after enter.");
            }

            ResourcesManager.LeavePlace(AddressableLabels.DemoAreaA);
            DemoBootSample placeAAfterLeave =
                ResourcesManager.LoadResource<DemoBootSample>(PlaceAAddress);
            if (placeAAfterLeave != null)
            {
                throw new InvalidOperationException(
                    "Place A must miss after leave (ReleaseLabel).");
            }

            IList<DemoBootSample> areaB =
                await ResourcesManager.EnterPlaceAsync<DemoBootSample>(AddressableLabels.DemoAreaB)
;
            if (areaB == null || areaB.Count == 0)
            {
                throw new InvalidOperationException("Place B enter failed.");
            }

            DemoBootSample placeBHit = ResourcesManager.LoadResourceByName<DemoBootSample>(PlaceBFileName);
            if (placeBHit == null)
            {
                throw new InvalidOperationException("Place B sync hit after enter.");
            }

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
                throw new InvalidOperationException(
                    $"PathMeta Lookup failed for '{fileName}'. 레이아웃 도구 → 갱신 또는 Demo → 부트 샘플 등록.");
            }

            if (lookedUp != expectedPath)
            {
                throw new InvalidOperationException(
                    $"PathMeta round-trip expected '{expectedPath}', got '{lookedUp}'.");
            }

            if (PathManager.IsAddressablePath(lookedUp) != expectAddressable)
            {
                throw new InvalidOperationException(
                    expectAddressable
                        ? "Lookup result must be an Addressables Assets/ path."
                        : "Lookup result must be a Resources.Load leaf (not Assets/).");
            }
        }
    }
}
