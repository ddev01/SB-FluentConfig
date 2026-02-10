using System;
using Sbui.Elements;

namespace Sbui
{
    public class DynamicTextboxesBuilder : IFlushableControlBuilder
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

        public DynamicTextboxesBuilder Hint(string text) { _hint = text; return this; }
        public DynamicTextboxesBuilder Preset(string[] values) { _presetValues = values ?? Array.Empty<string>(); return this; }
        public DynamicTextboxesBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DynamicTextboxesWithPresetElement(Label, _hint ?? "", _tabName, fullKey, _presetValues);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
