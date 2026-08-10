using System;
using FluentConfig.Core;
using FluentConfig.Native;
using Moq;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Xunit;

namespace FluentConfig.Tests
{
    public class GeneralSettingsStoreTests
    {
        [Fact]
        public void GlobalKey_IsFluentconfigSettings()
        {
            Assert.Equal("fluentconfig_settings", GeneralSettingsStore.GlobalKey);
        }

        [Fact]
        public void WindowGeometry_Load_ReturnsNull_WhenGlobalMissing()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.GetGlobalVar<string>(It.IsAny<string>(), true)).Returns((string)null);

            Assert.Null(WindowGeometryStore.Load(cph.Object, "Test"));
        }

        [Fact]
        public void WindowGeometry_Load_ParsesNestedWindowEntry()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            var json = new JObject
            {
                ["windows"] = new JObject
                {
                    ["Test"] = new JObject
                    {
                        ["left"] = 100,
                        ["top"] = 50,
                        ["width"] = 900,
                        ["height"] = 700,
                        ["state"] = "Normal",
                    },
                },
            }.ToString();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns(json);

            var data = WindowGeometryStore.Load(cph.Object, "Test");
            Assert.NotNull(data);
            Assert.Equal(900, data.Width);
            Assert.Equal(700, data.Height);
        }

        [Fact]
        public void WindowGeometry_Save_WritesNestedWindowEntry()
        {
            string saved = null;
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns((string)null);
            cph.Setup(x => x.SetGlobalVar(GeneralSettingsStore.GlobalKey, It.IsAny<object>(), true))
                .Callback<string, object, bool>((_, v, __) => saved = v?.ToString());

            WindowGeometryStore.Save(cph.Object, "Test", new WindowGeometryData
            {
                Left = 10,
                Top = 20,
                Width = 800,
                Height = 600,
                State = "Maximized",
            });

            Assert.NotNull(saved);
            var parsed = JObject.Parse(saved);
            var window = parsed["windows"]?["Test"] as JObject;
            Assert.NotNull(window);
            Assert.Equal(800, window.Value<double>("width"));
            Assert.Equal("Maximized", window.Value<string>("state"));
        }

        [Fact]
        public void WindowGeometry_Load_FallsBackToLegacyWindowGlobal()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns((string)null);
            var legacy = new JObject
            {
                ["left"] = 10,
                ["top"] = 20,
                ["width"] = 640,
                ["height"] = 480,
                ["state"] = "Normal",
            }.ToString();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.LegacyWindowKey("Test"), true)).Returns(legacy);

            var data = WindowGeometryStore.Load(cph.Object, "Test");
            Assert.NotNull(data);
            Assert.Equal(640, data.Width);
        }

        [Fact]
        public void WindowPrefs_Save_PreservesGeometryOnSameEntry()
        {
            string saved = null;
            var cph = new Mock<IInlineInvokeProxy>();
            var existing = new JObject
            {
                ["windows"] = new JObject
                {
                    ["Test"] = new JObject
                    {
                        ["left"] = 1,
                        ["top"] = 2,
                        ["width"] = 800,
                        ["height"] = 600,
                        ["state"] = "Normal",
                    },
                },
            }.ToString();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns(existing);
            cph.Setup(x => x.SetGlobalVar(GeneralSettingsStore.GlobalKey, It.IsAny<object>(), true))
                .Callback<string, object, bool>((_, v, __) => saved = v?.ToString());

            WindowPrefsStore.SetDontRemindDiscard(cph.Object, "Test", true);

            var parsed = JObject.Parse(saved);
            var window = parsed["windows"]?["Test"] as JObject;
            Assert.NotNull(window);
            Assert.True(window.Value<bool>("dontRemindDiscard"));
            Assert.Equal(800, window.Value<double>("width"));
        }

        [Fact]
        public void DllCheckLastUtc_ReadsFromGeneralSettings()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            var json = new JObject
            {
                ["dll_check_last_utc"] = "2026-01-01T00:00:00.0000000Z",
            }.ToString();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns(json);

            var loaded = GeneralSettingsStore.LoadDllCheckLastUtc(cph.Object);
            Assert.NotNull(loaded);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), loaded.Value);
        }

        [Fact]
        public void DllCheckLastUtc_FallsBackToLegacyGlobal()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.GlobalKey, true)).Returns((string)null);
            cph.Setup(x => x.GetGlobalVar<string>(GeneralSettingsStore.LegacyDllCheckLastUtcKey, true))
                .Returns("2026-02-01T00:00:00.0000000Z");

            var loaded = GeneralSettingsStore.LoadDllCheckLastUtc(cph.Object);
            Assert.NotNull(loaded);
            Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), loaded.Value);
        }
    }

    public class WindowGeometryStoreTests
    {
        [Fact]
        public void ClampToVirtualScreen_KeepsWindowOnScreen()
        {
            var data = new WindowGeometryData
            {
                Left = -5000,
                Top = -5000,
                Width = 900,
                Height = 700,
                State = "Normal",
            };

            var clamped = WindowGeometryStore.ClampToVirtualScreen(data);
            Assert.NotNull(clamped);
            Assert.True(clamped.Left >= System.Windows.SystemParameters.VirtualScreenLeft);
            Assert.True(clamped.Top >= System.Windows.SystemParameters.VirtualScreenTop);
            Assert.True(clamped.Width >= 480);
            Assert.True(clamped.Height >= 360);
        }
    }

    public class DwmTitleBarTests
    {
        [Theory]
        [InlineData("dark", true)]
        [InlineData("light", false)]
        [InlineData(null, true)]
        [InlineData("", true)]
        public void ResolveDark_HandlesKnownSchemes(string scheme, bool expectedDark)
        {
            Assert.Equal(expectedDark, DwmTitleBar.ResolveDark(scheme));
        }
    }
}
