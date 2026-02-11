using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class TextboxBuilder : ControlOptionsBase, IFlushableControlBuilder
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

        public TextboxBuilder Required() { _required = true; return this; }

        public override void Hint(string text) { _hint = text; }
        public override void Default(string value) { _defaultValue = value; }
        public override void Password() { _isPassword = true; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new TextboxElement(Label, _hint ?? "", _tabName, fullKey, _defaultValue ?? "", _isPassword);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
