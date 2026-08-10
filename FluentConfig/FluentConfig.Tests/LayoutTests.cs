using System;
using System.Linq;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class LayoutTests
    {
        [Fact]
        public void ParseGridSpec_HappyPath_OrderIndependent()
        {
            var a = LayoutTokens.ParseGridSpec("grid-cols-2 gap-3");
            Assert.Equal("grid", a.Mode);
            Assert.Equal(2, a.Columns);
            Assert.Equal(3, a.Gap);

            var b = LayoutTokens.ParseGridSpec("gap-4 grid-cols-3");
            Assert.Equal(3, b.Columns);
            Assert.Equal(4, b.Gap);
        }

        [Fact]
        public void ParseGridSpec_DefaultGap_WhenOmitted()
        {
            var spec = LayoutTokens.ParseGridSpec("grid-cols-2");
            Assert.Equal(2, spec.Columns);
            Assert.Equal(3, spec.Gap);
        }

        [Fact]
        public void ParseGridSpec_ItemsCenter_SetsAlign()
        {
            var spec = LayoutTokens.ParseGridSpec("grid-cols-2 gap-3 items-center");
            Assert.Equal("center", spec.Align);
        }

        [Fact]
        public void ParseGridSpec_InvalidToken_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => LayoutTokens.ParseGridSpec("grid-cols-2 foo"));
            Assert.Contains("foo", ex.Message);
        }

        [Fact]
        public void ParseRowSpec_Empty_Defaults()
        {
            var spec = LayoutTokens.ParseRowSpec("");
            Assert.Equal("row", spec.Mode);
            Assert.Equal(3, spec.Gap);
            Assert.Null(spec.Align);
        }

        [Fact]
        public void ParseRowSpec_GapAndItems()
        {
            var spec = LayoutTokens.ParseRowSpec("gap-2 items-center");
            Assert.Equal("row", spec.Mode);
            Assert.Equal(2, spec.Gap);
            Assert.Equal("center", spec.Align);
        }

        [Fact]
        public void ParseRowSpec_GridCols_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => LayoutTokens.ParseRowSpec("grid-cols-2"));
            Assert.Contains("grid-cols-2", ex.Message);
        }

        [Theory]
        [InlineData("w-fit", "fit-content")]
        [InlineData("w-full", "100%")]
        [InlineData("w-1/2", "50%")]
        [InlineData("w-1/3", "33.3333%")]
        [InlineData("w-2/3", "66.6667%")]
        [InlineData("w-1/4", "25%")]
        [InlineData("w-3/4", "75%")]
        [InlineData("w-[120px]", "120px")]
        [InlineData("w-[5rem]", "5rem")]
        public void ParseSize_WidthTokens_ResolveToCss(string token, string css)
        {
            var hint = LayoutTokens.ParseSize(token);
            Assert.Equal(css, hint.Width);
        }

        [Fact]
        public void ParseSize_MinMaxScaleAndLiterals()
        {
            var hint = LayoutTokens.ParseSize("w-fit min-w-20 max-w-96");
            Assert.Equal("fit-content", hint.Width);
            Assert.Equal("80px", hint.MinWidth);
            Assert.Equal("384px", hint.MaxWidth);

            var lit = LayoutTokens.ParseSize("min-w-[80px] max-w-[5rem]");
            Assert.Equal("80px", lit.MinWidth);
            Assert.Equal("5rem", lit.MaxWidth);
        }

        [Fact]
        public void ParseSize_ColSpan()
        {
            var hint = LayoutTokens.ParseSize("col-span-2 w-full");
            Assert.Equal(2, hint.Span);
            Assert.Equal("100%", hint.Width);
        }

        [Fact]
        public void ParseSize_GrowShrink()
        {
            var hint = LayoutTokens.ParseSize("grow shrink-0");
            Assert.True(hint.Grow);
            Assert.False(hint.Shrink);

            var zero = LayoutTokens.ParseSize("grow-0 shrink");
            Assert.False(zero.Grow);
            Assert.True(zero.Shrink);
        }

        [Fact]
        public void ParseSize_BareFraction_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => LayoutTokens.ParseSize("1/2"));
            Assert.Contains("1/2", ex.Message);
        }

        [Fact]
        public void ParseSize_InvalidToken_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => LayoutTokens.ParseSize("auto"));
            Assert.Contains("auto", ex.Message);
        }

        [Fact]
        public void Grid_EmitsGroupWithGridSpec_AndResolvedSize()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout Grid", "1.0");
            ui.Section("G", "g", s => s
                .Grid("grid-cols-2 gap-3 items-center", g => g
                    .Toggle("A", "a")
                    .Toggle("B", "b").Size("col-span-2")
                    .Textbox("Name", "name").Size("w-1/2")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var group = Assert.IsType<GroupNode>(Assert.Single(doc.Sections[0].Children));
            Assert.NotNull(group.Grid);
            Assert.Equal("grid", group.Grid.Mode);
            Assert.Equal(2, group.Grid.Columns);
            Assert.Equal(3, group.Grid.Gap);
            Assert.Equal("center", group.Grid.Align);
            Assert.Equal(3, group.Children.Count);

            var spanNode = Assert.IsType<ToggleNode>(group.Children[1]);
            Assert.Equal(2, spanNode.Layout?.Span);

            var widthNode = Assert.IsType<TextboxNode>(group.Children[2]);
            Assert.Equal("50%", widthNode.Layout?.Width);

            var json = ProtocolJson.Serialize(group);
            var jo = JObject.Parse(json);
            Assert.Equal(2, jo["grid"]?["columns"]?.Value<int>());
            Assert.Equal("center", jo["grid"]?["align"]?.ToString());
            Assert.Equal(2, jo["children"]?[1]?["layout"]?["span"]?.Value<int>());
            Assert.Equal("50%", jo["children"]?[2]?["layout"]?["width"]?.ToString());
        }

        [Fact]
        public void Row_EmitsFlexModeGroup()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout Row", "1.0");
            ui.Section("G", "g", s => s
                .Row("gap-2 items-center", r => r.Toggle("A", "a").Toggle("B", "b"))
            );

            var group = Assert.IsType<GroupNode>(
                Assert.Single(ui.Session.BuildDocumentForTests().Sections[0].Children));
            Assert.Equal("row", group.Grid.Mode);
            Assert.Equal(2, group.Grid.Gap);
            Assert.Equal("center", group.Grid.Align);
            Assert.Null(group.Grid.Columns);
        }

        [Fact]
        public void Size_GrowInsideRow_Succeeds()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout Grow", "1.0");
            ui.Section("G", "g", s => s
                .Row(r => r
                    .Textbox("Name", "name").Size("grow")
                    .Button("Go").Size("shrink-0")
                )
            );

            var group = Assert.IsType<GroupNode>(
                Assert.Single(ui.Session.BuildDocumentForTests().Sections[0].Children));
            var text = Assert.IsType<TextboxNode>(group.Children[0]);
            Assert.True(text.Layout?.Grow);
            var btn = Assert.IsType<ButtonNode>(group.Children[1]);
            Assert.False(btn.Layout?.Shrink);
        }

        [Fact]
        public void Size_GrowOutsideRow_Throws()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout Grow Bad", "1.0");
            ui.Section("G", "g", s => s
                .Textbox("Name", "name").Size("grow")
            );

            var ex = Assert.Throws<InvalidOperationException>(() => ui.Session.BuildDocumentForTests());
            Assert.Contains("grow/shrink", ex.Message);
            Assert.Contains("Row", ex.Message);
        }

        [Fact]
        public void Size_GrowInsideGrid_Throws()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout Grow Grid", "1.0");
            ui.Section("G", "g", s => s
                .Grid("grid-cols-2", g => g
                    .Textbox("Name", "name").Size("grow")
                )
            );

            var ex = Assert.Throws<InvalidOperationException>(() => ui.Session.BuildDocumentForTests());
            Assert.Contains("grow/shrink", ex.Message);
        }

        [Fact]
        public void Size_ColSpanOutsideGrid_Throws()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Layout ColSpan Bad", "1.0");
            ui.Section("G", "g", s => s
                .Textbox("Name", "name").Size("col-span-2")
            );

            var ex = Assert.Throws<InvalidOperationException>(() => ui.Session.BuildDocumentForTests());
            Assert.Contains("col-span", ex.Message);
            Assert.Contains("Grid", ex.Message);
        }
    }
}
