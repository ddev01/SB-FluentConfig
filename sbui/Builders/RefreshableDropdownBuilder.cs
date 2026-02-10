using System;
using Sbui.Elements;

namespace Sbui
{
    public class RefreshableDropdownBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string[] _options = Array.Empty<string>();
        private Func<string[]> _refreshCallback;
        private int _defaultIndex;
        private string _showWhenKey;

        internal RefreshableDropdownBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public RefreshableDropdownBuilder Hint(string text) { _hint = text; return this; }
        public RefreshableDropdownBuilder Options(string[] options) { _options = options ?? Array.Empty<string>(); return this; }
        public RefreshableDropdownBuilder Refresh(Func<string[]> callback) { _refreshCallback = callback; return this; }
        public RefreshableDropdownBuilder DefaultIndex(int index) { _defaultIndex = index; return this; }
        public RefreshableDropdownBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new RefreshableDropdownElement(Label, _hint ?? "", _tabName, fullKey, _options, _refreshCallback, _defaultIndex);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
