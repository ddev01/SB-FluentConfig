using Newtonsoft.Json.Linq;
using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    public class SettingsPathHelperTests
    {
        // --- ParseNestedPath ---

        [Fact]
        public void ParseNestedPath_SimpleName_ReturnsSinglePart()
        {
            var parts = SettingsPathHelper.ParseNestedPath("myKey");
            Assert.Single(parts);
            Assert.Equal("myKey", parts[0]);
        }

        [Fact]
        public void ParseNestedPath_DottedPath_SplitsOnDots()
        {
            var parts = SettingsPathHelper.ParseNestedPath("settings.timeout");
            Assert.Equal(2, parts.Length);
            Assert.Equal("settings", parts[0]);
            Assert.Equal("timeout", parts[1]);
        }

        [Fact]
        public void ParseNestedPath_BracketIndex_SplitsKeyAndIndex()
        {
            var parts = SettingsPathHelper.ParseNestedPath("rows[0]");
            Assert.Equal(2, parts.Length);
            Assert.Equal("rows", parts[0]);
            Assert.Equal("0", parts[1]);
        }

        [Fact]
        public void ParseNestedPath_Complex_SplitsAllParts()
        {
            var parts = SettingsPathHelper.ParseNestedPath("voice_aliases[2].name");
            Assert.Equal(3, parts.Length);
            Assert.Equal("voice_aliases", parts[0]);
            Assert.Equal("2", parts[1]);
            Assert.Equal("name", parts[2]);
        }

        [Fact]
        public void ParseNestedPath_EmptyString_ReturnsEmpty()
        {
            var parts = SettingsPathHelper.ParseNestedPath("");
            Assert.Empty(parts);
        }

        // --- SetNestedValue ---

        [Fact]
        public void SetNestedValue_SimpleKey_SetsDirectly()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "name", "Alice");
            Assert.Equal("Alice", root["name"]?.ToString());
        }

        [Fact]
        public void SetNestedValue_DottedKey_CreatesNestedObject()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "settings.timeout", 30);
            Assert.Equal(30, root["settings"]?["timeout"]?.Value<int>());
        }

        [Fact]
        public void SetNestedValue_ArrayIndex_CreatesArray()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "items[0]", "first");
            Assert.IsType<JArray>(root["items"]);
            Assert.Equal("first", ((JArray)root["items"])[0]?.ToString());
        }

        [Fact]
        public void SetNestedValue_ArrayWithNestedObject_CreatesStructure()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "rows[0].name", "Row1");
            SettingsPathHelper.SetNestedValue(root, "rows[0].value", 42);
            Assert.Equal("Row1", root["rows"]?[0]?["name"]?.ToString());
            Assert.Equal(42, root["rows"]?[0]?["value"]?.Value<int>());
        }

        [Fact]
        public void SetNestedValue_NullRoot_DoesNotThrow()
        {
            SettingsPathHelper.SetNestedValue(null, "key", "value");
            // No exception expected
        }

        [Fact]
        public void SetNestedValue_NullPath_DoesNotThrow()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, null, "value");
            Assert.Empty(root);
        }

        [Fact]
        public void SetNestedValue_EmptyPath_DoesNotThrow()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "", "value");
            Assert.Empty(root);
        }

        [Fact]
        public void SetNestedValue_OverwritesExistingValue()
        {
            var root = new JObject { ["key"] = "old" };
            SettingsPathHelper.SetNestedValue(root, "key", "new");
            Assert.Equal("new", root["key"]?.ToString());
        }

        [Fact]
        public void SetNestedValue_SparseArray_PadsWithDefaults()
        {
            var root = new JObject();
            SettingsPathHelper.SetNestedValue(root, "items[2]", "third");
            var arr = (JArray)root["items"];
            Assert.Equal(3, arr.Count);
            Assert.Equal("third", arr[2]?.ToString());
        }
    }
}
