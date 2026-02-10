using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Sbui.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class IntegerInputElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int DefaultValue { get; set; }

        public IntegerInputElement(string title, string description, string tabName, string saveKey, int defaultValue, int min, int max, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var token = context.GetSetting(SaveKey);
            int val = DefaultValue;
            if (token != null && (token.Type == Newtonsoft.Json.Linq.JTokenType.Integer || token.Type == Newtonsoft.Json.Linq.JTokenType.Float))
            {
                try { val = token.ToObject<int>(); }
                catch (Exception) { /* use DefaultValue */ }
            }
            val = Math.Max(Min, Math.Min(Max, val));

            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            var tb = new System.Windows.Controls.TextBox
            {
                Tag = SbuiTags.IntegerPrefix + SaveKey,
                Text = val.ToString(),
                Width = 80,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            tb.TextChanged += (s, e) =>
            {
                context.MarkDirty();
                if (int.TryParse(tb.Text, out var v))
                    tb.Text = Math.Max(Min, Math.Min(Max, v)).ToString();
            };

            context.Registry.Register(SaveKey, tb);

            var minusBtn = new Button
            {
                Content = new System.Windows.Controls.TextBlock { Text = "−", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                Width = 28, Height = 28, Margin = new Thickness(0, 4, 2, 0), Padding = new Thickness(0)
            };
            minusBtn.Click += (s, e) => { if (int.TryParse(tb.Text, out var v)) { v = Math.Max(Min, v - 1); tb.Text = v.ToString(); context.MarkDirty(); } };

            var plusBtn = new Button
            {
                Content = new System.Windows.Controls.TextBlock { Text = "+", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                Width = 28, Height = 28, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(0)
            };
            plusBtn.Click += (s, e) => { if (int.TryParse(tb.Text, out var v)) { v = Math.Min(Max, v + 1); tb.Text = v.ToString(); context.MarkDirty(); } };

            var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal };
            buttonPanel.Children.Add(minusBtn);
            buttonPanel.Children.Add(plusBtn);

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(tb, 0);
            Grid.SetColumn(buttonPanel, 1);
            row.Children.Add(tb);
            row.Children.Add(buttonPanel);
            stack.Children.Add(row);
            panel.Children.Add(stack);
        }
    }
}
