using System.Collections.Generic;
using AddressableLayout.Demo;
using AddressableLayout.Resource;
using NUnit.Framework;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class AddressableLabelLoaderEditTests
    {
        private const string BootAddress = "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";

        [Test]
        public void SyncLoadGroup_ThenSyncLoad_HitsCache()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Inconclusive(skipReason);
            }

            ResourcesManager.ReleaseAll();
            var loader = new AddressableLabelGroupManager(ResourcesManager.AssetManager);

            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            LoadOperation operation = loader.LoadGroup(BootGroupRequest("edit-boot", LabelLoadMode.Sync));

            Assert.That(operation.IsDone, Is.True);
            Assert.That(operation.HasFailed, Is.False);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            loader.Release("edit-boot");
            ResourcesManager.ReleaseAll();
        }

        [Test]
        public void KeepResident_NonForceReleaseIgnored_ForceEvicts()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Inconclusive(skipReason);
            }

            ResourcesManager.ReleaseAll();
            var loader = new AddressableLabelGroupManager(ResourcesManager.AssetManager);

            LoadRequest request = BootGroupRequest("resident", LabelLoadMode.Sync);
            request.KeepResident = true;
            LoadOperation operation = loader.LoadGroup(request);
            Assert.That(operation.IsDone, Is.True);

            Assert.That(loader.Release("resident"), Is.False);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            Assert.That(loader.Release("resident", force: true), Is.True);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
        }

        [Test]
        public void TwoGroupsShareLabel_OneRelease_KeepsCache()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Inconclusive(skipReason);
            }

            ResourcesManager.ReleaseAll();
            var loader = new AddressableLabelGroupManager(ResourcesManager.AssetManager);
            var labels = new List<string> { DevTestLabels.Boot };

            loader.LoadGroup(BootGroupRequest("group-a", LabelLoadMode.Sync, labels));
            loader.LoadGroup(BootGroupRequest("group-b", LabelLoadMode.Sync, labels));

            Assert.That(loader.Release("group-a"), Is.True);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            Assert.That(loader.Release("group-b"), Is.True);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
        }

        [Test]
        public void SharedLoader_UsesResourcesManagerAssetManager()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Inconclusive(skipReason);
            }

            ResourcesManager.ReleaseAll();

            LoadOperation operation = AddressableLabelGroupManager.Shared.LoadGroup(
                BootGroupRequest("shared-boot", LabelLoadMode.Sync));

            Assert.That(operation.IsDone, Is.True);
            Assert.That(ResourcesManager.AssetManager.IsLabelLoaded(DevTestLabels.Boot), Is.True);

            AddressableLabelGroupManager.Shared.Release("shared-boot", force: true);
            ResourcesManager.ReleaseAll();
        }

        [Test]
        public void SyncLoadGroup_ThenPreloadLabelSync_SharesLabelHandle()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Inconclusive(skipReason);
            }

            ResourcesManager.ReleaseAll();
            var loader = new AddressableLabelGroupManager(ResourcesManager.AssetManager);

            loader.LoadGroup(BootGroupRequest("typed-boot", LabelLoadMode.Sync));
            IList<DemoBootSample> secondAcquire =
                ResourcesManager.AssetManager.PreloadLabelSync<DemoBootSample>(DevTestLabels.Boot);
            Assert.That(secondAcquire, Is.Not.Null);
            Assert.That(secondAcquire.Count, Is.GreaterThan(0));

            loader.Release("typed-boot");
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            ResourcesManager.ReleaseLabel(DevTestLabels.Boot);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
        }

        private static LoadRequest BootGroupRequest(
            string id,
            LabelLoadMode mode,
            IReadOnlyList<string> labels = null)
        {
            return new LoadRequest(id, labels ?? new List<string> { DevTestLabels.Boot }, mode)
            {
                AssetType = typeof(DemoBootSample),
            };
        }

        private static bool TryEnsureDemoPathMeta(out string skipReason)
        {
            skipReason = null;
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                skipReason = "Demo PathMeta missing. Run PrepareTestCatalog or DemoAddressableSetup.RegisterBootSample().";
                return false;
            }

            IList<DemoBootSample> boot =
                ResourcesManagerForEditor.LoadResourcesByLabelForEditor<DemoBootSample>(DevTestLabels.Boot);
            if (boot == null || boot.Count == 0)
            {
                skipReason =
                    "Demo boot label has no DemoBootSample entries. Register boot sample (PrepareTestCatalog).";
                return false;
            }

            return true;
        }
    }
}
