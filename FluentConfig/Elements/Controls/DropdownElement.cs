using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FluentConfig.Components;
using FluentConfig.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace FluentConfig.Elements
{
    /// <summary>Display/value pair for pair-value dropdowns.</summary>
    public class DropdownItem
    {
        public string Display { get; set; }
        public string Value { get; set; }
    }

    public class DropdownElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string ValueKey { get; set; }
        public string[] Options { get; set; }
        public List<(string Value, string Display)> PairOptions { get; set; }
        public Func<string[]> RefreshCallback { get; set; }
        public Func<IRenderContext, string[]> RefreshWithContextCallback { get; set; }
        public string[] RefreshStaticOptions { get; set; }
        public Func<List<(string Value, string Display)>> RefreshPairCallback { get; set; }
        public int DefaultIndex { get; set; }
        public string DefaultByValue { get; set; }
        public bool HasRefresh { get; set; }
        public bool IsPairValue { get; set; }

        public DropdownElement(
            string title,
            string description,
            string tabName,
            string saveKey,
            string valueKey,
            string[] options,
            List<(string Value, string Display)> pairOptions,
            Func<string[]> refreshCallback,
            Func<IRenderContext, string[]> refreshWithContextCallback,
            string[] refreshStaticOptions,
            Func<List<(string Value, string Display)>> refreshPairCallback,
            int defaultIndex,
            string defaultByValue,
            bool hasRefresh,
            bool isPairValue)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            ValueKey = valueKey;
            Options = options ?? Array.Empty<string>();
            PairOptions = pairOptions;
            RefreshCallback = refreshCallback;
            RefreshWithContextCallback = refreshWithContextCallback;
            RefreshStaticOptions = refreshStaticOptions;
            RefreshPairCallback = refreshPairCallback;
            DefaultIndex = defaultIndex;
            DefaultByValue = defaultByValue;
            HasRefresh = hasRefresh;
            IsPairValue = isPairValue;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            System.Windows.Controls.ComboBox cb;
            int savedIndex = 0;
            string savedValue = null;

            if (IsPairValue && PairOptions != null && PairOptions.Count > 0)
            {
                var displayVal = context.GetSetting(SaveKey)?.ToString();
                var valueVal = context.GetSetting(ValueKey)?.ToString();
                savedValue = !string.IsNullOrEmpty(valueVal) ? valueVal : DefaultByValue;
                var items = PairOptions.Select(p => new DropdownItem { Value = p.Value, Display = p.Display }).ToList();
                var idx = items.FindIndex(i => string.Equals(i.Value, savedValue, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) idx = items.FindIndex(i => string.Equals(i.Display, displayVal, StringComparison.OrdinalIgnoreCase));
                savedIndex = Math.Max(0, Math.Min(idx >= 0 ? idx : 0, items.Count - 1));

                cb = new System.Windows.Controls.ComboBox
                {
                    Tag = FluentConfigTags.PairPrefix + SaveKey + "," + ValueKey,
                    ItemsSource = items,
                    DisplayMemberPath = "Display",
                    SelectedValuePath = "Value",
                    MinWidth = 200,
                    MinHeight = 30,
                    Height = 30,
                    Padding = new System.Windows.Thickness(6, 4, 6, 4),
                    Margin = new System.Windows.Thickness(0, 4, 8, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                cb.SelectedIndex = savedIndex;
            }
            else
            {
                var options = Options ?? Array.Empty<string>();
                var token = context.GetSetting(SaveKey);
                savedIndex = token != null && token.Type == Newtonsoft.Json.Linq.JTokenType.Integer
                    ? token.ToObject<int>() : DefaultIndex;
                savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
                cb = FluentConfigComponentFactory.CreateComboBox(SaveKey, options, savedIndex);
            }

            cb.SelectionChanged += (s, e) => context.MarkDirty();
            context.Registry.Register(SaveKey, cb);
            if (IsPairValue && ValueKey != null)
                context.Registry.Register(ValueKey, cb);

            var stack = FluentConfigComponentFactory.CreateTitledStack(Title, Description);

            if (HasRefresh)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var refreshIcon = new System.Windows.Controls.TextBlock
                {
                    Text = "\uE117",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var refreshBtn = new Button { Content = refreshIcon, ToolTip = "Refresh", Width = 30, Height = 30, Padding = new System.Windows.Thickness(0), Margin = new System.Windows.Thickness(0, 4, 0, 0) };
                var key = SaveKey;
                var valKey = ValueKey;
                var refreshCb = RefreshCallback;
                var refreshWithContextCb = RefreshWithContextCallback;
                var refreshStatic = RefreshStaticOptions;
                var refreshPairCb = RefreshPairCallback;
                refreshBtn.Click += (s, e) =>
                {
                    try
                    {
                        if (IsPairValue && refreshPairCb != null)
                        {
                            var newPairs = refreshPairCb() ?? new List<(string, string)>();
                            var selectedVal = (cb.SelectedItem as DropdownItem)?.Value;
                            context.UpdateDropdownWithPairValue(key, valKey, newPairs, selectedVal);
                        }
                        else if (refreshWithContextCb != null)
                        {
                            var newOptions = refreshWithContextCb(context) ?? Array.Empty<string>();
                            var idx = cb.SelectedIndex;
                            if (idx < 0 || idx >= (newOptions?.Length ?? 0)) idx = 0;
                            context.UpdateDropdown(key, newOptions ?? Array.Empty<string>(), idx);
                        }
                        else if (refreshCb != null)
                        {
                            var newOptions = refreshCb() ?? Array.Empty<string>();
                            var idx = cb.SelectedIndex;
                            if (idx < 0 || idx >= (newOptions?.Length ?? 0)) idx = 0;
                            context.UpdateDropdown(key, newOptions ?? Array.Empty<string>(), idx);
                        }
                        else if (refreshStatic != null)
                        {
                            var idx = cb.SelectedIndex;
                            if (idx < 0 || idx >= (refreshStatic.Length)) idx = 0;
                            context.UpdateDropdown(key, refreshStatic, idx);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Log($"Refresh callback error: {ex.Message}");
                    }
                };
                Grid.SetColumn(cb, 0);
                Grid.SetColumn(refreshBtn, 1);
                row.Children.Add(cb);
                row.Children.Add(refreshBtn);
                stack.Children.Add(row);
            }
            else
            {
                stack.Children.Add(cb);
            }
            panel.Children.Add(stack);
        }
    }
}
