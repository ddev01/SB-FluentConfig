using System;
using Sbui.Elements;

namespace Sbui
{
    public class DynamicTextboxesBuilder : ControlBuilderBase
    {
        private string[] _presetValues = Array.Empty<string>();
        private bool _allowDuplicates = true;

        internal DynamicTextboxesBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Preset(string[] values) { _presetValues = values ?? Array.Empty<string>(); }
        public override void AllowDuplicates(bool value) { _allowDuplicates = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new DynamicTextboxesElement(Label, HintText ?? "", TabName, fullKey, _presetValues, _allowDuplicates);
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
