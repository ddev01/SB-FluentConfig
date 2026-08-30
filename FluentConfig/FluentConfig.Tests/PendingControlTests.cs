using System;
using FluentConfig.Protocol;
using Xunit;

namespace FluentConfig.Tests
{
    public class PendingControlTests
    {
        private static (PendingControl pending, SchemaNodeList list) CreatePending()
        {
            var list = new SchemaNodeList();
            // Session is only needed for button/dropdown/pill registration — null-safe paths for simple nodes.
            var pending = new PendingControl(session: null, list);
            return (pending, list);
        }

        [Fact]
        public void BeginToggle_NullSaveKey_Throws()
        {
            var (pending, _) = CreatePending();
            Assert.Throws<ArgumentException>(() => pending.BeginToggle("Label", null));
            Assert.Throws<ArgumentException>(() => pending.BeginToggle("Label", "  "));
        }

        [Fact]
        public void RangeDouble_OnSlider_WiresMinMax()
        {
            var (pending, list) = CreatePending();
            pending.BeginSlider("Volume", "volume");
            pending.Range(1.4, 9.6);
            pending.Flush();
            var node = Assert.IsType<SliderNode>(Assert.Single(list.ToList()));
            Assert.Equal(1, node.Min);
            Assert.Equal(10, node.Max);
        }

        [Fact]
        public void Password_OnToggle_Throws()
        {
            var (pending, _) = CreatePending();
            pending.BeginToggle("On", "on");
            Assert.Throws<InvalidOperationException>(() => pending.Password());
        }

        [Fact]
        public void Exclusive_WithDefaultBool_ThrowsOnFlush()
        {
            var (pending, _) = CreatePending();
            pending.BeginToggle("Mode", "mode");
            pending.WithExclusive(new[] { "A", "B" });
            pending.Default(true);
            Assert.Throws<InvalidOperationException>(() => pending.Flush());
        }

        [Fact]
        public void DuplicateSaveKey_Throws()
        {
            var (pending, list) = CreatePending();
            pending.BeginToggle("A", "same");
            pending.Flush();
            var pending2 = new PendingControl(null, list);
            pending2.BeginTextbox("B", "same");
            Assert.Throws<InvalidOperationException>(() => pending2.Flush());
        }
    }
}
