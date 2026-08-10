using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentConfig;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Opening the UI seeds missing schema defaults into the global var (overwrite-safe).
    /// Runtime actions are expected to read via <c>FluentConfig.LoadSettings&lt;T&gt;</c>
    /// (or CPH.GetGlobalVar on <c>{slug}_settings</c>) after the user opens the menu once.
    /// </summary>
    public class SettingsDefaultsTests
    {
        private static void Configure(FluentConfigUi ui) => ui
            .Section("G", "g", s => s
                .Toggle("Enabled", "enabled").Default(true)
                .Slider("Volume", "volume").Range(0, 100).Default(50)
                .Textbox("Name", "name").Default("Ada")
                .Dropdown("Choice", "choice")
                    .Options(new[] { "A", "B", "C" })
                    .DefaultIndex(1)
            );

        [Fact]
        public void BuildDocument_SeedsMissingDefaults_DoesNotOverwriteExisting()
        {
            var store = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store);
            var key = "defaults_seed_settings";
            store[key] = new JObject { ["volume"] = 90 }.ToString();

            var ui = FluentConfigUi.Create(cph.Object, "Defaults Seed", "1.0");
            Configure(ui);
            var doc = ui.Session.BuildDocumentForTests();

            Assert.Equal(90, doc.Values.Value<int>("volume"));
            Assert.True(doc.Values.Value<bool>("enabled"));
            Assert.Equal("Ada", doc.Values.Value<string>("name"));
            Assert.Equal("B", doc.Values.Value<string>("choice"));

            // Second open must not clobber the user value.
            var ui2 = FluentConfigUi.Create(cph.Object, "Defaults Seed", "1.0");
            Configure(ui2);
            var doc2 = ui2.Session.BuildDocumentForTests();
            Assert.Equal(90, doc2.Values.Value<int>("volume"));
        }

        [Fact]
        public void SeedMissingDefaults_SkipsPresentKeys()
        {
            var manager = new SettingsManager(null, "test");
            manager.ReplaceSettings(new JObject { ["a"] = 1 });

            var defaults = new Dictionary<string, JToken>
            {
                ["a"] = 99,
                ["b"] = "x",
            };

            var added = SettingsSync.SeedMissingDefaults(manager, defaults, persist: false);
            Assert.Equal(1, added);
            Assert.Equal(1, manager.GetValue<int>("a"));
            Assert.Equal("x", manager.GetValue<string>("b"));
        }

        [Fact]
        public void Collector_PairDropdown_WritesDisplayAndValueKeys()
        {
            var sections = new List<SectionSchema>
            {
                new SectionSchema
                {
                    Id = "g",
                    Title = "G",
                    Children = new List<SchemaNode>
                    {
                        new DropdownNode
                        {
                            SaveKey = "device_display",
                            ValueSaveKey = "device_id",
                            DefaultByValue = "id2",
                            Options = new List<DropdownOption>
                            {
                                new DropdownOption { Value = "id1", Display = "Device A" },
                                new DropdownOption { Value = "id2", Display = "Device B" },
                            },
                        },
                    },
                },
            };

            var defaults = SettingsDefaultsCollector.Collect(sections, new JObject());
            Assert.Equal("Device B", defaults["device_display"]?.Value<string>());
            Assert.Equal("id2", defaults["device_id"]?.Value<string>());
        }
    }
}
