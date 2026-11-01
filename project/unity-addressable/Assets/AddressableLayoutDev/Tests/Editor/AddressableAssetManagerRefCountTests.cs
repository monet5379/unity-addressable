using System.Collections.Generic;
using AddressableLayout.Resource;
using NUnit.Framework;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class AddressableAssetManagerRefCountTests
    {
        [Test]
        public void CopyLoadedLabels_EmptyWhenNoAcquires()
        {
            var manager = new AddressableAssetManager();
            var destination = new List<(string Label, int RefCount, int AssetCount)>();

            manager.CopyLoadedLabels(destination);

            Assert.That(destination, Is.Empty);
        }

        [Test]
        public void ReleaseLabel_UnknownLabel_DoesNotThrow()
        {
            var manager = new AddressableAssetManager();
            Assert.DoesNotThrow(() => manager.ReleaseLabel("missing-label"));
        }

        [Test]
        public void ReleaseLabelRef_UnknownLabel_ReturnsFalse()
        {
            var manager = new AddressableAssetManager();
#pragma warning disable 618
            Assert.That(manager.ReleaseLabelRef("missing-label"), Is.False);
#pragma warning restore 618
        }
    }
}
