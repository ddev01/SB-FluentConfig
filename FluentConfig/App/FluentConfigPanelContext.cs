using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FluentConfig.Core;
using FluentConfig.Elements;
using FluentConfig.Helpers;
using Wpf.Ui.Controls;

namespace FluentConfig
{
    internal struct LayoutCellOptions
    {
        public int ColSpan;
        public int RowSpan;
        public HorizontalAlignment HorizontalAlignment;
        public VerticalAlignment VerticalAlignment;
        public Thickness? Padding;

        public static LayoutCellOptions Default => new LayoutCellOptions
        {
            ColSpan = 1,
            RowSpan = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = null
        };
    }

    internal interface ILayoutState
    {
        Panel CreateCell();
        LayoutCellOptions DefaultOptions { get; }
        void PlaceChild(Panel child, LayoutCellOptions opts);
    }

    internal static class LayoutAlignmentHelper
    {
        public static HorizontalAlignment ParseJustify(string value, HorizontalAlignment defaultVal)
        {
            if (string.IsNullOrEmpty(value)) return defaultVal;
            var v = value.Trim().ToLowerInvariant();
            if (v == "left" || v == "start") return HorizontalAlignment.Left;
            if (v == "right" || v == "end") return HorizontalAlignment.Right;
            if (v == "center") return HorizontalAlignment.Center;
            if (v == "stretch") return HorizontalAlignment.Stretch;
            return defaultVal;
        }

        public static VerticalAlignment ParseAlign(string value, VerticalAlignment defaultVal)
        {
            if (string.IsNullOrEmpty(value)) return defaultVal;
            var v = value.Trim().ToLowerInvariant();
            if (v == "top" || v == "start") return VerticalAlignment.Top;
            if (v == "bottom" || v == "end") return VerticalAlignment.Bottom;
            if (v == "center") return VerticalAlignment.Center;
            if (v == "stretch") return VerticalAlignment.Stretch;
            return defaultVal;
        }

