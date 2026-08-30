using System;
using System.Linq;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class ComparatorVisibilityTests
    {
        [Theory]
        [InlineData(Comparator.GreaterOrEqual, "gte", 4)]
        [InlineData(Comparator.LessOrEqual, "lte", 2)]
        [InlineData(Comparator.GreaterThan, "gt", 0)]
        [InlineData(Comparator.LessThan, "lt", 10)]
        public void ShowWhen_ComparatorLiteral_EmitsOperatorAndValue(Comparator op, string wireOp, int value)
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginIntegerInput("Amount", "amount");
            pending.ShowWhen("max_count", op, value);
            pending.Flush();

            var node = Assert.Single(list.ToList());
            var jo = JObject.Parse(ProtocolJson.Serialize(node));
            var vis = jo["visibility"] as JObject;
            Assert.NotNull(vis);
            Assert.Equal("max_count", vis["saveKey"]?.ToString());
            Assert.Equal(wireOp, vis["operator"]?.ToString());
            Assert.Equal(value, vis["value"]?.Value<int>());
            Assert.Null(vis["equals"]);
            Assert.Null(vis["compareKey"]);
        }

        [Fact]
        public void ShowWhen_ComparatorCompareKey_EmitsCompareKeyNotValue()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginIntegerInput("Amount", "amount");
            pending.ShowWhen("left_key", Comparator.GreaterOrEqual, "right_key");
            pending.Flush();

            var jo = JObject.Parse(ProtocolJson.Serialize(Assert.Single(list.ToList())));
            var vis = jo["visibility"] as JObject;
            Assert.Equal("gte", vis["operator"]?.ToString());
            Assert.Equal("right_key", vis["compareKey"]?.ToString());
            Assert.Null(vis["value"]);
            Assert.Null(vis["equals"]);
        }

        [Fact]
        public void ShowWhen_LegacyEquals_JsonShapeUnchanged()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginSlider("Level", "level");
            pending.ShowWhen("show_advanced");
            pending.Flush();

            var json = ProtocolJson.Serialize(Assert.Single(list.ToList()));
            var jo = JObject.Parse(json);
            var vis = jo["visibility"] as JObject;
            Assert.NotNull(vis);
            Assert.Equal("show_advanced", vis["saveKey"]?.ToString());
            Assert.Equal(true, vis["equals"]?.Value<bool>());
            Assert.Null(vis["operator"]);
            Assert.Null(vis["value"]);
            Assert.Null(vis["compareKey"]);
            // inverted:false is omitted by NullValueHandling — default false may or may not appear;
            // ensure no comparator fields leaked.
            Assert.DoesNotContain("\"operator\"", json);
            Assert.DoesNotContain("\"compareKey\"", json);
        }

        [Fact]
        public void WithVisibility_Comparator_EmitsGatedGroup()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "comparator_vis", "Comparator Vis", "1.0");
            ui.Section("G", "g", s => s
                .IntegerInput("Count", "count").Range(0, 10).Default(3)
                .WithVisibility("count", Comparator.GreaterOrEqual, 2, inner => inner
                    .Textbox("Extra", "extra")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var group = Assert.IsType<GroupNode>(
                doc.Sections[0].Children.Single(n => n is GroupNode));
            Assert.Equal("gte", group.Visibility.Operator);
            Assert.Equal(2, group.Visibility.Value);
            Assert.Null(group.Visibility.EqualsValue);
            Assert.Equal("extra", Assert.IsType<TextboxNode>(Assert.Single(group.Children)).SaveKey);
            Assert.Equal(false, group.Indented);
            var groupJson = JObject.Parse(ProtocolJson.Serialize(group));
            Assert.Equal(false, groupJson["indented"]?.Value<bool>());
        }

        [Fact]
        public void ShowWhen_StringEquals_EmitsEqualsWithoutOperator()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginNumberInput("Multiplier", "multiplier");
            pending.ShowWhen("default_mode", "random");
            pending.Flush();

            var jo = JObject.Parse(ProtocolJson.Serialize(Assert.Single(list.ToList())));
            var vis = jo["visibility"] as JObject;
            Assert.Equal("default_mode", vis["saveKey"]?.ToString());
            Assert.Equal("random", vis["equals"]?.ToString());
            Assert.Null(vis["operator"]);
            Assert.Null(vis["value"]);
        }

        [Fact]
        public void ShowWhenNot_EmitsInvertedEquals()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginTextbox("Note", "note");
            pending.ShowWhenNot("mode", "free");
            pending.Flush();

            var jo = JObject.Parse(ProtocolJson.Serialize(Assert.Single(list.ToList())));
            var vis = jo["visibility"] as JObject;
            Assert.Equal("free", vis["equals"]?.ToString());
            Assert.Equal(true, vis["inverted"]?.Value<bool>());
            Assert.Null(vis["operator"]);
        }

        [Fact]
        public void WithVisibility_StringEquals_EmitsFlatGroup()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "equals_vis", "Equals Vis", "1.0");
            ui.Section("G", "g", s => s
                .Dropdown("Mode", "mode").Options(new[] { "quiet", "loud" })
                .WithVisibility("mode", "quiet", inner => inner
                    .Textbox("Whisper", "whisper")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var group = Assert.IsType<GroupNode>(
                doc.Sections[0].Children.Single(n => n is GroupNode));
            Assert.Equal("quiet", group.Visibility.EqualsValue?.ToString());
            Assert.Null(group.Visibility.Operator);
            Assert.Equal(false, group.Indented);
            var json = ProtocolJson.Serialize(group);
            Assert.Contains("\"indented\":false", json.Replace(" ", ""));
        }

        [Fact]
        public void ShowWhen_WhenOff_EmitsInvertedBooleanTrue()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginTextbox("Note", "note");
            pending.ShowWhen(When.Off("premium"));
            pending.Flush();

            var jo = JObject.Parse(ProtocolJson.Serialize(Assert.Single(list.ToList())));
            var vis = jo["visibility"] as JObject;
            Assert.Equal("premium", vis["saveKey"]?.ToString());
            Assert.Equal(true, vis["equals"]?.Value<bool>());
            Assert.Equal(true, vis["inverted"]?.Value<bool>());
        }

        [Fact]
        public void ShowWhen_WhenAll_Throws()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginTextbox("Note", "note");
            var ex = Assert.Throws<InvalidOperationException>(() =>
                pending.ShowWhen(When.All(When.On("a"), When.On("b"))));
            Assert.Contains("When.All", ex.Message);
        }

        [Fact]
        public void WithVisibility_WhenAll_EmitsNestedGroups()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "when_all", "When All", "1.0");
            ui.Section("G", "g", s => s
                .Toggle("A", "flag_a")
                .Toggle("B", "flag_b")
                .WithVisibility(When.All(When.On("flag_a"), When.On("flag_b")), inner => inner
                    .Textbox("Both", "both_on")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var outer = Assert.IsType<GroupNode>(
                doc.Sections[0].Children.Single(n => n is GroupNode));
            Assert.Equal("flag_a", outer.Visibility.SaveKey);
            Assert.Equal(true, outer.Visibility.EqualsValue);
            Assert.False(outer.Visibility.Inverted);
            var inner = Assert.IsType<GroupNode>(Assert.Single(outer.Children));
            Assert.Equal("flag_b", inner.Visibility.SaveKey);
            Assert.Equal(true, inner.Visibility.EqualsValue);
            var leaf = Assert.IsType<TextboxNode>(Assert.Single(inner.Children));
            Assert.Equal("both_on", leaf.SaveKey);
        }

        [Fact]
        public void Toggle_ReturnsSectionBuilder_CanChainStructural()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "crtp_chain", "Crtp Chain", "1.0");
            ui.Section("G", "g", s => s
                .Toggle("A", "a").Hint("on")
                .Separator()
                .Textbox("B", "b").ShowWhen(When.Off("a"))
            );

            var doc = ui.Session.BuildDocumentForTests();
            Assert.Equal(3, doc.Sections[0].Children.Count);
            var text = Assert.IsType<TextboxNode>(doc.Sections[0].Children[2]);
            Assert.True(text.Visibility.Inverted);
            Assert.Equal(true, text.Visibility.EqualsValue);
        }
    }
}
