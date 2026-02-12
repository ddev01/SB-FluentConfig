using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class FilepathBuilder : ControlBuilderBase
    {
        private string _defaultPath;

        internal FilepathBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Default(string value) { _defaultPath = value; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var el = new FilepathElement(Label, HintText ?? "", TabName, fullKey, _defaultPath ?? "");
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