        public static GridLength ParseColumnWidth(string value)
        {
            if (string.IsNullOrEmpty(value)) return new GridLength(1, GridUnitType.Star);
            var v = value.Trim().ToLowerInvariant();
            if (v == "auto") return GridLength.Auto;
            if (v == "*" || v == "1*") return new GridLength(1, GridUnitType.Star);
            if (v.EndsWith("*") && v.Length > 1)
            {
                var numStr = v.Substring(0, v.Length - 1);
                if (double.TryParse(numStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var star) && star > 0)
                    return new GridLength(star, GridUnitType.Star);
            }
            if (double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var px) && px >= 0)
                return new GridLength(px, GridUnitType.Pixel);
            return new GridLength(1, GridUnitType.Star);
        }
    }

    internal sealed class GridLayoutState : ILayoutState
    {
        private readonly System.Windows.Controls.Grid _grid;
        private readonly int _numCols;
        private readonly int _gap;
        private readonly HorizontalAlignment _defaultJustify;
        private readonly VerticalAlignment _defaultAlign;
        private readonly Thickness? _defaultPadding;
        private readonly int[] _nextRowPerCol;
        private int _row;
        private int _col;

        internal GridLayoutState(System.Windows.Controls.Grid grid, int numCols, int gap = 0,
            HorizontalAlignment defaultJustify = HorizontalAlignment.Stretch,
            VerticalAlignment defaultAlign = VerticalAlignment.Stretch,
            Thickness? defaultPadding = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _numCols = Math.Max(1, numCols);
            _gap = Math.Max(0, gap);
            _defaultJustify = defaultJustify;
            _defaultAlign = defaultAlign;
            _defaultPadding = defaultPadding;
            _nextRowPerCol = new int[_numCols];
            _row = 0;
            _col = 0;
        }

        public Panel CreateCell()
        {
            return new StackPanel { Orientation = Orientation.Vertical };
        }

        public LayoutCellOptions DefaultOptions => new LayoutCellOptions
        {
            ColSpan = 1,
            RowSpan = 1,
            HorizontalAlignment = _defaultJustify,
            VerticalAlignment = _defaultAlign,
            Padding = _defaultPadding
        };

        public void PlaceChild(Panel child, LayoutCellOptions opts)
        {
            var colSpan = Math.Max(1, Math.Min(opts.ColSpan, _numCols));
            var rowSpan = Math.Max(1, opts.RowSpan);

            if (child is FrameworkElement fe)
            {
                fe.HorizontalAlignment = opts.HorizontalAlignment;
                fe.VerticalAlignment = opts.VerticalAlignment;
            }
            if (opts.Padding.HasValue)
            {
                child.Margin = opts.Padding.Value;
            }

            if (_col + colSpan > _numCols)
            {
                _col = 0;
                _row = 0;
                for (int i = 0; i < _numCols; i++)
                    if (_nextRowPerCol[i] > _row) _row = _nextRowPerCol[i];
            }
            int requiredRow = _row;
            for (int col = _col; col < _col + colSpan; col++)
                if (_nextRowPerCol[col] > requiredRow) requiredRow = _nextRowPerCol[col];
            _row = requiredRow;

            while (_grid.RowDefinitions.Count <= _row + rowSpan - 1)
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            System.Windows.Controls.Grid.SetRow(child, _row);
            System.Windows.Controls.Grid.SetColumn(child, _col);
            System.Windows.Controls.Grid.SetRowSpan(child, rowSpan);
            System.Windows.Controls.Grid.SetColumnSpan(child, colSpan);

            var margin = child.Margin;
            if (_gap > 0)
            {
                var right = _col + colSpan < _numCols ? _gap : 0;
                var bottom = _gap;
                margin = new Thickness(margin.Left, margin.Top, margin.Right + right, margin.Bottom + bottom);
                child.Margin = margin;
            }
            _grid.Children.Add(child);

            for (int col = _col; col < _col + colSpan; col++)
                _nextRowPerCol[col] = _row + rowSpan;

            _col += colSpan;
            if (_col >= _numCols)
            {
                _col = 0;
                _row = 0;
                for (int i = 0; i < _numCols; i++)
                    if (_nextRowPerCol[i] > _row) _row = _nextRowPerCol[i];
            }
        }
    }

    internal sealed class FlexLayoutState : ILayoutState
    {
        private readonly Panel _panel;
        private readonly int _gap;
        private readonly VerticalAlignment _alignItems;
        private readonly HorizontalAlignment _defaultJustify;
        private readonly VerticalAlignment _defaultAlign;
        private readonly Thickness? _defaultPadding;

        internal FlexLayoutState(Panel panel, int gap,
            VerticalAlignment alignItems = VerticalAlignment.Stretch,
            HorizontalAlignment defaultJustify = HorizontalAlignment.Stretch,
            VerticalAlignment defaultAlign = VerticalAlignment.Stretch,
            Thickness? defaultPadding = null)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _gap = Math.Max(0, gap);
            _alignItems = alignItems;
            _defaultJustify = defaultJustify;
            _defaultAlign = defaultAlign;
            _defaultPadding = defaultPadding;
        }

        public Panel CreateCell()
        {
            return new StackPanel { Orientation = Orientation.Vertical };
        }

        public LayoutCellOptions DefaultOptions => new LayoutCellOptions
        {
            ColSpan = 1,
            RowSpan = 1,
            HorizontalAlignment = _defaultJustify,
            VerticalAlignment = _defaultAlign,
            Padding = _defaultPadding
        };

        public void PlaceChild(Panel child, LayoutCellOptions opts)
        {
            var vAlign = opts.VerticalAlignment;
            var hAlign = opts.HorizontalAlignment;

            if (child is FrameworkElement fe)
            {
                fe.HorizontalAlignment = hAlign;
                fe.VerticalAlignment = vAlign;
            }
            if (opts.Padding.HasValue)
            {
                child.Margin = opts.Padding.Value;
            }

            var wrapper = new System.Windows.Controls.Border
            {
                Child = child,
                Margin = new Thickness(0, 0, _gap, 0),
                Padding = new Thickness(0),
                VerticalAlignment = _alignItems
            };
            _panel.Children.Add(wrapper);
        }
    }

    /// <summary>
    /// Manages panel/save-key stacks, WithVisibility, WithRepeatableRows, and Grid/Flex layouts.
    /// Extracted from FluentConfig to reduce god-object surface area.
    /// </summary>
    internal sealed class FluentConfigPanelContext
    {
        private readonly FluentConfig _config;
        private readonly Stack<Panel> _panelContext = new Stack<Panel>();
        private readonly Stack<string> _saveKeyContext = new Stack<string>();
        private readonly Stack<ILayoutState> _layoutStack = new Stack<ILayoutState>();
        private int _nextColSpan = 1;
        private int _nextRowSpan = 1;
        private string _nextJustify;
        private string _nextAlign;
        private double? _nextPadding;

        internal FluentConfigPanelContext(FluentConfig config) => _config = config ?? throw new ArgumentNullException(nameof(config));

        internal bool IsInLayout => _layoutStack.Count > 0;

        internal void SetNextColSpan(int n) => _nextColSpan = Math.Max(1, n);
        internal void SetNextRowSpan(int n) => _nextRowSpan = Math.Max(1, n);
        internal void SetNextJustify(string value) => _nextJustify = value;
        internal void SetNextAlign(string value) => _nextAlign = value;
        internal void SetNextPadding(double value) => _nextPadding = value >= 0 ? value : (double?)null;

        internal (Panel cellPanel, LayoutCellOptions options) PrepareCell()
        {
            var colSpan = _nextColSpan;
            var rowSpan = _nextRowSpan;
            var justify = _nextJustify;
            var align = _nextAlign;
            var padding = _nextPadding;
            _nextColSpan = 1;
            _nextRowSpan = 1;
            _nextJustify = null;
            _nextAlign = null;
            _nextPadding = null;

            if (_layoutStack.Count == 0)
                throw new InvalidOperationException("PrepareCell called outside of Grid/Flex layout");

            var state = _layoutStack.Peek();
            var def = state.DefaultOptions;
            var opts = new LayoutCellOptions
            {
                ColSpan = colSpan,
                RowSpan = rowSpan,
                HorizontalAlignment = LayoutAlignmentHelper.ParseJustify(justify, def.HorizontalAlignment),
                VerticalAlignment = LayoutAlignmentHelper.ParseAlign(align, def.VerticalAlignment),
                Padding = padding.HasValue ? new Thickness(padding.Value) : def.Padding
            };
            var cell = state.CreateCell();
            return (cell, opts);
        }

        internal void PlaceLayoutChild(Panel child, LayoutCellOptions options)
        {
            if (_layoutStack.Count == 0)
                throw new InvalidOperationException("PlaceLayoutChild called outside of Grid/Flex layout");
            _layoutStack.Peek().PlaceChild(child, options);
        }

        internal void PushLayout(ILayoutState state)
        {
            if (state != null) _layoutStack.Push(state);
        }

        internal void PopLayout()
        {
            if (_layoutStack.Count > 0) _layoutStack.Pop();
        }

        internal Panel GetTargetPanel(string tabName)
        {
            if (_panelContext.Count > 0)
                return _panelContext.Peek();
            _config.EnsureTabExists(tabName);
            return _config.VisibilityOverridePanel ?? (Panel)_config.TabManagerInternal?.GetPanel(tabName);
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
            var toggle = _config.ControlRegistryInternal.Get<ToggleSwitch>(toggleSaveKey);
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
            var toggle = _config.ControlRegistryInternal.Get<ToggleSwitch>(toggleSaveKey);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{toggleSaveKey}' must be added before WithVisibility block");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = (toggle.IsChecked == true) != inverted ? Visibility.Visible : Visibility.Collapsed;

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            var pb = new PanelBuilder(_config, container, tabName);
            build(pb);
            pb.FlushPending();
            _panelContext.Pop();

            toggle.Checked += (s, e) => container.Visibility = inverted ? Visibility.Collapsed : Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = inverted ? Visibility.Visible : Visibility.Collapsed;

            if (currentPanel != null)
                currentPanel.Children.Add(container);
        }

        internal void WithVisibility(string[] dependencyKeys, Func<IRenderContext, bool> predicate, string tabName, bool inverted, Action<PanelBuilder> build)
        {
            if (build == null || dependencyKeys == null || dependencyKeys.Length == 0 || predicate == null) return;
            var condition = VisibilityCondition.FromPredicate(dependencyKeys, predicate);
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            UpdateVisibilityFromPredicate(container, condition, inverted);

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            var pb = new PanelBuilder(_config, container, tabName);
            build(pb);
            pb.FlushPending();
            _panelContext.Pop();

            ControlChangeNotifier.Subscribe(_config.ControlRegistryInternal, dependencyKeys, () => UpdateVisibilityFromPredicate(container, condition, inverted));

            if (currentPanel != null)
                currentPanel.Children.Add(container);
        }

        private void UpdateVisibilityFromPredicate(StackPanel container, VisibilityCondition condition, bool inverted)
        {
            try
            {
                var visible = condition.Predicate(_config);
                container.Visibility = (visible != inverted) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                FluentConfig.LogInternal($"[FluentConfig] Visibility predicate error: {ex.Message}");
                container.Visibility = Visibility.Collapsed;
            }
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
                var rowPb = new PanelBuilder(_config, contentPanel, tabName);
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
            var rowPb = new PanelBuilder(_config, contentPanel, tabName);
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
                _config.UnregisterRowControls(saveKey, rowIndex);
                _config.MarkDirty();
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
                _config.EnsureExistingSettings();
                _config.ExistingSettings[saveKey] = arr;
                _config.MarkDirty();
            }
        }

        private JArray LoadRowsData(string saveKey)
        {
            if (_config.ExistingSettings == null) return new JArray();
            var token = _config.ExistingSettings[saveKey];
            if (token is JArray arr) return arr;
            if (token != null)
            {
                try { return JArray.Parse(token.ToString()); }
                catch (Exception ex) { FluentConfig.LogInternal("LoadRowsData parse error: " + ex.Message); }
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
