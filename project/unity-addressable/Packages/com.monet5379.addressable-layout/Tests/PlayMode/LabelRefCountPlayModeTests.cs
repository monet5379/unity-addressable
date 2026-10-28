using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableLayout.Demo;
using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AddressableLayout.Tests.PlayMode
{
    [Parallelizable(ParallelScope.None)]
    public class LabelRefCountPlayModeTests
    {
        private const string BootAddress = "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";

        [UnityTest]
        public IEnumerator SyncPreload_ThenSyncLoad_HitsCache()
        {
            ResourcesManager.ReleaseAll();
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive("Demo PathMeta missing. Register boot sample first.");
                yield break;
            }

            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            IList<DemoBootSample> loaded =
                ResourcesManager.AssetManager.PreloadLabelSync<DemoBootSample>(AddressableLabels.Boot);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Count, Is.GreaterThan(0));

            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            ResourcesManager.ReleaseAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoAcquires_OneRelease_StillHitsCache()
        {
            ResourcesManager.ReleaseAll();
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive("Demo PathMeta missing. Register boot sample first.");
                yield break;
            }

            Task<IList<DemoBootSample>> first = ResourcesManager.LoadResourcesByLabelAsync<DemoBootSample>(
                AddressableLabels.Boot);
            while (!first.IsCompleted)
            {
                yield return null;
            }

            Assert.That(first.Result, Is.Not.Null);
            Assert.That(first.Result.Count, Is.GreaterThan(0));

            Task<IList<DemoBootSample>> second = ResourcesManager.LoadResourcesByLabelAsync<DemoBootSample>(
                AddressableLabels.Boot);
            while (!second.IsCompleted)
            {
                yield return null;
            }

            ResourcesManager.ReleaseLabel(AddressableLabels.Boot);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            ResourcesManager.ReleaseLabel(AddressableLabels.Boot);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SyncPreloadWhileAsyncInflight_OneAcquirePerCall()
        {
            ResourcesManager.ReleaseAll();
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive("Demo PathMeta missing. Register boot sample first.");
                yield break;
            }

            Task<IList<DemoBootSample>> asyncLoad = ResourcesManager.LoadResourcesByLabelAsync<DemoBootSample>(
                AddressableLabels.Boot);

            IList<DemoBootSample> syncLoaded =
                ResourcesManager.AssetManager.PreloadLabelSync<DemoBootSample>(AddressableLabels.Boot);
            Assert.That(syncLoaded, Is.Not.Null);
            Assert.That(syncLoaded.Count, Is.GreaterThan(0));

            while (!asyncLoad.IsCompleted)
            {
                yield return null;
            }

            Assert.That(asyncLoad.Result, Is.Not.Null);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            ResourcesManager.ReleaseLabel(AddressableLabels.Boot);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            ResourcesManager.ReleaseLabel(AddressableLabels.Boot);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
            yield return null;
        }
    }
}
