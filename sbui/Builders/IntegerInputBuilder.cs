using System;
using Sbui.Elements;

namespace Sbui
{
    public class IntegerInputBuilder : IFlushableControlBuilder
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

        public IntegerInputBuilder Hint(string text) { HintText = text; return this; }
        public IntegerInputBuilder Default(int value) { DefaultValue = value; return this; }
        public IntegerInputBuilder Range(int min, int max) { Min = min; Max = max; return this; }
        public IntegerInputBuilder ShowWhen(string key) { ShowWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new IntegerInputElement(Label, HintText ?? "", _tabName, fullKey, DefaultValue, Min, Max);
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
