using System;
using Sbui.Elements;

namespace Sbui
{
    public class SliderBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        internal string HintText { get; private set; }
        internal int Min { get; private set; }
        internal int Max { get; private set; }
        internal int DefaultValue { get; private set; }
        internal string ShowWhenKey { get; private set; }

        internal SliderBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public SliderBuilder Hint(string text) { HintText = text; return this; }
        public SliderBuilder Range(int min, int max) { Min = min; Max = max; return this; }
        public SliderBuilder Default(int value) { DefaultValue = value; return this; }
        public SliderBuilder ShowWhen(string key) { ShowWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new SliderElement(Label, HintText ?? "", _tabName, fullKey, Min, Max, DefaultValue);
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
