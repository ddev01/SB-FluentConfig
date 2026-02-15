using System;
using System.Windows.Controls;
using FluentConfig.Core;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class StatusIndicatorBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly FluentConfig _ui;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _initialStatus = "Red";
        private string _buttonText;
        private Action<UiContext> _onClick;
        private Action<UiContext> _onLoadCheck;
        private VisibilityCondition _showWhenCondition;

        internal StatusIndicatorBuilder(IAddStrategy strategy, FluentConfig ui, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public StatusIndicatorBuilder Hint(string text) { _hint = text; return this; }
        public StatusIndicatorBuilder InitialStatus(string status) { _initialStatus = status ?? "Red"; return this; }
        public StatusIndicatorBuilder ButtonText(string text) { _buttonText = text; return this; }
        public StatusIndicatorBuilder OnClick(Action<UiContext> callback) { _onClick = callback; return this; }
        public StatusIndicatorBuilder OnLoadCheck(Action<UiContext> callback) { _onLoadCheck = callback; return this; }
        public StatusIndicatorBuilder ShowWhen(string key) { _showWhenCondition = VisibilityCondition.Toggle(key); return this; }

        void IControlOptions.Hint(string text) { _hint = text; }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { }
        void IControlOptions.Default(int value) { }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { _showWhenCondition = VisibilityCondition.Toggle(key); }
        void IControlOptions.ShowWhen(string[] dependencyKeys, Func<IRenderContext, bool> predicate) { _showWhenCondition = VisibilityCondition.FromPredicate(dependencyKeys, predicate); }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.OptionsPairs(System.Collections.Generic.IList<(string display, string id)> pairs) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.Refresh(Func<IRenderContext, string[]> callback) { }
        void IControlOptions.Refresh(string[] options) { }
        void IControlOptions.RefreshPairs(Func<System.Collections.Generic.IList<(string display, string id)>> callback) { }
        void IControlOptions.WithPairValue(string displayKey, string idKey) { }
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.AllowDuplicates(bool value) { }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { }
        void IControlOptions.Text(string caption) { _buttonText = caption; }
        void IControlOptions.OnClick(Action<UiContext> callback) { _onClick = callback; }
        void IControlOptions.InitialStatus(string status) { _initialStatus = status ?? "Red"; }
        void IControlOptions.OnLoadCheck(Action<UiContext> callback) { _onLoadCheck = callback; }
        void IControlOptions.WithPermanentOption(bool value) { }
        void IControlOptions.Units(string[] labels) { }
        void IControlOptions.AsTimePicker() { }
        void IControlOptions.Placeholder(string text) { }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.Type(string value) { }
        void IControlOptions.Width(string value) { }
        void IControlOptions.Width(int value) { }
        void IControlOptions.ColSpan(int n) { }
        void IControlOptions.RowSpan(int n) { }
        void IControlOptions.Justify(string value) { }
        void IControlOptions.Align(string value) { }
        void IControlOptions.Padding(double value) { }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new StatusIndicatorElement(Label, _hint ?? "", _tabName, fullKey, _initialStatus, _buttonText, _onClick, _onLoadCheck, null);
            _strategy.Add(el, _showWhenCondition);
        }
    }
}
