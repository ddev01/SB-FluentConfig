using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class NumberInputElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; }
        public double DefaultValue { get; set; }
        public bool WithStepper { get; set; }

        public NumberInputElement(string title, string description, string tabName, string saveKey, double min, double max, double step, double defaultValue, bool withStepper = false, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Min = min;
            Max = max;
            Step = step;
            DefaultValue = defaultValue;
            WithStepper = withStepper;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var token = context.GetSetting(SaveKey);
            var val = token != null && (token.Type == Newtonsoft.Json.Linq.JTokenType.Float || token.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                ? token.ToObject<double>() : DefaultValue;
            val = Math.Max(Min, Math.Min(Max, val));

            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            var tb = new System.Windows.Controls.TextBox
            {
                Tag = SaveKey,
                Text = val.ToString("G"),
                Width = 80,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            tb.TextChanged += (s, e) =>
            {
                context.MarkDirty();
                if (double.TryParse(tb.Text, out var v))
                    tb.Text = Math.Max(Min, Math.Min(Max, v)).ToString("G");
            };
            context.Registry.Register(SaveKey, tb);

            if (WithStepper)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var minusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "−", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 2, 0), Padding = new Thickness(0)
                };
                minusBtn.Click += (s, e) => { if (double.TryParse(tb.Text, out var v)) { v = Math.Max(Min, v - Step); tb.Text = v.ToString("G"); context.MarkDirty(); } };
                var plusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "+", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(0)
                };
                plusBtn.Click += (s, e) => { if (double.TryParse(tb.Text, out var v)) { v = Math.Min(Max, v + Step); tb.Text = v.ToString("G"); context.MarkDirty(); } };
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
                buttonPanel.Children.Add(minusBtn);
                buttonPanel.Children.Add(plusBtn);
                Grid.SetColumn(tb, 0);
                Grid.SetColumn(buttonPanel, 1);
                row.Children.Add(tb);
                row.Children.Add(buttonPanel);
                stack.Children.Add(row);
            }
            else
            {
                stack.Children.Add(tb);
            }

            panel.Children.Add(stack);
        }
    }
}
