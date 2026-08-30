using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentConfig;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Phase 2 host-side smoke: schema build, save RPC, dropdown.refresh, without Streamer.bot GUI.
    /// </summary>
    public class Phase2HostSmokeTests
    {
        [Fact]
        public void Schema_Build_IncludesToggleTextboxDropdownPill()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "phase2_smoke", "Phase2 Smoke", "0.2.0");
            ui.Section("General", "general", s => s
                .Toggle("Enabled", "smoke_enabled").Default(true)
                .Textbox("Display name", "smoke_name").Default("smoke-user")
                .Dropdown("Channel reward", "smoke_reward_display")
                    .WithPairValue("smoke_reward_id")
                    .Options(new[] { ("rew_1", "Reward Alpha"), ("rew_2", "Reward Beta") })
                    .Refresh(() => new[] { ("rew_1", "Reward Alpha"), ("rew_2", "Reward Beta"), ("rew_3", "Reward Gamma") })
                    .DefaultByValue("rew_2")
                .PillInput("Watch list", "smoke_pills")
                    .WithItemTemplate(item => item
                        .Title("Item: {name}")
                        .Toggle("Active", "{name}_active").Default(true)
                    )
            );

            var doc = ui.Session.BuildDocumentForTests();
            Assert.Equal("Phase2 Smoke", doc.Title);
            Assert.Single(doc.Sections);
            var types = FlattenTypes(doc.Sections[0].Children).ToList();
            Assert.Contains("toggle", types);
            Assert.Contains("textbox", types);
            Assert.Contains("dropdown", types);
            Assert.Contains("pill-input", types);

            var json = ProtocolJson.Serialize(doc);
            Assert.DoesNotContain("System.Windows.Controls.Panel", json);
            Assert.Contains("smoke_enabled", json);
            Assert.Contains("itemTemplate", json);
        }

        [Fact]
        public void SaveRpc_PersistsValues_ViaMockCph()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var cph = TestCph.CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "phase2_persist", "Phase2 Persist", "0.2.0");
            ui.Section("G", "g", s => s.Toggle("Enabled", "smoke_enabled").Textbox("Name", "smoke_name"));

            ui.Session.BuildDocumentForTests();

            var values = new JObject
            {
                ["smoke_enabled"] = false,
                ["smoke_name"] = "after-edit",
            };
            var req = ProtocolJson.Serialize(WireMessage.Request(1, RpcMethods.Save, new SaveParams { Values = values }));
            ui.Session.HandleWebMessage(req);

            var settings = ui.Session.GetSettingsForTests();
            Assert.Equal(false, settings["smoke_enabled"]?.Value<bool>());
            Assert.Equal("after-edit", settings["smoke_name"]?.ToString());

            var key = store.Keys.FirstOrDefault(k => k == "phase2_persist_settings");
            Assert.False(string.IsNullOrEmpty(key));
            var persisted = JObject.Parse(store[key]);
            Assert.Equal("after-edit", persisted["smoke_name"]?.ToString());
        }

        [Fact]
        public void DropdownRefresh_ReturnsFakeRewards()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "phase2_dropdown", "Phase2 Dropdown", "0.2.0");
            ui.Section("G", "g", s => s
                .Dropdown("Reward", "smoke_reward_display")
                    .Options(new[] { ("rew_1", "A") })
                    .Refresh(() => new[] { ("rew_1", "A"), ("rew_2", "B"), ("rew_3", "C") })
            );

            ui.Session.BuildDocumentForTests();

            // Refresh is registered during schema flush; invoke the stored callback directly.
            var refreshField = typeof(FluentConfigSession).GetField("_dropdownRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshField);
            var map = (Dictionary<string, Func<IList<DropdownOption>>>)refreshField.GetValue(ui.Session);
            Assert.True(map.ContainsKey("smoke_reward_display"));
            var options = map["smoke_reward_display"]();
            Assert.Equal(3, options.Count);
            Assert.Equal("rew_3", options[2].Value);
        }

        [Fact]
        public void RefreshOnlyDropdown_ShipsEmptyOptions()
        {
            var refreshCalls = 0;
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "refresh_seed", "Refresh Seed", "1.0");
            ui.Section("G", "g", s => s
                .Dropdown("Groups", "include_groups")
                    .Refresh(() =>
                    {
                        refreshCalls++;
                        return new[] { "vip", "sub", "mod" };
                    })
            );

            var doc = ui.Session.BuildDocumentForTests();
            var node = Assert.IsType<DropdownNode>(doc.Sections[0].Children.Single(n => n is DropdownNode));
            Assert.True(node.Refreshable);
            Assert.True(node.Options == null || node.Options.Count == 0);
            Assert.Equal(0, refreshCalls);

            var refreshField = typeof(FluentConfigSession).GetField("_dropdownRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshField);
            var map = (Dictionary<string, Func<IList<DropdownOption>>>)refreshField.GetValue(ui.Session);
            Assert.True(map.ContainsKey("include_groups"));
            Assert.Equal(3, map["include_groups"]().Count);
            Assert.Equal(1, refreshCalls);
        }

        [Fact]
        public void RefreshOnlyPairDropdown_BehindVisibility_DoesNotEagerRefresh()
        {
            var refreshCalls = 0;
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "refresh_pair_vis", "Refresh Pair Vis", "1.0");
            ui.Section("G", "g", s => s
                .Toggle("Use reward", "use_reward").Default(true)
                .WithVisibility("use_reward", inner => inner
                    .Dropdown("Reward", "reward_display")
                        .WithPairValue("reward_id")
                        .DefaultByValue("rew_2")
                        .Refresh(() =>
                        {
                            refreshCalls++;
                            return new[] { ("rew_1", "Alpha"), ("rew_2", "Beta") };
                        })
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            Assert.Equal(0, refreshCalls);

            var group = Assert.IsType<GroupNode>(doc.Sections[0].Children.Single(n => n is GroupNode));
            Assert.Equal("use_reward", group.Visibility.SaveKey);
            var node = Assert.IsType<DropdownNode>(Assert.Single(group.Children.OfType<DropdownNode>()));
            Assert.True(node.Refreshable);
            Assert.True(node.Options == null || node.Options.Count == 0);
            Assert.Equal("rew_2", node.DefaultByValue);
            Assert.Equal("reward_id", node.ValueSaveKey);

            // Empty-options collector seeds the id only — gated first paint uses this, not Refresh.
            Assert.Equal("rew_2", doc.Values.Value<string>("reward_id"));
            Assert.False(doc.Values.ContainsKey("reward_display"));
            Assert.True(doc.Values.Value<bool>("use_reward"));

            var refreshField = typeof(FluentConfigSession).GetField("_dropdownRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshField);
            var map = (Dictionary<string, Func<IList<DropdownOption>>>)refreshField.GetValue(ui.Session);
            Assert.True(map.ContainsKey("reward_display"));
            var options = map["reward_display"]();
            Assert.Equal(1, refreshCalls);
            Assert.Equal(2, options.Count);
            Assert.Equal("rew_2", options[1].Value);
        }

        [Fact]
        public void DropdownWithStaticOptions_DoesNotSeedFromRefresh()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "static_options", "Static Options", "1.0");
            ui.Section("G", "g", s => s
                .Dropdown("Choice", "dropdown_choice")
                    .Options(new[] { "A", "B" })
                    .Refresh(() => new[] { "A", "B", "C" })
            );

            var doc = ui.Session.BuildDocumentForTests();
            var node = Assert.IsType<DropdownNode>(doc.Sections[0].Children.Single(n => n is DropdownNode));
            Assert.Equal(2, node.Options.Count);
            Assert.Equal("A", node.Options[0].Value);
        }

        [Fact]
        public void WithVisibilityWhenOff_EmitsInvertedCondition()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "phase2_vis", "Phase2 Vis", "0.2.0");
            ui.Section("G", "g", s => s
                .Toggle("Premium", "premium_mode")
                .WithVisibilityWhenOff("premium_mode", inner => inner
                    .Textbox("Free", "free_tier")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var json = ProtocolJson.Serialize(doc);
            Assert.Contains("\"inverted\":true", json.Replace(" ", ""));
            Assert.Contains("free_tier", json);
        }

        private static IEnumerable<string> FlattenTypes(IEnumerable<SchemaNode> nodes)
        {
            if (nodes == null) yield break;
            foreach (var n in nodes)
            {
                yield return n.Type;
                if (n is GroupNode g)
                {
                    foreach (var c in FlattenTypes(g.Children))
                        yield return c;
                }
            }
        }
    }
}
