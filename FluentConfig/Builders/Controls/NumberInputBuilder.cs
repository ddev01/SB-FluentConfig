using System;
using FluentConfig.Elements;
using FluentConfig.Helpers;

namespace FluentConfig
{
    public class NumberInputBuilder : ControlBuilderBase
    {
        private double _min;
        private double _max = 100;
        private double _step = 1;
        private double _defaultValue;
        private bool _withStepper;

        internal NumberInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(double value) { _defaultValue = value; }
        public override void Range(double min, double max) { _min = Math.Min(min, max); _max = Math.Max(min, max); }
        public override void Step(double value) { _step = value; }
        public override void WithStepper(bool value) { _withStepper = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new InputElement(Label, HintText ?? "", TabName, fullKey,
                InputValidation.InputType.Double,
                "", 0, _defaultValue, (float)_defaultValue,
                0, int.MaxValue, _min, _max, (float)_min, (float)_max,
                _step, _withStepper);
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
