using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Sbui.Helpers;

namespace Sbui.Elements
{
    public class SliderElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int DefaultValue { get; set; }

        public SliderElement(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue, string visibilityKey = null)
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
            var val = context.GetSetting(SaveKey) != null ? context.GetSetting(SaveKey).ToObject<int>() : DefaultValue;
            val = Math.Max(Min, Math.Min(Max, val));
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var valueBox = SbuiComponentFactory.CreateSliderValueBox(val);
            InputValidation.CreatePreviewTextInputHandler(valueBox, InputValidation.InputType.Int);
            InputValidation.AddDataObjectPastingHandler(valueBox, InputValidation.InputType.Int);
            var slider = SbuiComponentFactory.CreateSlider(SaveKey, Min, Max, val);
            context.Registry.Register(SaveKey, slider);
            Grid.SetColumn(valueBox, 0);
            Grid.SetColumn(slider, 1);
            grid.Children.Add(valueBox);
            grid.Children.Add(slider);
            slider.ValueChanged += (s, e) =>
            {
                context.MarkDirty();
                if (!valueBox.IsFocused)
                    valueBox.Text = ((int)slider.Value).ToString();
            };
            valueBox.TextChanged += (s, e) =>
            {
                context.MarkDirty();
                if (InputValidation.TryParseInt(valueBox.Text, out var v, Min, Max))
                    slider.Value = v;
            };
            valueBox.LostFocus += (s, e) =>
            {
                if (!InputValidation.TryParseInt(valueBox.Text, out var v, Min, Max))
                    valueBox.Text = ((int)slider.Value).ToString();
            };
            stack.Children.Add(grid);
            panel.Children.Add(stack);
        }
    }
}
