using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class CacheOwnershipTests
    {
        CacheOwnership _cache;
        Texture2D _asset;

        [SetUp]
        public void SetUp()
        {
            _cache = new CacheOwnership();
            _asset = new Texture2D(1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_asset != null)
            {
                Object.DestroyImmediate(_asset);
            }

            _cache = null;
        }

        [Test]
        public void AliasAndPrimaryKey_BothTryGetAndResolveToPrimary()
        {
            const string primary = "assets/sample.png";
            const string alias = "internal/sample-id";

            _cache.Add(primary, _asset, "boot");
            _cache.AddAlias(primary, alias);

            Assert.That(_cache.TryGet<Texture2D>(primary, out Texture2D fromPrimary), Is.True);
            Assert.That(fromPrimary, Is.SameAs(_asset));
            Assert.That(_cache.TryGet<Texture2D>(alias, out Texture2D fromAlias), Is.True);
            Assert.That(fromAlias, Is.SameAs(_asset));
            Assert.That(_cache.TryResolve(alias, out string resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(primary));
            Assert.That(_cache.Contains(primary), Is.True);
            Assert.That(_cache.Contains(alias), Is.True);
        }

        [Test]
        public void LabelAndDirect_ReleaseLabelKeepsEntryUntilDirectReleased()
        {
            const string primary = "assets/shared.png";
            const string alias = "alias/shared";
            const string label = "boot";

            _cache.Add(primary, _asset, label);
            _cache.Add(primary, _asset, CacheOwnership.Direct);
            _cache.AddAlias(primary, alias);

            _cache.ReleaseOwner(label);

            Assert.That(_cache.Contains(primary), Is.True);
            Assert.That(_cache.Contains(alias), Is.True);
            Assert.That(_cache.HasOwner(primary, CacheOwnership.Direct), Is.True);
            Assert.That(_cache.HasOwner(primary, label), Is.False);

            Assert.That(_cache.Release(primary, CacheOwnership.Direct), Is.True);

            Assert.That(_cache.Contains(primary), Is.False);
            Assert.That(_cache.Contains(alias), Is.False);
            Assert.That(_cache.TryGet<Texture2D>(primary, out _), Is.False);
            Assert.That(_cache.TryGet<Texture2D>(alias, out _), Is.False);
        }

        [Test]
        public void TwoLabelsSharingPrimary_EvictsOnlyWhenLastLabelReleased()
        {
            const string primary = "assets/multi-label.png";
            const string labelA = "label-a";
            const string labelB = "label-b";

            _cache.Add(primary, _asset, labelA);
            _cache.Add(primary, _asset, labelB);

            _cache.ReleaseOwner(labelA);

            Assert.That(_cache.Contains(primary), Is.True);
            Assert.That(_cache.HasOwner(primary, labelB), Is.True);

            _cache.ReleaseOwner(labelB);

            Assert.That(_cache.Contains(primary), Is.False);
        }

        [Test]
        public void ReleaseOwner_IsIdempotentAndReleaseReturnsFalseForMissingOwner()
        {
            const string primary = "assets/idempotent.png";

            _cache.Add(primary, _asset, "only-label");

            Assert.DoesNotThrow(() => _cache.ReleaseOwner("missing-label"));
            Assert.DoesNotThrow(() => _cache.ReleaseOwner("missing-label"));

            Assert.That(_cache.Release(primary, "missing-owner"), Is.False);
            Assert.That(_cache.Contains(primary), Is.True);

            _cache.ReleaseOwner("only-label");
            Assert.That(_cache.Contains(primary), Is.False);
        }

        [Test]
        public void Clear_RemovesAllKeys()
        {
            const string primary = "assets/clear-me.png";
            const string alias = "alias/clear-me";

            _cache.Add(primary, _asset, CacheOwnership.Direct);
            _cache.AddAlias(primary, alias);

            _cache.Clear();

            Assert.That(_cache.Contains(primary), Is.False);
            Assert.That(_cache.Contains(alias), Is.False);
            Assert.That(_cache.TryResolve(primary, out _), Is.False);
        }
    }
}
