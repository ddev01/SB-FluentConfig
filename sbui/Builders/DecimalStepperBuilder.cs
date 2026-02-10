using System;
using Sbui.Elements;

namespace Sbui
{
    public class DecimalStepperBuilder : IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private double _min;
        private double _max = 100;
        private double _step = 1;
        private double _defaultValue;
        private string _showWhenKey;

        internal DecimalStepperBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public DecimalStepperBuilder Hint(string text) { _hint = text; return this; }
        public DecimalStepperBuilder Range(double min, double max) { _min = min; _max = max; return this; }
        public DecimalStepperBuilder Step(double value) { _step = value; return this; }
        public DecimalStepperBuilder Default(double value) { _defaultValue = value; return this; }
        public DecimalStepperBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DecimalStepperElement(Label, _hint ?? "", _tabName, fullKey, _min, _max, _step, _defaultValue);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
