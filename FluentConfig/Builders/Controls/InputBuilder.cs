using System;
using FluentConfig.Elements;
using FluentConfig.Helpers;

namespace FluentConfig
{
    public class InputBuilder : ControlBuilderBase
    {
        private string _type = "string";
        private string _defaultString = "";
        private int _defaultInt;
        private double _defaultDouble;
        private float _defaultFloat;
        private int _minInt;
        private int _maxInt = int.MaxValue;
        private double _minDouble;
        private double _maxDouble = 100;
        private float _minFloat;
        private float _maxFloat = 100;
        private double _step = 1;
        private bool _withStepper;
        private string _width;

        internal InputBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Type(string value) { _type = value ?? "string"; }
        public override void Width(string value) { _width = value; }
        public override void Width(int value) { _width = value.ToString(); }
        public override void Default(string value) { _defaultString = value ?? ""; }
        public override void Default(int value) { _defaultInt = value; }
        public override void Default(double value) { _defaultDouble = value; _defaultFloat = (float)value; }
        public override void Range(int min, int max) { _minInt = Math.Min(min, max); _maxInt = Math.Max(min, max); }
        public override void Range(double min, double max) { _minDouble = Math.Min(min, max); _maxDouble = Math.Max(min, max); _minFloat = (float)Math.Min(min, max); _maxFloat = (float)Math.Max(min, max); }
        public override void Step(double value) { _step = value; }
        public override void WithStepper(bool value = true) { _withStepper = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var inputType = InputValidation.ParseInputType(_type);
            var el = new InputElement(new InputElementOptions
            {
                Title = Label,
                Description = HintText ?? "",
                TabName = TabName,
                SaveKey = fullKey,
                InputType = inputType,
                DefaultString = _defaultString ?? "",
                DefaultInt = _defaultInt,
                DefaultDouble = _defaultDouble,
                DefaultFloat = _defaultFloat,
                MinInt = _minInt,
                MaxInt = _maxInt,
                MinDouble = _minDouble,
                MaxDouble = _maxDouble,
                MinFloat = _minFloat,
                MaxFloat = _maxFloat,
                Step = _step,
                WithStepper = _withStepper,
                Width = _width
            });
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
