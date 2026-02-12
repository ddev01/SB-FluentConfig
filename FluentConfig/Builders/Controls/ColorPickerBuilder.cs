using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class ColorPickerBuilder : ControlBuilderBase
    {
        private string _defaultColor = "#000000";

        internal ColorPickerBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(string value) { _defaultColor = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new ColorPickerElement(Label, HintText ?? "", TabName, fullKey, _defaultColor ?? "#000000");
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
