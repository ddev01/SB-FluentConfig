using System;
using Sbui.Elements;

namespace Sbui
{
    public class SliderWithToggleBuilder : IFlushableControlBuilder
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

        public SliderWithToggleBuilder Hint(string text) { _hint = text; return this; }
        public SliderWithToggleBuilder Range(int min, int max) { _min = min; _max = max; return this; }
        public SliderWithToggleBuilder Default(int value) { _defaultValue = value; return this; }
        public SliderWithToggleBuilder ToggleDefault(bool value) { _toggleDefault = value; return this; }
        public SliderWithToggleBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new SliderWithToggleSwitchElement(Label, _hint ?? "", _tabName, fullKey, _min, _max, _defaultValue, _toggleDefault);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
