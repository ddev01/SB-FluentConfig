using System;
using System.Windows.Controls;
using FluentConfig.Elements;

namespace FluentConfig
{
    /// <summary>
    /// Strategy for adding an element (to tab directly or to a panel via WithPanel). Used by control builders.
    /// </summary>
    public interface IAddStrategy
    {
        string GetFullSaveKey(string saveKey);
        void Add(Elements.UIElement element, string showWhenKey);
    }

    internal sealed class AddToTabStrategy : IAddStrategy
    {
        private readonly FluentConfig _ui;
        private readonly string _tabName;

        public AddToTabStrategy(FluentConfig ui, string tabName)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
        }

        public string GetFullSaveKey(string saveKey) => _ui.GetFullSaveKey(saveKey);

        public void Add(Elements.UIElement element, string showWhenKey)
        {
            _ui.AddElement(_tabName, element, showWhenKey);
        }
    }

    internal sealed class AddToPanelStrategy : IAddStrategy
    {
        private readonly FluentConfig _ui;
        private readonly Panel _panel;
        private readonly string _tabName;

        public AddToPanelStrategy(FluentConfig ui, Panel panel, string tabName)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _tabName = tabName ?? "";
        }

        public string GetFullSaveKey(string saveKey) => _ui.GetFullSaveKey(saveKey);

        public void Add(Elements.UIElement element, string showWhenKey)
        {
            _ui.WithPanel(_panel, () => _ui.AddElement(_tabName, element, showWhenKey));
        }
    }
}
