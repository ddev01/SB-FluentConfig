using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class PillInputBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly Sbui _ui;
        private readonly string _tabName;
        internal readonly string Label;
        internal readonly string Key;
        private string _hint;
        private string _showWhenKey;

        internal Action<StackPanel, Panel, CallbackContext> WithSectionsPanelCallback { get; private set; }
        internal Action<string, StackPanel, CallbackContext> OnPillAddedCallback { get; private set; }
        internal Action<string, StackPanel, CallbackContext> OnPillRemovedCallback { get; private set; }

        internal PillInputBuilder(IAddStrategy strategy, Sbui ui, string tabName, string label, string key)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public PillInputBuilder Hint(string text) { _hint = text; return this; }
        public PillInputBuilder ShowWhen(string key) { _showWhenKey = key; return this; }

        public PillInputBuilder WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build)
        {
            WithSectionsPanelCallback = build;
            return this;
        }

        public PillInputBuilder OnPillAdded(Action<string, StackPanel, CallbackContext> build)
        {
            OnPillAddedCallback = build;
            return this;
        }

        public PillInputBuilder OnPillRemoved(Action<string, StackPanel, CallbackContext> build)
        {
            OnPillRemovedCallback = build;
            return this;
        }

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
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { }
        void IControlOptions.Text(string caption) { }
        void IControlOptions.OnClick(Action<UiContext> callback) { }
        void IControlOptions.WithPermanentOption(bool value) { }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { WithSectionsPanelCallback = build; }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { OnPillAddedCallback = build; }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { OnPillRemovedCallback = build; }

        public void Add()
        {
            var fullKey = _strategy.GetFullSaveKey(Key);
            var ctx = new CallbackContext(_ui);
            Action<StackPanel, Panel> withSections = null;
            if (WithSectionsPanelCallback != null)
                withSections = (sections, tab) => WithSectionsPanelCallback(sections, tab, ctx);
            Action<string, StackPanel> onAdded = null;
            if (OnPillAddedCallback != null)
                onAdded = (alias, sections) => OnPillAddedCallback(alias, sections, ctx);
            Action<string, StackPanel> onRemoved = null;
            if (OnPillRemovedCallback != null)
                onRemoved = (alias, sections) => OnPillRemovedCallback(alias, sections, ctx);

            var el = new PillInputElement(Label, _hint ?? "", _tabName, fullKey, withSections, onAdded, onRemoved);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
