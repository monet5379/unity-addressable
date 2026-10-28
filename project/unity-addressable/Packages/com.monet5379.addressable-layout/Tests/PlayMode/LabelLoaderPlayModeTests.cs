using System.Collections;
using System.Collections.Generic;
using AddressableLayout.Demo;
using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AddressableLayout.Tests.PlayMode
{
    [Parallelizable(ParallelScope.None)]
    public class LabelLoaderPlayModeTests
    {
        private const string BootAddress = "Assets/Addressables/Scriptable/Demo/DemoBootSample.asset";

        [UnityTest]
        public IEnumerator AsyncLoadGroup_CompletesWithProgress()
        {
            ResourcesManager.ReleaseAll();
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive("Demo PathMeta missing. Register boot sample first.");
                yield break;
            }

            var loader = new AddressableLabelLoader(ResourcesManager.AssetManager);
            LoadOperation operation = loader.LoadGroup(
                new LoadRequest("play-async-boot", new List<string> { AddressableLabels.Boot }, LabelLoadMode.Async)
                {
                    AssetType = typeof(DemoBootSample),
                });

            while (!operation.IsDone)
            {
                yield return null;
            }

            Assert.That(operation.Progress, Is.EqualTo(1f).Within(0.001f));
            Assert.That(operation.HasFailed, Is.False);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Not.Null);

            loader.Release("play-async-boot");
            ResourcesManager.ReleaseAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AsyncLoadGroup_ReleaseWhileInflight_DropsAcquiredLabels()
        {
            ResourcesManager.ReleaseAll();
            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive("Demo PathMeta missing. Register boot sample first.");
                yield break;
            }

            var loader = new AddressableLabelLoader(ResourcesManager.AssetManager);
            var labels = new List<string> { AddressableLabels.Boot, AddressableLabels.DemoAreaA };
            LoadOperation operation = loader.LoadGroup(
                new LoadRequest("play-cancel", labels, LabelLoadMode.Async)
                {
                    AssetType = typeof(DemoBootSample),
                });

            yield return null;

            Assert.That(loader.Release("play-cancel"), Is.True);

            while (!operation.IsDone)
            {
                yield return null;
            }

            Assert.That(operation.IsDone, Is.True);
            Assert.That(ResourcesManager.LoadResource<DemoBootSample>(BootAddress), Is.Null);

            ResourcesManager.ReleaseAll();
            yield return null;
        }
    }
}
