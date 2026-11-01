using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class SyncCacheSeedTests
    {
        const string TestPath = "Assets/Addressables/SyncCacheSeedTest.asset";

        ScriptableObject _seedAsset;

        [SetUp]
        public void SetUp()
        {
            ResourcesManager.ReleaseAll();
            _seedAsset = ScriptableObject.CreateInstance<ScriptableObject>();
        }

        [TearDown]
        public void TearDown()
        {
            ResourcesManager.ClearSyncCacheSeeds();
            ResourcesManager.ReleaseAll();
            if (_seedAsset != null)
            {
                Object.DestroyImmediate(_seedAsset);
            }
        }

        [Test]
        public void RegisterSyncCacheHit_LoadResource_ReturnsSeededAsset()
        {
            ResourcesManager.RegisterSyncCacheHit(TestPath, _seedAsset);

            ScriptableObject loaded = ResourcesManager.LoadResource<ScriptableObject>(TestPath);

            Assert.That(loaded, Is.SameAs(_seedAsset));
            Assert.That(ResourcesManager.IsResourceLoaded(TestPath), Is.True);
        }

        [Test]
        public void ClearSyncCacheSeeds_AfterSeed_LoadResourceMisses()
        {
            ResourcesManager.RegisterSyncCacheHit(TestPath, _seedAsset);
            ResourcesManager.ClearSyncCacheSeeds();

            ScriptableObject loaded = ResourcesManager.LoadResource<ScriptableObject>(TestPath);

            Assert.That(loaded, Is.Null);
            Assert.That(ResourcesManager.IsResourceLoaded(TestPath), Is.False);
        }

        [Test]
        public void ReleaseDirect_DoesNotEvictSeedOwner()
        {
            ResourcesManager.RegisterSyncCacheHit(TestPath, _seedAsset);

            ResourcesManager.Release(TestPath);

            ScriptableObject loaded = ResourcesManager.LoadResource<ScriptableObject>(TestPath);

            Assert.That(loaded, Is.SameAs(_seedAsset));
        }
    }
}
