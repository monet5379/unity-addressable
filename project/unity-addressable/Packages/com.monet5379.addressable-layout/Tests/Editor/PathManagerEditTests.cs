using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AddressableLayout.Editor;
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

        [Test]
        public void StringKeyedMap_DifferentInsertionOrder_SerializesToSameJson()
        {
            var first = new Dictionary<string, string>
            {
                { "Z.asset", "z" },
                { "A.asset", "a" },
                { "M.asset", "m" },
            };
            var second = new Dictionary<string, string>
            {
                { "M.asset", "m" },
                { "Z.asset", "z" },
                { "A.asset", "a" },
            };

            string jsonFirst = JsonUtility.ToJson(new StringKeyedMap(first));
            string jsonSecond = JsonUtility.ToJson(new StringKeyedMap(second));

            Assert.That(jsonSecond, Is.EqualTo(jsonFirst));
        }

        [Test]
        public void BuildDatabase_ResourcesDuplicate_KeepsOrdinalFirstFullPath()
        {
            string assetsRoot = CreateTempAssetsRoot();
            try
            {
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "b", "Dup.prefab"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "a", "Dup.prefab"));

                Dictionary<string, string> database = PathMetaWriter.BuildDatabase(assetsRoot);

                Assert.That(database.Count, Is.EqualTo(1));
                Assert.That(database["Dup.prefab"], Is.EqualTo("a/Dup"));
            }
            finally
            {
                DeleteTempAssetsRoot(assetsRoot);
            }
        }

        [Test]
        public void BuildDatabase_ResourcesWinsOverAddressablesForSameFileName()
        {
            string assetsRoot = CreateTempAssetsRoot();
            try
            {
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "Shared.prefab"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Addressables", "Shared.prefab"));

                Dictionary<string, string> database = PathMetaWriter.BuildDatabase(assetsRoot);

                Assert.That(database["Shared.prefab"], Is.EqualTo("Shared"));
            }
            finally
            {
                DeleteTempAssetsRoot(assetsRoot);
            }
        }

        [Test]
        public void BuildDatabase_IgnoresMetaShaderPathMetaDataAndDotFiles()
        {
            string assetsRoot = CreateTempAssetsRoot();
            try
            {
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "skip.meta"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "shader.SHADER"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "Data", "PathMetaData.json"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", ".hidden.txt"));
                WriteEmptyFile(Path.Combine(assetsRoot, "Resources", "Keep.prefab"));

                Dictionary<string, string> database = PathMetaWriter.BuildDatabase(assetsRoot);

                Assert.That(database.ContainsKey("skip.meta"), Is.False);
                Assert.That(database.ContainsKey("shader.SHADER"), Is.False);
                Assert.That(database.ContainsKey("PathMetaData.json"), Is.False);
                Assert.That(database.ContainsKey(".hidden.txt"), Is.False);
                Assert.That(database["Keep.prefab"], Is.EqualTo("Keep"));
            }
            finally
            {
                DeleteTempAssetsRoot(assetsRoot);
            }
        }

        [Test]
        public void BuildDatabase_AddressableEntriesUseAssetsPrefix()
        {
            string assetsRoot = CreateTempAssetsRoot();
            try
            {
                WriteEmptyFile(Path.Combine(assetsRoot, "Addressables", "X.prefab"));

                Dictionary<string, string> database = PathMetaWriter.BuildDatabase(assetsRoot);

                Assert.That(database["X.prefab"], Does.StartWith("Assets/"));
            }
            finally
            {
                DeleteTempAssetsRoot(assetsRoot);
            }
        }

        private static string CreateTempAssetsRoot()
        {
            string path = Path.Combine(Path.GetTempPath(), "PathMetaWriterTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void WriteEmptyFile(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, string.Empty);
        }

        private static void DeleteTempAssetsRoot(string assetsRoot)
        {
            if (Directory.Exists(assetsRoot))
            {
                Directory.Delete(assetsRoot, recursive: true);
            }
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
