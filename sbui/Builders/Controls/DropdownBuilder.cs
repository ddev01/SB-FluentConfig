using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DropdownBuilder : ControlOptionsBase, IFlushableControlBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string[] _options = Array.Empty<string>();
        private IEnumerable<(string Value, string Display)> _pairOptions;
        private string _valueKey;
        private Func<string[]> _refreshCallback;
        private Func<IEnumerable<(string Value, string Display)>> _refreshPairCallback;
        private int _defaultIndex;
        private string _defaultByValue;
        private string _showWhenKey;

        internal bool IsPairValue => _valueKey != null;
        internal bool HasRefresh => _refreshCallback != null || _refreshPairCallback != null;

        internal DropdownBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public DropdownBuilder Options(IEnumerable<(string Value, string Display)> pairOptions)
        {
            _pairOptions = pairOptions?.ToList() ?? new List<(string, string)>();
            _options = _pairOptions.Select(p => p.Display).ToArray();
            return this;
        }

        public DropdownBuilder WithRefresh(Func<string[]> callback)
        {
            _refreshCallback = callback;
            _refreshPairCallback = null;
            return this;
        }

        public DropdownBuilder WithRefresh(Func<IEnumerable<(string Value, string Display)>> callback)
        {
            _refreshPairCallback = callback;
            _refreshCallback = null;
            return this;
        }

        public override void Hint(string text) { _hint = text; }
        public override void Options(string[] options) { _options = options ?? Array.Empty<string>(); _pairOptions = null; }
        public override void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions) { _pairOptions = pairOptions?.ToList(); _options = _pairOptions?.Select(p => p.Display).ToArray() ?? Array.Empty<string>(); }
        public override void WithPairValue(string valueKey) { _valueKey = valueKey; }
        public override void DefaultByValue(string value) { _defaultByValue = value; }
        public override void Refresh(Func<string[]> callback) { _refreshCallback = callback; _refreshPairCallback = null; }
        public override void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback)
        {
            _refreshCallback = null;
            if (callback != null)
                _refreshPairCallback = () => callback()?.ToList();
            else
                _refreshPairCallback = null;
        }
        public override void DefaultIndex(int index) { _defaultIndex = index; }
        public override void ShowWhen(string key) { _showWhenKey = key; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var fullValueKey = IsPairValue ? _strategy.GetFullSaveKey(_valueKey) : null;
            var pairOpts = _pairOptions?.ToList();
            var el = new DropdownElement(
                Label,
                _hint ?? "",
                _tabName,
                fullKey,
                fullValueKey,
                _options,
                pairOpts,
                _refreshCallback,
                () => _refreshPairCallback?.Invoke()?.ToList(),
                _defaultIndex,
                _defaultByValue,
                HasRefresh,
                IsPairValue
            );
            _strategy.Add(el, _showWhenKey);
        }
    }
}
