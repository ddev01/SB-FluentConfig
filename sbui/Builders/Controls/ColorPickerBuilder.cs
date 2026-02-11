using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class ColorPickerBuilder : ControlOptionsBase, IFlushableControlBuilder
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

        public override void Hint(string text) { _hint = text; }
        public override void Default(string value) { _defaultColor = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new ColorPickerElement(Label, _hint ?? "", _tabName, fullKey, _defaultColor ?? "#000000");
            _strategy.Add(el, _showWhenKey);
        }
    }
}
