using System;
using System.Collections.Generic;
using System.Linq;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class DropdownBuilder : ControlBuilderBase
    {
        private string[] _options = Array.Empty<string>();
        private IEnumerable<(string Value, string Display)> _pairOptions;
        private string _valueKey;
        private Func<string[]> _refreshCallback;
        private Func<IRenderContext, string[]> _refreshWithContextCallback;
        private string[] _refreshStaticOptions;
        private Func<IEnumerable<(string Value, string Display)>> _refreshPairCallback;
        private int _defaultIndex;
        private string _defaultByValue;

        internal bool IsPairValue => _valueKey != null;
        internal bool HasRefresh => _refreshCallback != null || _refreshWithContextCallback != null || _refreshStaticOptions != null || _refreshPairCallback != null;

        internal DropdownBuilder(IAddStrategy strategy, string tabName, string label, string key)
            : base(strategy, tabName, label, key) { }

        public override void Options(string[] options) { _options = options ?? Array.Empty<string>(); _pairOptions = null; }
        public override void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions) { _pairOptions = pairOptions?.ToList(); _options = _pairOptions?.Select(p => p.Display).ToArray() ?? Array.Empty<string>(); }
        public override void WithPairValue(string valueKey) { _valueKey = valueKey; }
        public override void DefaultByValue(string value) { _defaultByValue = value; }
        public override void Refresh(Func<string[]> callback) { _refreshCallback = callback; _refreshWithContextCallback = null; _refreshStaticOptions = null; _refreshPairCallback = null; }
        public override void Refresh(Func<IRenderContext, string[]> callback) { _refreshWithContextCallback = callback; _refreshCallback = null; _refreshStaticOptions = null; _refreshPairCallback = null; }
        public override void Refresh(string[] options) { _refreshStaticOptions = options ?? Array.Empty<string>(); _refreshCallback = null; _refreshWithContextCallback = null; _refreshPairCallback = null; }
        public override void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback)
        {
            _refreshCallback = null;
            _refreshWithContextCallback = null;
            _refreshStaticOptions = null;
            if (callback != null)
                _refreshPairCallback = () => callback()?.ToList();
            else
                _refreshPairCallback = null;
        }
        public override void DefaultIndex(int index) { _defaultIndex = index; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var fullValueKey = IsPairValue ? Strategy.GetFullSaveKey(_valueKey) : null;
            var pairOpts = _pairOptions?.ToList();
            var el = new DropdownElement(
                Label,
                HintText ?? "",
                TabName,
                fullKey,
                fullValueKey,
                _options,
                pairOpts,
                _refreshCallback,
                _refreshWithContextCallback,
                _refreshStaticOptions,
                () => _refreshPairCallback?.Invoke()?.ToList(),
                _defaultIndex,
                _defaultByValue,
                HasRefresh,
                IsPairValue
            );
            Strategy.Add(el, ShowWhenCondition);
        }
    }
}
