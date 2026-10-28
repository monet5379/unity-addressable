using System.Collections.Generic;
using System.Reflection;
using AddressableLayout.Resource;
using NUnit.Framework;
using UnityEngine;

namespace AddressableLayout.Tests
{
    [TestFixture]
    public class PathManagerEditTests
    {
        [SetUp]
        [TearDown]
        public void ResetPathManagerState()
        {
            InvokeResetStatics();
        }

        [Test]
        public void ParseDatabase_NullOrWhitespace_ReturnsEmptyDictionary()
        {
            Assert.That(PathManager.ParseDatabase(null).Count, Is.EqualTo(0));
            Assert.That(PathManager.ParseDatabase(string.Empty).Count, Is.EqualTo(0));
            Assert.That(PathManager.ParseDatabase("   ").Count, Is.EqualTo(0));
        }

        [Test]
        public void ParseDatabase_InvalidJson_ReturnsEmptyWithoutThrow()
        {
            Dictionary<string, string> result = PathManager.ParseDatabase("{ not valid json");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void ParseDatabase_ValidJson_ReturnsEntries()
        {
            var source = new Dictionary<string, string>
            {
                { "Foo.prefab", "Assets/Addressables/Foo.prefab" },
                { "Bar.png", "Demo/Bar" },
            };
            string json = JsonUtility.ToJson(new StringKeyedMap(source));

            Dictionary<string, string> parsed = PathManager.ParseDatabase(json);

            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That(parsed["Foo.prefab"], Is.EqualTo("Assets/Addressables/Foo.prefab"));
            Assert.That(parsed["Bar.png"], Is.EqualTo("Demo/Bar"));
        }

        [Test]
        public void ParseDatabase_EmptyObjectJson_ReturnsEmptyDictionary()
        {
            Assert.That(PathManager.ParseDatabase("{}").Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckLoaded_BeforeAnyLoad_ReturnsFalse()
        {
            Assert.That(PathManager.CheckLoaded(), Is.False);
        }

        [Test]
        public void ApplyBuiltDatabase_WithEntries_SetsCheckLoadedAndTryLookup()
        {
            var database = new Dictionary<string, string> { { "Key.asset", "Assets/Addressables/Key.asset" } };
            PathManager.ApplyBuiltDatabase(database);

            Assert.That(PathManager.CheckLoaded(), Is.True);
            Assert.That(PathManager.TryLookup("Key.asset", out string path), Is.True);
            Assert.That(path, Is.EqualTo("Assets/Addressables/Key.asset"));
        }

        [Test]
        public void ApplyBuiltDatabase_EmptyDictionary_CheckLoadedFalse()
        {
            PathManager.ApplyBuiltDatabase(new Dictionary<string, string>());

            Assert.That(PathManager.CheckLoaded(), Is.False);
        }

        [Test]
        public void TryLookup_MissingKey_ReturnsFalseAndNullPath()
        {
            PathManager.ApplyBuiltDatabase(new Dictionary<string, string> { { "Only.asset", "Demo/Only" } });

            Assert.That(PathManager.TryLookup("Missing.prefab", out string path), Is.False);
            Assert.That(path, Is.Null);
        }

        [Test]
        public void Load_CalledTwice_DoesNotChangeEntryCount()
        {
            if (!TryEnsureDemoPathMeta(out string skipReason))
            {
                Assert.Ignore(skipReason);
            }

            PathManager.Load();
            int afterFirst = PathManager.LoadedEntryCount;
            Assert.That(afterFirst, Is.GreaterThan(0));
            Assert.That(PathManager.CheckLoaded(), Is.True);

            PathManager.Load();
            Assert.That(PathManager.LoadedEntryCount, Is.EqualTo(afterFirst));
            Assert.That(PathManager.CheckLoaded(), Is.True);
        }

        [Test]
        public void FindPrefabPath_UsesTryLookup_OnMissReturnsEmptyNotNull()
        {
            PathManager.ApplyBuiltDatabase(new Dictionary<string, string>());

            string path = PathManager.FindPrefabPath("NoSuchPrefab");

            Assert.That(path, Is.EqualTo(string.Empty));
        }

        private static void InvokeResetStatics()
        {
            MethodInfo reset = typeof(PathManager).GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null, "PathManager.ResetStatics missing");
            reset.Invoke(null, null);
        }

        private static bool TryEnsureDemoPathMeta(out string skipReason)
        {
            skipReason = null;
            PathManager.Load();
            if (PathManager.TryLookup("DemoBootSample.asset", out _))
            {
                InvokeResetStatics();
                return true;
            }

            skipReason = "Demo PathMeta missing. Run PrepareDemo or register boot sample.";
            return false;
        }
    }
}
