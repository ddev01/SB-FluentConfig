using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class SliderWithToggleBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private int _min;
        private int _max = 100;
        private int _defaultValue;
        private bool _toggleDefault;
        private string _showWhenKey;

        internal SliderWithToggleBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { _hint = text; }
        public override void Default(int value) { _defaultValue = value; }
        public override void Range(int min, int max) { _min = min; _max = max; }
        public override void ToggleDefault(bool value) { _toggleDefault = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new SliderWithToggleSwitchElement(Label, _hint ?? "", _tabName, fullKey, _min, _max, _defaultValue, _toggleDefault);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
