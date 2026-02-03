using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class RefreshableDropdownElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string[] Options { get; set; }
        public Func<string[]> RefreshCallback { get; set; }
        public int DefaultIndex { get; set; }

        public RefreshableDropdownElement(string title, string description, string tabName, string saveKey, string[] options, Func<string[]> refreshCallback, int defaultIndex, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Options = options ?? Array.Empty<string>();
            RefreshCallback = refreshCallback;
            DefaultIndex = defaultIndex;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var options = Options ?? Array.Empty<string>();
            var savedIndex = context.Settings?[SaveKey] != null && context.Settings[SaveKey].Type == Newtonsoft.Json.Linq.JTokenType.Integer
                ? context.Settings[SaveKey].ToObject<int>() : DefaultIndex;
            savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cb = SbuiComponentFactory.CreateComboBox(SaveKey, options, savedIndex);
            cb.SelectionChanged += (s, e) => context.MarkDirty();
            context.Registry.Register(SaveKey, cb);
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
            var refreshCallback = RefreshCallback;
            refreshBtn.Click += (s, e) =>
            {
                try
                {
                    var newOptions = refreshCallback?.Invoke() ?? Array.Empty<string>();
                    var idx = cb.SelectedIndex;
                    if (idx < 0 || idx >= (newOptions?.Length ?? 0)) idx = 0;
                    context.UpdateDropdown(key, newOptions ?? Array.Empty<string>(), idx);
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
            panel.Children.Add(stack);
        }
    }
}
