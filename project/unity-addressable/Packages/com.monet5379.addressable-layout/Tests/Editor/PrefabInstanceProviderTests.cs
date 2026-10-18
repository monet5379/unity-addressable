using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class PrefabInstanceProviderTests
    {
        [TearDown]
        public void TearDown()
        {
            ResourcesManager.SetInstanceProvider(null);
        }

        [Test]
        public void Despawn_UsesProvider_AndSkipsNull()
        {
            var provider = new RecordingProvider();
            ResourcesManager.SetInstanceProvider(provider);
            var go = new GameObject("x");

            ResourcesManager.Despawn(go);
            ResourcesManager.Despawn(null);

            Assert.That(provider.Despawned, Is.EqualTo(1));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SpawnPrefab_EmptyName_DoesNotCallProvider()
        {
            var provider = new RecordingProvider();
            ResourcesManager.SetInstanceProvider(provider);

            Assert.That(ResourcesManager.SpawnPrefab(""), Is.Null);
            Assert.That(ResourcesManager.SpawnPrefab(null), Is.Null);
            Assert.That(provider.SpawnCalls, Is.EqualTo(0));
        }

        sealed class RecordingProvider : IPrefabInstanceProvider
        {
            public int Despawned;
            public int SpawnCalls;

            public GameObject Spawn(GameObject prefab, Transform parent)
            {
                SpawnCalls++;
                return null;
            }

            public void Despawn(GameObject instance) => Despawned++;
        }
    }
}
