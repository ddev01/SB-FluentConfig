using System;
using System.Linq;
using FluentConfig.Protocol;
using Xunit;

namespace FluentConfig.Tests
{
    public class BuilderGuardTests
    {
        private static (PendingControl pending, SchemaNodeList list) CreatePending()
        {
            var list = new SchemaNodeList();
            return (new PendingControl(session: null, list), list);
        }

        [Fact]
        public void Hint_WithoutActiveControl_Throws()
        {
            var (pending, _) = CreatePending();
            pending.BeginToggle("A", "a");
            pending.Flush();
            var ex = Assert.Throws<InvalidOperationException>(() => pending.Hint("lost"));
            Assert.Contains("Hint", ex.Message);
        }

        [Fact]
        public void Size_WithoutActiveControl_Throws()
        {
            var (pending, _) = CreatePending();
            Assert.Throws<InvalidOperationException>(() => pending.Size("w-fit"));
        }

        [Fact]
        public void ShowWhen_WithoutActiveControl_Throws()
        {
            var (pending, _) = CreatePending();
            Assert.Throws<InvalidOperationException>(() => pending.ShowWhen("k"));
        }

        [Fact]
        public void Size_MergesPerAxis_LastTokenWinsWithinAxis()
        {
            var list = new SchemaNodeList { ContainerMode = "grid" };
            var pending = new PendingControl(null, list);
            pending.BeginTextbox("N", "n");
            pending.Size("w-fit");
            pending.Size("col-span-2");
            pending.Flush();
            var node = Assert.IsType<TextboxNode>(Assert.Single(list.ToList()));
            Assert.Equal("fit-content", node.Layout.Width);
            Assert.Equal(2, node.Layout.Span);
        }

        [Fact]
        public void Size_SingleString_Unchanged()
        {
            var list = new SchemaNodeList { ContainerMode = "grid" };
            var pending = new PendingControl(null, list);
            pending.BeginTextbox("N", "n");
            pending.Size("w-fit col-span-2");
            pending.Flush();
            var node = Assert.IsType<TextboxNode>(Assert.Single(list.ToList()));
            Assert.Equal("fit-content", node.Layout.Width);
            Assert.Equal(2, node.Layout.Span);
        }

        [Fact]
        public void Toggle_MaxSelectedWithoutExclusive_Throws()
        {
            var (pending, _) = CreatePending();
            pending.BeginToggle("T", "t");
            pending.MaxSelected(2);
            var ex = Assert.Throws<InvalidOperationException>(() => pending.Flush());
            Assert.Contains("WithExclusive", ex.Message);
        }

        [Fact]
        public void Toggle_DefaultIndexWithoutExclusive_Throws()
        {
            var (pending, _) = CreatePending();
            pending.BeginToggle("T", "t");
            pending.DefaultIndex(0);
            Assert.Throws<InvalidOperationException>(() => pending.Flush());
        }

        [Fact]
        public void RefreshOnlyDropdown_DefaultIndex_Throws()
        {
            var (pending, _) = CreatePending();
            pending.BeginDropdown("R", "r");
            pending.Refresh(() => new[] { "a", "b" });
            pending.DefaultIndex(0);
            var ex = Assert.Throws<InvalidOperationException>(() => pending.Flush());
            Assert.Contains("DefaultByValue", ex.Message);
        }

        [Fact]
        public void Number_Range_LastCallWins_BothOrders()
        {
            var (pending, list) = CreatePending();
            pending.BeginNumberInput("N", "n");
            pending.Range(0, 10);
            pending.Range(1.5, 9.5);
            pending.Flush();
            var node = Assert.IsType<NumberInputNode>(Assert.Single(list.ToList()));
            Assert.Equal(1.5, node.Min);
            Assert.Equal(9.5, node.Max);

            var (pending2, list2) = CreatePending();
            pending2.BeginNumberInput("N", "n2");
            pending2.Range(1.5, 9.5);
            pending2.Range(0, 10);
            pending2.Flush();
            var node2 = Assert.IsType<NumberInputNode>(Assert.Single(list2.ToList()));
            Assert.Equal(0, node2.Min);
            Assert.Equal(10, node2.Max);
        }

        [Fact]
        public void Slider_Range_LastCallWins_BothOrders()
        {
            var (pending, list) = CreatePending();
            pending.BeginSlider("S", "s");
            pending.Range(1.4, 9.6);
            pending.Range(2, 8);
            pending.Flush();
            var node = Assert.IsType<SliderNode>(Assert.Single(list.ToList()));
            Assert.Equal(2, node.Min);
            Assert.Equal(8, node.Max);

            var (pending2, list2) = CreatePending();
            pending2.BeginSlider("S", "s2");
            pending2.Range(2, 8);
            pending2.Range(1.4, 9.6);
            pending2.Flush();
            var node2 = Assert.IsType<SliderNode>(Assert.Single(list2.ToList()));
            Assert.Equal(1, node2.Min);
            Assert.Equal(10, node2.Max);
        }

        [Fact]
        public void Number_Default_LastCallWins()
        {
            var (pending, list) = CreatePending();
            pending.BeginNumberInput("N", "n");
            pending.Default(3);
            pending.Default(4.5);
            pending.Flush();
            var node = Assert.IsType<NumberInputNode>(Assert.Single(list.ToList()));
            Assert.Equal(4.5, Convert.ToDouble(node.DefaultValue));

            var (pending2, list2) = CreatePending();
            pending2.BeginNumberInput("N", "n2");
            pending2.Default(4.5);
            pending2.Default(3);
            pending2.Flush();
            var node2 = Assert.IsType<NumberInputNode>(Assert.Single(list2.ToList()));
            Assert.Equal(3, Convert.ToInt32(node2.DefaultValue));
        }

        [Fact]
        public void AssertUniqueSaveKeys_CatchesValueSaveKeyCollision()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "dup_value", "Dup Value", "1.0");
            ui.Section("G", "g", s => s
                .Dropdown("A", "a_display").WithPairValue("shared_id").Options(new[] { ("1", "One") })
                .Dropdown("B", "b_display").WithPairValue("shared_id").Options(new[] { ("2", "Two") })
            );
            var ex = Assert.Throws<InvalidOperationException>(() => ui.Session.BuildDocumentForTests());
            Assert.Contains("shared_id", ex.Message);
        }

        [Fact]
        public void GeneratedIds_DedupeOnCollision()
        {
            var cph = TestCph.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "dup_ids", "Dup Ids", "1.0");
            ui.Section("G", "g", s => s
                .Grid("grid-cols-2", g => g.Toggle("A", "a"))
                .Grid("grid-cols-2", g => g.Toggle("B", "b"))
                .WithVisibility("premium", _ => { })
                .WithVisibilityWhenOff("premium", _ => { })
                .Button("Save")
                .Button("Save")
            );
            var doc = ui.Session.BuildDocumentForTests();
            var ids = doc.Sections[0].Children.Select(n =>
            {
                switch (n)
                {
                    case GroupNode g: return g.Id;
                    case ButtonNode b: return b.Id;
                    default: return null;
                }
            }).Where(id => id != null).ToList();
            Assert.Contains("grid_2", ids);
            Assert.Contains("grid_2_2", ids);
            Assert.Contains("vis_premium", ids);
            Assert.Contains("vis_premium_2", ids);
            Assert.Contains("btn_save", ids);
            Assert.Contains("btn_save_2", ids);
        }
    }
}
