using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class SliderWithToggleBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private int _min;
        private int _max = 100;
        private int _defaultValue;
        private bool _toggleDefault;
        private string _showWhenKey;

        internal SliderWithToggleBuilder(IAddStrategy strategy, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public SliderWithToggleBuilder Hint(string text) { _hint = text; return this; }
        public SliderWithToggleBuilder Range(int min, int max) { _min = min; _max = max; return this; }
        public SliderWithToggleBuilder Default(int value) { _defaultValue = value; return this; }
        public SliderWithToggleBuilder ToggleDefault(bool value) { _toggleDefault = value; return this; }
        public SliderWithToggleBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        void IControlOptions.Hint(string text) { _hint = text; }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { }
        void IControlOptions.Default(int value) { _defaultValue = value; }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { _min = min; _max = max; }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { _showWhenKey = key; }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.ToggleDefault(bool value) { _toggleDefault = value; }
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
            var el = new SliderWithToggleSwitchElement(Label, _hint ?? "", _tabName, fullKey, _min, _max, _defaultValue, _toggleDefault);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
