using System.Collections.Concurrent;
using FluentConfig.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class SettingsLoadHelpersTests
    {
        private class SampleSettings
        {
            public int MaxCount { get; set; } = 3;
            public bool ExcludeBroadcaster { get; set; } = true;
            public string Message { get; set; } = "default msg";
            public string PointsVariable { get; set; } = "points";
        }

        [Fact]
        public void LoadSettings_MapsSnakeCaseFields_AndPreservesDefaultsForMissing()
        {
            var store = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);
            var key = SettingsKeyHelper.SettingsKeyFor("First Chatters");
            store[key] = new JObject
            {
                ["max_count"] = 7,
                ["exclude_broadcaster"] = false
                // message / points_variable intentionally absent
            }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;
            var s = FluentConfig.LoadSettings<SampleSettings>(cph, "First Chatters");

            Assert.Equal(7, s.MaxCount);
            Assert.False(s.ExcludeBroadcaster);
            Assert.Equal("default msg", s.Message);
            Assert.Equal("points", s.PointsVariable);
        }

        [Fact]
        public void LoadSettings_MalformedField_KeepsDefaultForThatField()
        {
            var store = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);
            var key = SettingsKeyHelper.SettingsKeyFor("First Chatters");
            store[key] = new JObject
            {
                ["max_count"] = "not-a-number",
                ["message"] = "ok"
            }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;
            var s = FluentConfig.LoadSettings<SampleSettings>(cph, "First Chatters");

            Assert.Equal(3, s.MaxCount); // default preserved
            Assert.Equal("ok", s.Message);
        }

        [Fact]
        public void LoadSettings_MissingGlobal_ReturnsDefaults()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph().Object;
            var s = FluentConfig.LoadSettings<SampleSettings>(cph, "First Chatters");

            Assert.Equal(3, s.MaxCount);
            Assert.True(s.ExcludeBroadcaster);
            Assert.Equal("default msg", s.Message);
        }

        [Fact]
        public void LoadSettings_ValidateCallback_IsInvoked()
        {
            var store = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);
            store[SettingsKeyHelper.SettingsKeyFor("First Chatters")] =
                new JObject { ["max_count"] = 99 }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;
            var s = FluentConfig.LoadSettings<SampleSettings>(cph, "First Chatters", x =>
            {
                if (x.MaxCount > 10)
                    x.MaxCount = 10;
            });

            Assert.Equal(10, s.MaxCount);
        }

        [Fact]
        public void GetSetting_ReadsFlatNumberedKeys()
        {
            var store = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);
            store[SettingsKeyHelper.SettingsKeyFor("First Chatters")] = new JObject
            {
                ["points_1"] = 1000,
                ["points_2"] = 500
            }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;

            Assert.Equal(1000, FluentConfig.GetSetting(cph, "First Chatters", "points_1", 0));
            Assert.Equal(500, FluentConfig.GetSetting(cph, "First Chatters", "points_2", 0));
            Assert.Equal(0, FluentConfig.GetSetting(cph, "First Chatters", "points_3", 0));
        }

        [Fact]
        public void FacadeKeyHelpers_MatchCoreHelper()
        {
            Assert.Equal(
                SettingsKeyHelper.SettingsKeyFor("First Chatters"),
                Fc.SettingsKeyFor("First Chatters"));
            Assert.Equal(
                SettingsKeyHelper.Slugify("First Chatters"),
                Fc.SlugFor("First Chatters"));
            Assert.Equal(
                SettingsKeyHelper.KeyFor("First Chatters", "counter"),
                Fc.KeyFor("First Chatters", "counter"));
            Assert.Equal(
                FluentConfig.SettingsKeyFor("First Chatters"),
                Fc.SettingsKeyFor("First Chatters"));
        }
    }
}
