using System;
using Sbui.Elements;

namespace Sbui
{
    public class CompetingTogglesBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string[] _options = Array.Empty<string>();
        private int _defaultIndex;
        private string _showWhenKey;

        internal CompetingTogglesBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public CompetingTogglesBuilder Hint(string text) { _hint = text; return this; }
        public CompetingTogglesBuilder Options(string[] options) { _options = options ?? Array.Empty<string>(); return this; }
        public CompetingTogglesBuilder DefaultIndex(int index) { _defaultIndex = index; return this; }
        public CompetingTogglesBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new CompetingToggleSwitchesElement(Label, _hint ?? "", _tabName, fullKey, _options, _defaultIndex);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
