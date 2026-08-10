using System;
using FluentConfig.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Exercises the real Host save-merge path:
    /// <see cref="SettingsPathHelper.MergeValues"/> and <see cref="SettingsSync.ApplyAndSave"/>.
    /// </summary>
    public class SettingsMergeTests
    {
        private static SettingsManager CreateManager(JObject seed = null)
        {
            // null CPH: Save is a no-op; ReplaceSettings / GetSettings exercise the merge path.
            var mgr = new SettingsManager(null, "mergetest_settings");
            if (seed != null)
                mgr.ReplaceSettings((JObject)seed.DeepClone());
            return mgr;
        }

        [Fact]
        public void Merge_SimpleKeys_OverwritesAndPreservesOthers()
        {
            var existing = new JObject { ["keep"] = "yes", ["volume"] = 10 };
            var blob = new JObject { ["volume"] = 50 };

            var target = (JObject)existing.DeepClone();
            SettingsPathHelper.MergeValues(target, blob);

            Assert.Equal("yes", target["keep"]?.ToString());
            Assert.Equal(50, target["volume"]?.Value<int>());
        }

        [Fact]
        public void Merge_NestedDottedObject_UpdatesLeaf()
        {
            var existing = JObject.Parse(@"{ ""settings"": { ""timeout"": 10, ""retries"": 3 } }");
            var blob = JObject.Parse(@"{ ""settings"": { ""timeout"": 30 } }");

            var target = (JObject)existing.DeepClone();
            SettingsPathHelper.MergeValues(target, blob);

            Assert.Equal(30, target["settings"]?["timeout"]?.Value<int>());
            Assert.Equal(3, target["settings"]?["retries"]?.Value<int>());
        }

        [Fact]
        public void ApplyAndSave_PersistsMergedBlob()
        {
            var mgr = CreateManager(new JObject { ["keep"] = "yes", ["volume"] = 10 });
            var blob = new JObject { ["volume"] = 50 };

            var saved = SettingsSync.ApplyAndSave(mgr, blob);

            Assert.Equal("yes", saved["keep"]?.ToString());
            Assert.Equal(50, saved["volume"]?.Value<int>());
            Assert.Equal(50, mgr.GetSettings()?["volume"]?.Value<int>());
        }

        [Fact]
        public void Merge_RepeatableRows_ViaIndexedPaths()
        {
            var existing = new JObject();
            SettingsPathHelper.SetNestedValue(existing, "rows[0].name", "Old");
            SettingsPathHelper.SetNestedValue(existing, "rows[0].amount", 1);

            // Whole-array replace via MergeValues (how save blobs typically arrive).
            var blob = new JObject
            {
                ["rows"] = new JArray
                {
                    new JObject { ["name"] = "New", ["amount"] = 99 },
                    new JObject { ["name"] = "Second", ["amount"] = 5 },
                }
            };

            SettingsPathHelper.MergeValues(existing, blob);

            Assert.Equal("New", existing["rows"]?[0]?["name"]?.ToString());
            Assert.Equal(99, existing["rows"]?[0]?["amount"]?.Value<int>());
            Assert.Equal("Second", existing["rows"]?[1]?["name"]?.ToString());
            Assert.Equal(5, existing["rows"]?[1]?["amount"]?.Value<int>());
        }

        [Fact]
        public void Merge_PillNamesArray_ReplacesWholeArray()
        {
            var existing = new JObject { ["test_items"] = new JArray("Alpha") };
            var blob = new JObject { ["test_items"] = new JArray("Alpha", "Beta") };

            SettingsPathHelper.MergeValues(existing, blob);

            var arr = Assert.IsType<JArray>(existing["test_items"]);
            Assert.Equal(2, arr.Count);
            Assert.Equal("Beta", arr[1]?.ToString());
        }

        [Fact]
        public void Merge_NullBlob_LeavesTargetUnchanged()
        {
            var existing = new JObject { ["a"] = 1 };
            var before = existing.ToString();
            SettingsPathHelper.MergeValues(existing, null);
            Assert.Equal(before, existing.ToString());
        }

        [Fact]
        public void RemoveNestedValue_SplicesArrayElement()
        {
            var root = new JObject { ["items"] = new JArray("a", "b", "c") };
            SettingsPathHelper.RemoveNestedValue(root, "items[1]");
            var arr = Assert.IsType<JArray>(root["items"]);
            Assert.Equal(2, arr.Count);
            Assert.Equal("a", arr[0]?.ToString());
            Assert.Equal("c", arr[1]?.ToString());
        }

        [Fact]
        public void SetNestedValue_ThrowsOnTypeMismatch()
        {
            var root = new JObject { ["scalar"] = "hello" };
            var ex = Assert.Throws<InvalidOperationException>(
                () => SettingsPathHelper.SetNestedValue(root, "scalar.child", 1));
            Assert.Contains("type mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetNestedValue_ReadsIndexedPath()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "rows[0].name", "A");
            Assert.Equal("A", SettingsPathHelper.GetNestedValue(root, "rows[0].name")?.ToString());
        }
    }
}
