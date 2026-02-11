using System;
using Sbui.Elements;
using Sbui.Helpers;

namespace Sbui
{
    public class IntegerInputBuilder : ControlBuilderBase
    {
        private int _defaultValue;
        private int _min;
        private int _max = int.MaxValue;

        internal IntegerInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(int value) { _defaultValue = value; }
        public override void Range(int min, int max) { _min = Math.Min(min, max); _max = Math.Max(min, max); }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new InputElement(Label, HintText ?? "", TabName, fullKey,
                InputValidation.InputType.Int,
                "", _defaultValue, 0, 0,
                _min, _max, 0, 100, 0, 100,
                1, false);
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
