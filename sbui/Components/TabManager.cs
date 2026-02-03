using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sbui.Components
{
    /// <summary>
    /// Ensures tab exists, maps tab name to StackPanel, and manages sidebar selection (scroll offset save/restore).
    /// </summary>
    public class TabManager
    {
        private readonly Dictionary<string, StackPanel> _tabContentPanels = new Dictionary<string, StackPanel>();
        private readonly StackPanel _tabContainer;
        private readonly ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private readonly Dictionary<string, double> _tabScrollOffsets = new Dictionary<string, double>();

        public TabManager(StackPanel tabContainer, ListBox sidebar)
        {
            _tabContainer = tabContainer;
            _sidebar = sidebar;
            if (_sidebar != null)
            {
                _sidebar.SelectionChanged += OnSelectionChanged;
            }
        }

        public void SetScrollViewer(ScrollViewer scrollViewer)
        {
            _contentScrollViewer = scrollViewer;
        }

        /// <summary>
        /// Ensures a tab with the given name exists; creates panel and sidebar item if needed. Returns the content panel.
        /// </summary>
        public StackPanel EnsureTab(string tabName)
        {
            if (_tabContentPanels == null || _tabContainer == null || _sidebar == null)
                return null;
            if (_tabContentPanels.TryGetValue(tabName, out var existing))
                return existing;

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };

            var listItem = new ListBoxItem
            {
                Content = tabName,
                Tag = tabName,
                Padding = new Thickness(12, 10, 12, 10)
            };

            _tabContentPanels[tabName] = panel;
            _tabContainer.Children.Add(panel);
            _sidebar.Items.Add(listItem);

            if (_sidebar.SelectedItem == null)
            {
                _sidebar.SelectedItem = listItem;
                panel.Visibility = Visibility.Visible;
            }

            return panel;
        }

        /// <summary>
        /// Gets the content panel for a tab, or null if it doesn't exist.
        /// </summary>
        public StackPanel GetPanel(string tabName)
        {
            return _tabContentPanels != null && _tabContentPanels.TryGetValue(tabName, out var panel) ? panel : null;
        }

        public ListBox Sidebar => _sidebar;
        public StackPanel TabContainer => _tabContainer;
        public IReadOnlyDictionary<string, StackPanel> TabContentPanels => _tabContentPanels;

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ListBoxItem removed && removed.Tag is string prevTab &&
                _tabContentPanels.TryGetValue(prevTab, out var prevPanel))
            {
                if (_contentScrollViewer != null)
                    _tabScrollOffsets[prevTab] = _contentScrollViewer.VerticalOffset;
                prevPanel.Visibility = Visibility.Collapsed;
            }
            if (_sidebar.SelectedItem is ListBoxItem selected && selected.Tag is string tabName &&
                _tabContentPanels.TryGetValue(tabName, out var panel))
            {
                panel.Visibility = Visibility.Visible;
                if (_contentScrollViewer != null && _tabScrollOffsets.TryGetValue(tabName, out var offset))
                    _contentScrollViewer.ScrollToVerticalOffset(offset);
            }
        }
    }
}
