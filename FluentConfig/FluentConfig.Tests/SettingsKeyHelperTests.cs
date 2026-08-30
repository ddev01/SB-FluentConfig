using System;
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
        public void SettingsKeyFor_AppendsSettingsSuffix_OnValidId()
        {
            Assert.Equal("first_chatters_settings", SettingsKeyHelper.SettingsKeyFor("first_chatters"));
        }

        [Fact]
        public void KeyFor_CustomSuffix()
        {
            Assert.Equal("first_chatters_counter", SettingsKeyHelper.KeyFor("first_chatters", "counter"));
        }

        [Fact]
        public void KeyFor_EmptySuffix_ReturnsIdAlone()
        {
            Assert.Equal("first_chatters", SettingsKeyHelper.KeyFor("first_chatters", ""));
            Assert.Equal("first_chatters", SettingsKeyHelper.KeyFor("first_chatters", null));
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("Foo!")]
        [InlineData("Foo ")]
        [InlineData("1alerts")]
        [InlineData("")]
        [InlineData("alerts-v2")]
        public void ValidateMenuId_RejectsLegacyTitleShapes(string id)
        {
            Assert.Throws<ArgumentException>(() => SettingsKeyHelper.ValidateMenuId(id));
            Assert.False(SettingsKeyHelper.IsValidMenuId(id));
        }

        [Fact]
        public void ValidateMenuId_Null_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => SettingsKeyHelper.ValidateMenuId(null));
        }

        [Theory]
        [InlineData("a")]
        [InlineData("alerts")]
        [InlineData("first_chatters")]
        [InlineData("a1_b2")]
        public void ValidateMenuId_AcceptsCanonicalIds(string id)
        {
            Assert.Equal(id, SettingsKeyHelper.ValidateMenuId(id));
            Assert.True(SettingsKeyHelper.IsValidMenuId(id));
        }
    }
}
