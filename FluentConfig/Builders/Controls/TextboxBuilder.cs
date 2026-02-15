using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class TextboxBuilder : ControlBuilderBase
    {
        private string _defaultValue;
        private bool _isPassword;
        private bool _isMultiline;

        internal TextboxBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Multiline() { _isMultiline = true; }
        public override void Default(string value) { _defaultValue = value; }
        public override void Password() { _isPassword = true; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new TextboxElement(Label, HintText ?? "", TabName, fullKey, _defaultValue ?? "", _isPassword, _isMultiline);
            Strategy.Add(el, ShowWhenCondition);
        }
    }
}
