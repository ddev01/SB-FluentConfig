using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class IntegerInputBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        internal string HintText { get; private set; }
        internal int DefaultValue { get; private set; }
        internal int Min { get; private set; }
        internal int Max { get; private set; }
        internal string ShowWhenKey { get; private set; }

        internal IntegerInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
            Max = int.MaxValue;
        }

        public override void Hint(string text) { HintText = text; }
        public override void Default(int value) { DefaultValue = value; }
        public override void Range(int min, int max) { Min = min; Max = max; }
        public override void ShowWhen(string key) { ShowWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new IntegerInputElement(Label, HintText ?? "", _tabName, fullKey, DefaultValue, Min, Max);
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
