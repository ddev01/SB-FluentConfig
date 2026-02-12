using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class SliderBuilder : ControlBuilderBase
    {
        private int _min;
        private int _max;
        private int _defaultValue;

        internal SliderBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(int value) { _defaultValue = value; }
        public override void Range(int min, int max) { _min = Math.Min(min, max); _max = Math.Max(min, max); }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new SliderElement(Label, HintText ?? "", TabName, fullKey, _min, _max, _defaultValue);
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
