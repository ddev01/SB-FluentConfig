using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DecimalStepperBuilder : ControlOptionsBase, IFlushableControlBuilder
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

        public override void Hint(string text) { _hint = text; }
        public override void Default(double value) { _defaultValue = value; }
        public override void Range(double min, double max) { _min = min; _max = max; }
        public override void Step(double value) { _step = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DecimalStepperElement(Label, _hint ?? "", _tabName, fullKey, _min, _max, _step, _defaultValue);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
