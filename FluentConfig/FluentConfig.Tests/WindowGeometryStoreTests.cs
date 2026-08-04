using FluentConfig.Core;
using FluentConfig.Native;
using Moq;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Xunit;

namespace FluentConfig.Tests
{
    public class WindowGeometryStoreTests
    {
        [Fact]
        public void KeyForTitle_IncludesTitle()
        {
            Assert.Equal("FluentConfig_Window_My Plugin", WindowGeometryStore.KeyForTitle("My Plugin"));
        }

        [Fact]
        public void Load_ReturnsNull_WhenGlobalMissing()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.GetGlobalVar<string>(It.IsAny<string>(), true)).Returns((string)null);

            Assert.Null(WindowGeometryStore.Load(cph.Object, "Test"));
        }

        [Fact]
        public void Load_ParsesValidJson()
        {
            var cph = new Mock<IInlineInvokeProxy>();
            var json = new JObject
            {
                ["left"] = 100,
                ["top"] = 50,
                ["width"] = 900,
                ["height"] = 700,
                ["state"] = "Normal",
            }.ToString();
            cph.Setup(x => x.GetGlobalVar<string>("FluentConfig_Window_Test", true)).Returns(json);

            var data = WindowGeometryStore.Load(cph.Object, "Test");
            Assert.NotNull(data);
            Assert.Equal(900, data.Width);
            Assert.Equal(700, data.Height);
        }

        [Fact]
        public void Save_WritesGlobalVar()
        {
            string saved = null;
            var cph = new Mock<IInlineInvokeProxy>();
            cph.Setup(x => x.SetGlobalVar("FluentConfig_Window_Test", It.IsAny<object>(), true))
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
            Assert.Equal(800, parsed.Value<double>("width"));
            Assert.Equal("Maximized", parsed.Value<string>("state"));
        }

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
