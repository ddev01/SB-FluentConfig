using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class PillInputBuilder : IFlushableControlBuilder
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
