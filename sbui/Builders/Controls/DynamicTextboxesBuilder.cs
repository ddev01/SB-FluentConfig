using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class DynamicTextboxesBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string[] _presetValues = Array.Empty<string>();
        private string _showWhenKey;

        internal DynamicTextboxesBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public DynamicTextboxesBuilder Hint(string text) { _hint = text; return this; }
        public DynamicTextboxesBuilder Preset(string[] values) { _presetValues = values ?? Array.Empty<string>(); return this; }
        public DynamicTextboxesBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        void IControlOptions.Hint(string text) { _hint = text; }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { }
        void IControlOptions.Default(int value) { }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { _showWhenKey = key; }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.Preset(string[] values) { _presetValues = values ?? Array.Empty<string>(); }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { }
        void IControlOptions.Text(string caption) { }
        void IControlOptions.OnClick(Action<UiContext> callback) { }
        void IControlOptions.WithPermanentOption(bool value) { }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var el = new DynamicTextboxesWithPresetElement(Label, _hint ?? "", _tabName, fullKey, _presetValues);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
