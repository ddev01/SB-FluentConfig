using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class ToggleBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private bool _defaultValue;
        private string _showWhenKey;
        private string[] _competingOptions;
        private int _maxSelected = 1;
        private int[] _defaultIndices;

        internal ToggleBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { _hint = text; }
        public override void Default(bool value) { _defaultValue = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }
        public override void WithExclusive(string[] options) { _competingOptions = options ?? Array.Empty<string>(); }
        public override void MaxSelected(int n) { _maxSelected = Math.Max(1, n); }
        public override void DefaultIndices(int[] indices) { _defaultIndices = indices; }
        public override void DefaultIndex(int index) { _defaultIndices = new[] { index }; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            if (_competingOptions != null && _competingOptions.Length > 0)
            {
                var defaultIdx = _defaultIndices != null && _defaultIndices.Length > 0 ? _defaultIndices[0] : 0;
                var el = new CompetingToggleSwitchesElement(Label, _hint ?? "", _tabName, fullKey, _competingOptions, defaultIdx, _maxSelected, _defaultIndices);
                _strategy.Add(el, _showWhenKey);
            }
            else
            {
                var el = new ToggleSwitchElement(Label, _hint ?? "", _tabName, fullKey, _defaultValue);
                _strategy.Add(el, _showWhenKey);
            }
        }
    }
}
