using System;
using Sbui.Elements;

namespace Sbui
{
    public class DurationInputBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        internal string HintText { get; private set; }
        private bool _permanentOption = true;
        internal bool PermanentOption => _permanentOption;
        internal string DefaultValue { get; private set; } = "permanent";
        internal string ShowWhenKey { get; private set; }

        internal DurationInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public DurationInputBuilder Hint(string text) { HintText = text; return this; }
        public DurationInputBuilder Default(string value) { DefaultValue = value; return this; }
        public DurationInputBuilder WithPermanentOption(bool value) { _permanentOption = value; return this; }
        public DurationInputBuilder ShowWhen(string key) { ShowWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DurationInputElement(Label, HintText ?? "", _tabName, fullKey, _permanentOption, DefaultValue ?? "permanent");
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
