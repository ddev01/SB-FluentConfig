using System;
using Sbui.Elements;

namespace Sbui
{
    public class ResponseBoxBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _defaultText;
        private string _showWhenKey;

        internal ResponseBoxBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public ResponseBoxBuilder Hint(string text) { _hint = text; return this; }
        public ResponseBoxBuilder Default(string text) { _defaultText = text; return this; }
        public ResponseBoxBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new ResponseBoxElement(Label, _hint ?? "", _tabName, fullKey, _defaultText ?? "");
            _strategy.Add(el, _showWhenKey);
        }
    }
}
