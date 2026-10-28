using System;
using System.Collections;
using System.Threading.Tasks;
using AddressableLayout.Demo;
using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AddressableLayout.Tests.PlayMode
{
    internal sealed class DemoPlayModeTestsPrebuild : IPrebuildSetup
    {
        public void Setup()
        {
            DemoPlayModeVerification.DisableAutoBootstrap = true;
        }
    }

    internal sealed class DemoPlayModeTestsPostbuild : IPostBuildCleanup
    {
        public void Cleanup()
        {
            DemoPlayModeVerification.DisableAutoBootstrap = false;
        }
    }

    /// <summary>
    /// Demo milestones A–F. Test Runner <b>PlayMode</b> tab.
    /// </summary>
    [PrebuildSetup(typeof(DemoPlayModeTestsPrebuild))]
    [PostBuildCleanup(typeof(DemoPlayModeTestsPostbuild))]
    [Parallelizable(ParallelScope.None)]
    public class DemoPlayModeTests
    {
        [UnityTest]
        public IEnumerator RunAllMilestones_WhenDemoRegistered_Succeeds()
        {
            DemoPlayModeVerification.DisableAutoBootstrap = true;
            ResourcesManager.ReleaseAll();

            var strayHost = UnityEngine.Object.FindFirstObjectByType<DemoAddressableHost>();
            if (strayHost != null)
            {
                UnityEngine.Object.Destroy(strayHost.gameObject);
            }

            PathManager.Load();
            if (PathManager.Lookup("DemoBootSample.asset") == null)
            {
                Assert.Inconclusive(
                    "Demo PathMeta missing. Demo → 부트 샘플 등록 후 재실행.");
                yield break;
            }

            Task task = DemoPlayModeVerification.RunAllAsync();
            float timeoutAt = Time.realtimeSinceStartup + 60f;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > timeoutAt)
                {
                    throw new TimeoutException("DemoPlayModeVerification.RunAllAsync timed out.");
                }

                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception!.GetBaseException();
            }
        }
    }
}
