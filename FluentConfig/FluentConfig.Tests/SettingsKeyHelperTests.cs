using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    public class SettingsKeyHelperTests
    {
        [Theory]
        [InlineData("First Chatters", "first_chatters")]
        [InlineData("First  Chatters!", "first_chatters")]
        [InlineData("Tutorial 03 Dropdown And Buttons", "tutorial_03_dropdown_and_buttons")]
        [InlineData("Name Placeholder", "name_placeholder")]
        [InlineData("Defaults Seed", "defaults_seed")]
        [InlineData("MergeTest", "mergetest")]
        [InlineData("  spaced  ", "spaced")]
        [InlineData("!!!", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("MiXeD CaSe", "mixed_case")]
        [InlineData("a--b__c", "a_b_c")]
        public void Slugify_ProducesExpectedSlug(string title, string expected)
        {
            Assert.Equal(expected, SettingsKeyHelper.Slugify(title));
        }

        [Fact]
        public void SettingsKeyFor_AppendsSettingsSuffix()
        {
            Assert.Equal("first_chatters_settings", SettingsKeyHelper.SettingsKeyFor("First Chatters"));
        }

        [Fact]
        public void LegacySettingsKeyFor_UsesOldPrefixAndRawTitle()
        {
            Assert.Equal("FluentConfig_Settings_Alerts", SettingsKeyHelper.LegacySettingsKeyFor("Alerts"));
            Assert.Equal("FluentConfig_Settings_First Chatters", SettingsKeyHelper.LegacySettingsKeyFor("First Chatters"));
        }

        [Fact]
        public void KeyFor_CustomSuffix()
        {
            Assert.Equal("first_chatters_counter", SettingsKeyHelper.KeyFor("First Chatters", "counter"));
        }

        [Fact]
        public void KeyFor_EmptySlug_ReturnsSuffixAlone()
        {
            Assert.Equal("settings", SettingsKeyHelper.KeyFor("!!!", "settings"));
            Assert.Equal("settings", SettingsKeyHelper.SettingsKeyFor(""));
        }

        [Fact]
        public void KeyFor_EmptySuffix_ReturnsSlugAlone()
        {
            Assert.Equal("first_chatters", SettingsKeyHelper.KeyFor("First Chatters", ""));
            Assert.Equal("first_chatters", SettingsKeyHelper.KeyFor("First Chatters", null));
        }
    }
}