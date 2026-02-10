using System;
using Sbui.Elements;

namespace Sbui
{
    public class ToggleBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private bool _defaultValue;
        private string _showWhenKey;

        internal ToggleBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public ToggleBuilder Hint(string text) { _hint = text; return this; }
        public ToggleBuilder Default(bool value) { _defaultValue = value; return this; }
        public ToggleBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new ToggleSwitchElement(Label, _hint ?? "", _tabName, fullKey, _defaultValue);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
