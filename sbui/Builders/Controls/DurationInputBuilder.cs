using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DurationInputBuilder : ControlOptionsBase, IFlushableControlBuilder
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

        public override void Hint(string text) { HintText = text; }
        public override void Default(string value) { DefaultValue = value; }
        public override void WithPermanentOption(bool value) { _permanentOption = value; }
        public override void ShowWhen(string key) { ShowWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DurationInputElement(Label, HintText ?? "", _tabName, fullKey, _permanentOption, DefaultValue ?? "permanent");
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
