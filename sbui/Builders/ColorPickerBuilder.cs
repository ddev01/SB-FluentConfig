using System;
using Sbui.Elements;

namespace Sbui
{
    public class ColorPickerBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _defaultColor = "#000000";
        private string _showWhenKey;

        internal ColorPickerBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public ColorPickerBuilder Hint(string text) { _hint = text; return this; }
        public ColorPickerBuilder Default(string hex) { _defaultColor = hex; return this; }
        public ColorPickerBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new ColorPickerElement(Label, _hint ?? "", _tabName, fullKey, _defaultColor ?? "#000000");
            _strategy.Add(el, _showWhenKey);
        }
    }
}
