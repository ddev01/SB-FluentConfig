using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class CompetingTogglesBuilder : ControlOptionsBase, IFlushableControlBuilder
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

        public override void Hint(string text) { _hint = text; }
        public override void Options(string[] options) { _options = options ?? Array.Empty<string>(); }
        public override void DefaultIndex(int index) { _defaultIndex = index; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new CompetingToggleSwitchesElement(Label, _hint ?? "", _tabName, fullKey, _options, _defaultIndex);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
