using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class ToggleBuilder : ControlBuilderBase
    {
        private bool _defaultValue;
        private string[] _competingOptions;
        private int _maxSelected = 1;
        private int[] _defaultIndices;

        internal ToggleBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(bool value) { _defaultValue = value; }
        public override void WithExclusive(string[] options) { _competingOptions = options ?? Array.Empty<string>(); }
        public override void MaxSelected(int n) { _maxSelected = Math.Max(1, n); }
        public override void DefaultIndices(int[] indices) { _defaultIndices = indices; }
        public override void DefaultIndex(int index) { _defaultIndices = new[] { index }; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            if (_competingOptions != null && _competingOptions.Length > 0)
            {
                var defaultIdx = _defaultIndices != null && _defaultIndices.Length > 0 ? _defaultIndices[0] : 0;
                var el = new CompetingToggleSwitchesElement(Label, HintText ?? "", TabName, fullKey, _competingOptions, defaultIdx, _maxSelected, _defaultIndices);
                Strategy.Add(el, ShowWhenKey);
            }
            else
            {
                var el = new ToggleSwitchElement(Label, HintText ?? "", TabName, fullKey, _defaultValue);
                Strategy.Add(el, ShowWhenKey);
            }
        }
    }
}
