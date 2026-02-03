using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Wpf.Ui.Controls;

namespace Sbui.Elements
{
    public class SliderWithToggleSwitchElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int DefaultValue { get; set; }
        public bool ToggleDefault { get; set; }

        public SliderWithToggleSwitchElement(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue, bool toggleDefault, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
            ToggleDefault = toggleDefault;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            string enabledKey = SaveKey + "_enabled";
            bool enabled = context.Settings?[enabledKey] != null ? context.Settings[enabledKey].ToObject<bool>() : ToggleDefault;
            int val = context.Settings?[SaveKey] != null ? context.Settings[SaveKey].ToObject<int>() : DefaultValue;
            val = Math.Max(Min, Math.Min(Max, val));
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var valueBox = SbuiComponentFactory.CreateSliderValueBox(val);
            var slider = SbuiComponentFactory.CreateSlider(SaveKey, Min, Max, val);
            slider.IsEnabled = enabled;
            context.Registry.Register(SaveKey, slider);
            var ts = SbuiComponentFactory.CreateToggleSwitch(enabledKey, enabled);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(valueBox, 0);
            Grid.SetColumn(slider, 1);
            grid.Children.Add(valueBox);
            grid.Children.Add(slider);
            grid.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            ts.Checked += (s, e) => { context.MarkDirty(); grid.Visibility = Visibility.Visible; slider.IsEnabled = true; };
            ts.Unchecked += (s, e) => { context.MarkDirty(); grid.Visibility = Visibility.Collapsed; slider.IsEnabled = false; };
            context.Registry.Register(enabledKey, ts);
            stack.Children.Add(ts);
            slider.ValueChanged += (s, e) => { context.MarkDirty(); if (!valueBox.IsFocused) valueBox.Text = ((int)slider.Value).ToString(); };
            valueBox.TextChanged += (s, e) => { context.MarkDirty(); if (int.TryParse(valueBox.Text, out var v)) slider.Value = Math.Max(Min, Math.Min(Max, v)); };
            stack.Children.Add(grid);
            panel.Children.Add(stack);
        }
    }
}
