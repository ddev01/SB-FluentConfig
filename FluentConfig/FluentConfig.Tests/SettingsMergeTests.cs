using FluentConfig.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Spec for the simplified sync flow: receive a flat/nested values blob from the web
    /// (save RPC), merge via SettingsPathHelper, persist via SettingsManager.
    /// Replaces WPF control-tree walking in the old SettingsSynchronizer.
    ///
    /// replace local merge helper with the real Host API.
    /// </summary>
    public class SettingsMergeTests
    {
        /// <summary>
        /// Intended Host save path until SettingsManager owns this: apply each
        /// top-level / nested path from a values blob onto an existing settings object.
        /// </summary>
        private static JObject MergeValuesBlob(JObject existing, JObject valuesBlob)
        {
            var result = existing != null ? (JObject)existing.DeepClone() : new JObject();
            if (valuesBlob == null) return result;

            void Walk(JToken token, string path)
            {
                if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        var childPath = string.IsNullOrEmpty(path) ? prop.Name : path + "." + prop.Name;
                        if (prop.Value is JObject || prop.Value is JArray)
                            Walk(prop.Value, childPath);
                        else
                            SettingsPathHelper.SetNestedValue(result, childPath, prop.Value);
                    }
                }
                else if (token is JArray arr)
                {
                    // Whole arrays under a saveKey (pill-input, dynamic-textboxes) replace by path.
                    SettingsPathHelper.SetNestedValue(result, path, arr.DeepClone());
                }
            }

            foreach (var prop in valuesBlob.Properties())
            {
                if (prop.Value is JObject nested)
                    Walk(nested, prop.Name);
                else if (prop.Value is JArray arr)
                    SettingsPathHelper.SetNestedValue(result, prop.Name, arr.DeepClone());
                else
                    SettingsPathHelper.SetNestedValue(result, prop.Name, prop.Value);
            }

            return result;
        }

        [Fact]
        public void Merge_SimpleKeys_OverwritesAndPreservesOthers()
        {
            var existing = new JObject { ["keep"] = "yes", ["volume"] = 10 };
            var blob = new JObject { ["volume"] = 50 };

            var merged = MergeValuesBlob(existing, blob);

            Assert.Equal("yes", merged["keep"]?.ToString());
            Assert.Equal(50, merged["volume"]?.Value<int>());
        }

        [Fact]
        public void Merge_NestedDottedObject_UpdatesLeaf()
        {
            var existing = JObject.Parse(@"{ ""settings"": { ""timeout"": 10, ""retries"": 3 } }");
            var blob = JObject.Parse(@"{ ""settings"": { ""timeout"": 30 } }");

            var merged = MergeValuesBlob(existing, blob);

            Assert.Equal(30, merged["settings"]?["timeout"]?.Value<int>());
            // Current merge walks nested objects and only writes visited leaves —
            // sibling "retries" is preserved because we deep-cloned existing first.
            Assert.Equal(3, merged["settings"]?["retries"]?.Value<int>());
        }

        [Fact]
        public void Merge_RepeatableRows_ViaIndexedPaths()
        {
            var existing = new JObject();
            SettingsPathHelper.SetNestedValue(existing, "rows[0].name", "Old");
            SettingsPathHelper.SetNestedValue(existing, "rows[0].amount", 1);

            var blob = new JObject();
            // Simulate web save of nested-path style values (host may flatten before merge).
            SettingsPathHelper.SetNestedValue(blob, "rows[0].name", "New");
            SettingsPathHelper.SetNestedValue(blob, "rows[0].amount", 99);
            SettingsPathHelper.SetNestedValue(blob, "rows[1].name", "Second");
            SettingsPathHelper.SetNestedValue(blob, "rows[1].amount", 5);

            var merged = MergeValuesBlob(existing, blob);

            Assert.Equal("New", merged["rows"]?[0]?["name"]?.ToString());
            Assert.Equal(99, merged["rows"]?[0]?["amount"]?.Value<int>());
            Assert.Equal("Second", merged["rows"]?[1]?["name"]?.ToString());
            Assert.Equal(5, merged["rows"]?[1]?["amount"]?.Value<int>());
        }

        [Fact]
        public void Merge_PillNamesArray_ReplacesWholeArray()
        {
            var existing = new JObject { ["test_items"] = new JArray("Alpha") };
            var blob = new JObject { ["test_items"] = new JArray("Alpha", "Beta") };

            var merged = MergeValuesBlob(existing, blob);

            var arr = Assert.IsType<JArray>(merged["test_items"]);
            Assert.Equal(2, arr.Count);
            Assert.Equal("Beta", arr[1]?.ToString());
        }

        [Fact]
        public void Merge_NullBlob_ReturnsCloneOfExisting()
        {
            var existing = new JObject { ["a"] = 1 };
            var merged = MergeValuesBlob(existing, null);
            Assert.Equal(1, merged["a"]?.Value<int>());
            Assert.NotSame(existing, merged);
        }
    }
}
