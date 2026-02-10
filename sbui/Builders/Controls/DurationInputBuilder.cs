using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DurationInputBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        internal string HintText { get; private set; }
        private bool _permanentOption = true;
        internal bool PermanentOption => _permanentOption;
        internal string DefaultValue { get; private set; } = "permanent";
        internal string ShowWhenKey { get; private set; }

        internal DurationInputBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public DurationInputBuilder Hint(string text) { HintText = text; return this; }
        public DurationInputBuilder Default(string value) { DefaultValue = value; return this; }
        public DurationInputBuilder WithPermanentOption(bool value) { _permanentOption = value; return this; }
        public DurationInputBuilder ShowWhen(string key) { ShowWhenKey = key; return this; }

        void IControlOptions.Hint(string text) { HintText = text; }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { DefaultValue = value; }
        void IControlOptions.Default(int value) { }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { ShowWhenKey = key; }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { }
        void IControlOptions.Text(string caption) { }
        void IControlOptions.OnClick(Action<UiContext> callback) { }
        void IControlOptions.WithPermanentOption(bool value) { _permanentOption = value; }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DurationInputElement(Label, HintText ?? "", _tabName, fullKey, _permanentOption, DefaultValue ?? "permanent");
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
