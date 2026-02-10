using System;
using Sbui.Elements;

namespace Sbui
{
    public class TextboxBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _defaultValue;
        private bool _isPassword;
        private string _showWhenKey;
        private bool _required;

        internal TextboxBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public TextboxBuilder Hint(string text) { _hint = text; return this; }
        public TextboxBuilder Default(string value) { _defaultValue = value; return this; }
        public TextboxBuilder Password() { _isPassword = true; return this; }
        public TextboxBuilder ShowWhen(string key) { _showWhenKey = key; return this; }
        public TextboxBuilder Required() { _required = true; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new TextboxElement(Label, _hint ?? "", _tabName, fullKey, _defaultValue ?? "", _isPassword);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
