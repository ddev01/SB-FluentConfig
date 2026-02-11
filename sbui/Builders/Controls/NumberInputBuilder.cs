using System;
using System.Windows.Controls;
using Sbui.Elements;
using Sbui.Helpers;

namespace Sbui
{
    public class NumberInputBuilder : ControlOptionsBase, IFlushableControlBuilder
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
        private bool _withStepper = false;
        private string _showWhenKey;

        internal NumberInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { _hint = text; }
        public override void Default(double value) { _defaultValue = value; }
        public override void Range(double min, double max) { _min = Math.Min(min, max); _max = Math.Max(min, max); }
        public override void Step(double value) { _step = value; }
        public override void WithStepper(bool value) { _withStepper = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new InputElement(Label, _hint ?? "", _tabName, fullKey,
                Helpers.InputValidation.InputType.Double,
                "", 0, _defaultValue, (float)_defaultValue,
                0, int.MaxValue, _min, _max, (float)_min, (float)_max,
                _step, _withStepper, _showWhenKey);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
