using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Strategy for adding an element (to tab directly or to a panel via WithPanel). Used by control builders.
    /// </summary>
    internal interface IAddStrategy
    {
        string GetFullSaveKey(string saveKey);
        void Add(Elements.UIElement element, string showWhenKey);
    }

    internal sealed class AddToTabStrategy : IAddStrategy
    {
        private readonly Sbui _ui;
        private readonly string _tabName;

        public AddToTabStrategy(Sbui ui, string tabName)
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
        private readonly Sbui _ui;
        private readonly Panel _panel;
        private readonly string _tabName;

        public AddToPanelStrategy(Sbui ui, Panel panel, string tabName)
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
