using System;
using Sbui.Elements;

namespace Sbui
{
    public class DurationInputBuilder : ControlBuilderBase
    {
        private bool _permanentOption = true;
        private string _defaultValue = "permanent";

        internal DurationInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(string value) { _defaultValue = value; }
        public override void WithPermanentOption(bool value) { _permanentOption = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new DurationInputElement(Label, HintText ?? "", TabName, fullKey, _permanentOption, _defaultValue ?? "permanent");
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
