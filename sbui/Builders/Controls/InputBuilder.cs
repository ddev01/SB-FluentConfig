using System;
using System.Windows.Controls;
using Sbui.Elements;
using Sbui.Helpers;

namespace Sbui
{
    public class InputBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _type = "string";
        private string _defaultString = "";
        private int _defaultInt;
        private double _defaultDouble;
        private float _defaultFloat;
        private int _minInt = 0;
        private int _maxInt = int.MaxValue;
        private double _minDouble = 0;
        private double _maxDouble = 100;
        private float _minFloat = 0;
        private float _maxFloat = 100;
        private double _step = 1;
        private bool _withStepper;
        private string _showWhenKey;

        internal InputBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { _hint = text; }
        public override void Type(string value) { _type = value ?? "string"; }
        public override void Default(string value) { _defaultString = value ?? ""; }
        public override void Default(int value) { _defaultInt = value; }
        public override void Default(double value) { _defaultDouble = value; _defaultFloat = (float)value; }
        public override void Range(int min, int max) { _minInt = min; _maxInt = max; }
        public override void Range(double min, double max) { _minDouble = min; _maxDouble = max; _minFloat = (float)min; _maxFloat = (float)max; }
        public override void Step(double value) { _step = value; }
        public override void WithStepper(bool value = true) { _withStepper = value; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var inputType = InputValidation.ParseInputType(_type);
            var el = new InputElement(
                Label, _hint ?? "", _tabName, fullKey,
                inputType,
                _defaultString, _defaultInt, _defaultDouble, _defaultFloat,
                _minInt, _maxInt, _minDouble, _maxDouble, _minFloat, _maxFloat,
                _step, _withStepper);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
