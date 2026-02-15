using System;
using System.Windows.Controls;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class PillInputBuilder : ControlBuilderBase
    {
        private readonly FluentConfig _ui;
        private Action<StackPanel, Panel, CallbackContext> _withSectionsPanelCallback;
        private Action<string, StackPanel, CallbackContext> _onPillAddedCallback;
        private Action<string, StackPanel, CallbackContext> _onPillRemovedCallback;

        internal PillInputBuilder(IAddStrategy strategy, FluentConfig ui, string tabName, string label, string key)
            : base(strategy, tabName, label, key)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public override void WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { _withSectionsPanelCallback = build; }
        public override void OnPillAdded(Action<string, StackPanel, CallbackContext> build) { _onPillAddedCallback = build; }
        public override void OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { _onPillRemovedCallback = build; }

        public override void Add()
        {
            var fullKey = Strategy.GetFullSaveKey(Key);
            var ctx = new CallbackContext(_ui);
            Action<StackPanel, Panel> withSections = null;
            if (_withSectionsPanelCallback != null)
                withSections = (sections, tab) => _withSectionsPanelCallback(sections, tab, ctx);
            Action<string, StackPanel> onAdded = null;
            if (_onPillAddedCallback != null)
                onAdded = (alias, sections) => _onPillAddedCallback(alias, sections, ctx);
            Action<string, StackPanel> onRemoved = null;
            if (_onPillRemovedCallback != null)
                onRemoved = (alias, sections) => _onPillRemovedCallback(alias, sections, ctx);

            var el = new PillInputElement(Label, HintText ?? "", TabName, fullKey, withSections, onAdded, onRemoved);
            Strategy.Add(el, ShowWhenCondition);
        }
    }
}
