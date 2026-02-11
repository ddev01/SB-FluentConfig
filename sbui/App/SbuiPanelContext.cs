using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Sbui.Core;
using Wpf.Ui.Controls;

namespace Sbui
{
    /// <summary>
    /// Manages panel/save-key stacks, WithVisibility, and WithRepeatableRows.
    /// Extracted from Sbui to reduce god-object surface area.
    /// </summary>
    internal sealed class SbuiPanelContext
    {
        private readonly Sbui _sbui;
        private readonly Stack<Panel> _panelContext = new Stack<Panel>();
        private readonly Stack<string> _saveKeyContext = new Stack<string>();

        internal SbuiPanelContext(Sbui sbui) => _sbui = sbui ?? throw new ArgumentNullException(nameof(sbui));

        internal Panel GetTargetPanel(string tabName)
        {
            if (_panelContext.Count > 0)
                return _panelContext.Peek();
            _sbui.EnsureTabExists(tabName);
            return _sbui.VisibilityOverridePanel ?? (Panel)_sbui.TabManagerInternal?.GetPanel(tabName);
        }

        internal string GetFullSaveKey(string saveKey)
        {
            return _saveKeyContext.Count > 0
                ? $"{_saveKeyContext.Peek()}.{saveKey}"
                : saveKey;
        }

        internal void WithPanel(Panel panel, Action content)
        {
            if (panel == null || content == null) return;
            _panelContext.Push(panel);
            try { content(); }
            finally { _panelContext.Pop(); }
        }

        internal void PushPanel(Panel panel) => _panelContext.Push(panel);
        internal void PopPanel() => _panelContext.Pop();
        internal void PushSaveKey(string key) => _saveKeyContext.Push(key);
        internal void PopSaveKey() => _saveKeyContext.Pop();

        internal void WithVisibility(string toggleSaveKey, string tabName, Action content, bool inverted = false)
        {
            if (content == null) return;
            var toggle = _sbui.ControlRegistryInternal.Get<ToggleSwitch>(toggleSaveKey);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{toggleSaveKey}' must be added before WithVisibility block");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = (toggle.IsChecked == true) != inverted ? Visibility.Visible : Visibility.Collapsed;

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            content();
            _panelContext.Pop();

            toggle.Checked += (s, e) => container.Visibility = inverted ? Visibility.Collapsed : Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = inverted ? Visibility.Visible : Visibility.Collapsed;

            if (currentPanel != null)
                currentPanel.Children.Add(container);
        }

        internal void WithVisibility(string toggleSaveKey, string tabName, bool inverted, Action<PanelBuilder> build)
        {
            if (build == null) return;
            var toggle = _sbui.ControlRegistryInternal.Get<ToggleSwitch>(toggleSaveKey);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{toggleSaveKey}' must be added before WithVisibility block");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = (toggle.IsChecked == true) != inverted ? Visibility.Visible : Visibility.Collapsed;

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            var pb = new PanelBuilder(_sbui, container, tabName);
            build(pb);
            pb.FlushPending();
            _panelContext.Pop();

            toggle.Checked += (s, e) => container.Visibility = inverted ? Visibility.Collapsed : Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = inverted ? Visibility.Visible : Visibility.Collapsed;

            if (currentPanel != null)
                currentPanel.Children.Add(container);
        }

        internal void WithRepeatableRows(string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            if (buildRow == null) return;
            var panel = GetTargetPanel(tabName);
            if (panel == null) return;

            var container = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            var rowsData = LoadRowsData(saveKey);

            for (int i = 0; i < rowsData.Count; i++)
            {
                var (outerRow, contentPanel) = CreateRowPanel(i, saveKey, container);
                _panelContext.Push(contentPanel);
                _saveKeyContext.Push($"{saveKey}[{i}]");
                var rowPb = new PanelBuilder(_sbui, contentPanel, tabName);
                buildRow(rowPb);
                rowPb.FlushPending();
                _saveKeyContext.Pop();
                _panelContext.Pop();
                container.Children.Add(outerRow);
            }

            var addBtn = new Wpf.Ui.Controls.Button
            {
                Content = "+ Add Row",
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(15, 5, 15, 5)
            };
            addBtn.Click += (s, e) => AddRowWithBuilder(container, saveKey, tabName, buildRow);
            container.Children.Add(addBtn);

            panel.Children.Add(container);
        }

        private void AddRowWithBuilder(Panel container, string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            int newIndex = Math.Max(0, container.Children.Count - 1);
            var (outerRow, contentPanel) = CreateRowPanel(newIndex, saveKey, container);
            _panelContext.Push(contentPanel);
            _saveKeyContext.Push($"{saveKey}[{newIndex}]");
            var rowPb = new PanelBuilder(_sbui, contentPanel, tabName);
            buildRow(rowPb);
            rowPb.FlushPending();
            _saveKeyContext.Pop();
            _panelContext.Pop();
            container.Children.Insert(container.Children.Count - 1, outerRow);
        }

        private (DockPanel outerRow, StackPanel contentPanel) CreateRowPanel(int rowIndex, string saveKey, Panel container)
        {
            var outerRow = new DockPanel
            {
                Margin = new Thickness(0, 5, 0, 5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255))
            };
            outerRow.Tag = new RowTag { RowIndex = rowIndex, SaveKey = saveKey };

            var deleteBtn = new Wpf.Ui.Controls.Button
            {
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = "×",
                    FontSize = 18,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                MinWidth = 30,
                MinHeight = 30
            };
            System.Windows.Controls.Panel.SetZIndex(deleteBtn, 10);

            var contentPanel = new StackPanel { Margin = new Thickness(10) };
            deleteBtn.Click += (s, e) =>
            {
                container.Children.Remove(outerRow);
                RenumberRows(container, saveKey);
                DeleteRowData(saveKey, rowIndex);
                _sbui.MarkDirty();
            };
            DockPanel.SetDock(deleteBtn, Dock.Right);
            outerRow.Children.Add(deleteBtn);
            outerRow.Children.Add(contentPanel);

            return (outerRow, contentPanel);
        }

        private void RenumberRows(Panel container, string saveKey)
        {
            int index = 0;
            foreach (var child in container.Children)
            {
                if (child is DockPanel dock && dock.Tag is RowTag tag)
                    tag.RowIndex = index++;
            }
        }

        private void DeleteRowData(string saveKey, int rowIndex)
        {
            var arr = LoadRowsData(saveKey);
            if (rowIndex >= 0 && rowIndex < arr.Count)
            {
                arr.RemoveAt(rowIndex);
                _sbui.EnsureExistingSettings();
                _sbui.ExistingSettings[saveKey] = arr;
                _sbui.MarkDirty();
            }
        }

        private JArray LoadRowsData(string saveKey)
        {
            if (_sbui.ExistingSettings == null) return new JArray();
            var token = _sbui.ExistingSettings[saveKey];
            if (token is JArray arr) return arr;
            if (token != null)
            {
                try { return JArray.Parse(token.ToString()); }
                catch (Exception ex) { Sbui.LogInternal("LoadRowsData parse error: " + ex.Message); }
            }
            return new JArray();
        }

        private class RowTag
        {
            internal int RowIndex;
            internal string SaveKey;
        }
    }
}
