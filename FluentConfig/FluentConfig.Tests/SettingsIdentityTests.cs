using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class SettingsIdentityTests
    {
        [Fact]
        public void RenameDisplayTitle_DoesNotOrphanSettings_WhenIdIsStable()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var key = SettingsKeyHelper.SettingsKeyFor("alerts");
            store[key] = new JObject { ["volume"] = 42 }.ToString();
            var cph = TestCph.CreateMockCph(store);

            var ui = FluentConfigUi.Create(cph.Object, "alerts", "Alerts (renamed)", "2.0");
            ui.Section("G", "g", s => s.Slider("Volume", "volume").Range(0, 100).Default(10));
            var doc = ui.Session.BuildDocumentForTests();

            Assert.Equal("Alerts (renamed)", doc.Title);
            Assert.Equal(42, doc.Values.Value<int>("volume"));
            Assert.Equal(key, SettingsKeyHelper.SettingsKeyFor(ui.Session.ScopeKey));
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("Foo!")]
        [InlineData("Foo ")]
        public void SlugCollisionTitles_AreRejectedAsIds(string id)
        {
            var cph = TestCph.CreateMockCph();
            Assert.Throws<ArgumentException>(() => FluentConfigUi.Create(cph.Object, id, "Foo", "1.0"));
        }

        [Fact]
        public void Collector_OptionlessDropdown_SeedsDefaultByValueOnIdKey()
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
                            SaveKey = "reward_display",
                            ValueSaveKey = "reward_id",
                            DefaultByValue = "rew_2",
                            Options = new List<DropdownOption>(),
                        },
                    },
                },
            };

            var defaults = SettingsDefaultsCollector.Collect(sections, new JObject());
            Assert.Equal("rew_2", defaults["reward_id"]?.Value<string>());
            Assert.False(defaults.ContainsKey("reward_display"));
        }

        [Fact]
        public void Collector_OptionlessDropdown_WithoutPair_SeedsSaveKey()
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
                            SaveKey = "mode",
                            DefaultByValue = "quiet",
                            Options = null,
                        },
                    },
                },
            };

            var defaults = SettingsDefaultsCollector.Collect(sections, new JObject());
            Assert.Equal("quiet", defaults["mode"]?.Value<string>());
        }

        [Fact]
        public void SeedMissingDefaults_DoesNotOverwritePresentEmptyString()
        {
            var manager = new SettingsManager(null, "test");
            manager.ReplaceSettings(new JObject { ["reward_id"] = "" });

            var defaults = new Dictionary<string, JToken>
            {
                ["reward_id"] = "rew_2",
            };

            var added = SettingsSync.SeedMissingDefaults(manager, defaults, persist: false);
            Assert.Equal(0, added);
            Assert.Equal("", manager.GetValue<string>("reward_id"));
        }
    }

    [Collection("StaWpf")]
    public class SettingsIdentityReservationTests
    {
        [Fact]
        public void PlaceholderReservation_BlocksSecondOpener()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    Assert.True(FluentConfigWindowManager.TryBeginOpen("alerts", "1.0", null));
                    Assert.False(FluentConfigWindowManager.TryBeginOpen("alerts", "1.0", null));
                    Assert.False(FluentConfigWindowManager.AlreadyOpened("alerts", "1.0", null));

                    var window = new Window();
                    FluentConfigWindowManager.Register("alerts", window);
                    Assert.True(FluentConfigWindowManager.AlreadyOpened("alerts", "1.0", null));
                    Assert.False(FluentConfigWindowManager.TryBeginOpen("alerts", "1.0", null));
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void NotifyClosedForTests_ClearsReservation_SecondTryBeginOpenSucceeds()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    var cph = TestCph.CreateMockCph();
                    var ui = FluentConfigUi.Create(cph.Object, "menu_a", "Menu A", "1.0");
                    var scope = ui.Session.ScopeKey;
                    Assert.True(FluentConfigWindowManager.TryBeginOpen(scope, "1.0", null));
                    ui.MarkOpenReservedForTests();
                    Assert.True(ui.OpenReservedForTests);

                    ui.Session.NotifyClosedForTests();

                    Assert.False(ui.OpenReservedForTests);
                    Assert.True(FluentConfigWindowManager.TryBeginOpen(scope, "1.0", null));
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        private static void ClearWindows()
        {
            var windows = FluentConfigWindowManager.TakeAllWindows();
            foreach (var w in windows)
            {
                try { w?.Close(); } catch { /* ignore */ }
            }
        }

        private static void RunSta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { error = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (error != null)
                throw error;
        }
    }
}
