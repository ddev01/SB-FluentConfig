using System;
using FluentConfig.Core;
using Moq;
using Streamer.bot.Plugin.Interface;
using Xunit;

namespace FluentConfig.Tests
{
    public class UpdatePrefsStoreTests
    {
        [Fact]
        public void KeyForTitle_UsesUpdatePrefsPrefix()
        {
            Assert.Equal("FluentConfig_UpdatePrefs_My Ext", UpdatePrefsStore.KeyForTitle("My Ext"));
        }

        [Fact]
        public void IsWithinDailyThrottle_TrueWhenLastCheckRecent()
        {
            var prefs = new UpdatePrefsData
            {
                LastCheckUtc = DateTime.UtcNow.AddHours(-1),
            };
            Assert.True(UpdatePrefsStore.IsWithinDailyThrottle(prefs, DateTime.UtcNow));
        }

        [Fact]
        public void IsWithinDailyThrottle_FalseWhenOlderThan24h()
        {
            var prefs = new UpdatePrefsData
            {
                LastCheckUtc = DateTime.UtcNow.AddHours(-25),
            };
            Assert.False(UpdatePrefsStore.IsWithinDailyThrottle(prefs, DateTime.UtcNow));
        }

        [Fact]
        public void IsWithinDailyThrottle_FalseWhenNeverChecked()
        {
            Assert.False(UpdatePrefsStore.IsWithinDailyThrottle(null));
            Assert.False(UpdatePrefsStore.IsWithinDailyThrottle(new UpdatePrefsData()));
        }

        [Fact]
        public void IsVersionIgnored_MatchesStrippedSemver()
        {
            var prefs = new UpdatePrefsData { IgnoredVersion = "v1.2.3" };
            Assert.True(UpdatePrefsStore.IsVersionIgnored(prefs, "1.2.3"));
            Assert.False(UpdatePrefsStore.IsVersionIgnored(prefs, "1.2.4"));
        }

        [Fact]
        public void SaveAndLoad_RoundTripsLastCheckAndIgnoredVersion()
        {
            string stored = null;
            var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Strict);
            mock.Setup(c => c.GetGlobalVar<string>("FluentConfig_UpdatePrefs_Demo", true))
                .Returns(() => stored);
            mock.Setup(c => c.SetGlobalVar("FluentConfig_UpdatePrefs_Demo", It.IsAny<object>(), true))
                .Callback<string, object, bool>((_, value, __) => stored = value?.ToString());

            var now = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
            UpdatePrefsStore.Save(mock.Object, "Demo", new UpdatePrefsData
            {
                LastCheckUtc = now,
                IgnoredVersion = "1.4.0",
            });

            Assert.False(string.IsNullOrWhiteSpace(stored));
            Assert.Contains("2026-08-07", stored);
            Assert.Contains("\"ignoredVersion\":\"1.4.0\"", stored.Replace(" ", ""));

            var loaded = UpdatePrefsStore.Load(mock.Object, "Demo");
            Assert.NotNull(loaded);
            Assert.Equal("1.4.0", loaded.IgnoredVersion);
            Assert.NotNull(loaded.LastCheckUtc);
            Assert.Equal(now, loaded.LastCheckUtc.Value.ToUniversalTime());
        }

        [Fact]
        public void IgnoreVersion_PersistsWithoutClearingLastCheck()
        {
            string stored = null;
            var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Loose);
            mock.Setup(c => c.GetGlobalVar<string>(It.IsAny<string>(), true))
                .Returns(() => stored);
            mock.Setup(c => c.SetGlobalVar(It.IsAny<string>(), It.IsAny<object>(), true))
                .Callback<string, object, bool>((_, value, __) => stored = value?.ToString());

            UpdatePrefsStore.MarkChecked(mock.Object, "Demo", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            UpdatePrefsStore.IgnoreVersion(mock.Object, "Demo", "v2.0.0");

            var loaded = UpdatePrefsStore.Load(mock.Object, "Demo");
            Assert.Equal("2.0.0", loaded.IgnoredVersion);
            Assert.NotNull(loaded.LastCheckUtc);
        }
    }
}
