using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DynamicTextboxesBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string[] _presetValues = Array.Empty<string>();
        private string _showWhenKey;

        internal DynamicTextboxesBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { _hint = text; }
        public override void Preset(string[] values) { _presetValues = values ?? Array.Empty<string>(); }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DynamicTextboxesElement(Label, _hint ?? "", _tabName, fullKey, _presetValues);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
