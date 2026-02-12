using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FluentConfig.Components
{
    /// <summary>
    /// Ensures tab exists, maps tab name to StackPanel, and manages sidebar selection (scroll offset save/restore).
    /// Supports deferred (lazy) tab building: register a build callback and it runs on first tab switch.
    /// </summary>
    public class TabManager
    {
        private readonly Dictionary<string, StackPanel> _tabContentPanels = new Dictionary<string, StackPanel>();
        private readonly StackPanel _tabContainer;
        private readonly ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private readonly Dictionary<string, double> _tabScrollOffsets = new Dictionary<string, double>();
        private readonly Dictionary<string, Action> _deferredBuilders = new Dictionary<string, Action>();

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
        /// Registers a deferred builder for a tab. The builder will be invoked the first time the tab is selected.
        /// The tab and sidebar item are created immediately (so they appear in the sidebar) but remain empty.
        /// </summary>
        public void RegisterDeferredBuilder(string tabName, Action builder)
        {
            if (string.IsNullOrEmpty(tabName) || builder == null) return;
            EnsureTab(tabName);
            _deferredBuilders[tabName] = builder;
        }

        /// <summary>
        /// Builds the deferred content for a specific tab (the first/eager tab).
        /// Called after the window is shown to populate the initially visible tab.
        /// </summary>
        internal void BuildFirstDeferred(string tabName)
        {
            if (_deferredBuilders.TryGetValue(tabName, out var builder))
            {
                builder();
                _deferredBuilders.Remove(tabName);
            }
        }

        /// <summary>
        /// Builds all remaining deferred tabs that haven't been built yet.
        /// </summary>
        internal void BuildAllDeferred()
        {
            // Copy keys to avoid modifying collection during iteration
            var keys = new List<string>(_deferredBuilders.Keys);
            foreach (var key in keys)
            {
                if (_deferredBuilders.TryGetValue(key, out var builder))
                {
                    builder();
                    _deferredBuilders.Remove(key);
                }
            }
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
                // Build deferred content on first activation
                if (_deferredBuilders.TryGetValue(tabName, out var builder))
                {
                    builder();
                    _deferredBuilders.Remove(tabName);
                }
                panel.Visibility = Visibility.Visible;
                if (_contentScrollViewer != null && _tabScrollOffsets.TryGetValue(tabName, out var offset))
                    _contentScrollViewer.ScrollToVerticalOffset(offset);
            }
        }
    }
}
