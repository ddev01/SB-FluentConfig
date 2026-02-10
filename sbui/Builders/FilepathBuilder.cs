using System;
using Sbui.Elements;

namespace Sbui
{
    public class FilepathBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _defaultPath;
        private string _showWhenKey;

        internal FilepathBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public FilepathBuilder Hint(string text) { _hint = text; return this; }
        public FilepathBuilder Default(string path) { _defaultPath = path; return this; }
        public FilepathBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new FilepathElement(Label, _hint ?? "", _tabName, fullKey, _defaultPath ?? "");
            _strategy.Add(el, _showWhenKey);
        }
    }
}
