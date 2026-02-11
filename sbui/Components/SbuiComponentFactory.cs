using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Sbui.Components
{
    /// <summary>
    /// Creates WPF controls (ToggleSwitch, TextBox, Slider, etc.) with consistent styling and tagging; no persistence or registry.
    /// </summary>
    public static class SbuiComponentFactory
    {
        public static ToggleSwitch CreateToggleSwitch(string saveKey, bool isChecked)
        {
            var ts = new ToggleSwitch { Tag = saveKey, IsChecked = isChecked };
            return ts;
        }

        public static System.Windows.Controls.TextBox CreateTextBox(string saveKey, string initialText)
        {
            return new System.Windows.Controls.TextBox
            {
                Tag = saveKey,
                Text = initialText ?? "",
                MinWidth = 200,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 0, 0)
            };
        }

        public static System.Windows.Controls.PasswordBox CreatePasswordBox(string saveKey, string initialPassword)
        {
            return new System.Windows.Controls.PasswordBox
            {
                Tag = saveKey,
                Password = initialPassword ?? "",
                MinWidth = 200,
                Height = 28,
                Margin = new Thickness(0, 4, 0, 0)
            };
        }

        public static System.Windows.Controls.Slider CreateSlider(string saveKey, int min, int max, int value)
        {
            return new System.Windows.Controls.Slider
            {
                Tag = saveKey,
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, value)),
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static System.Windows.Controls.TextBox CreateSliderValueBox(int value)
        {
            return new System.Windows.Controls.TextBox
            {
                Text = value.ToString(),
                Width = 60,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        public static System.Windows.Controls.TextBox CreateResponseBox(string saveKey, string initialText)
        {
            return new System.Windows.Controls.TextBox
            {
                Tag = saveKey,
                Text = initialText ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinWidth = 200,
                MinHeight = 80,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        public static System.Windows.Controls.ComboBox CreateComboBox(string saveKey, string[] options, int selectedIndex)
        {
            options = options ?? Array.Empty<string>();
            selectedIndex = Math.Max(0, Math.Min(selectedIndex, options.Length > 0 ? options.Length - 1 : 0));
            return new System.Windows.Controls.ComboBox
            {
                Tag = saveKey,
                ItemsSource = options,
                SelectedIndex = selectedIndex,
                MinWidth = 200,
                MinHeight = 30,
                Height = 30,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Creates a ComboBox without Tag, for use inside composite controls (e.g. DurationInput). Same styling as CreateComboBox.
        /// </summary>
        public static System.Windows.Controls.ComboBox CreateComboBoxForEmbedded(string[] options, int selectedIndex)
        {
            options = options ?? Array.Empty<string>();
            selectedIndex = Math.Max(0, Math.Min(selectedIndex, options.Length > 0 ? options.Length - 1 : 0));
            return new System.Windows.Controls.ComboBox
            {
                ItemsSource = options,
                SelectedIndex = selectedIndex,
                MinWidth = 90,
                MinHeight = 30,
                Height = 30,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static System.Windows.Controls.TextBox CreateFilepathTextBox(string saveKey, string initialPath)
        {
            return new System.Windows.Controls.TextBox
            {
                Tag = saveKey,
                Text = initialPath ?? "",
                MinWidth = 200,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        public static System.Windows.Controls.TextBox CreateColorPickerTextBox(string saveKey, string initialColor)
        {
            return new System.Windows.Controls.TextBox
            {
                Tag = saveKey,
                Text = initialColor ?? "#000000",
                MinWidth = 120,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Creates a vertical StackPanel with a title and optional description, using the standard section margin.
        /// Use this as the root container in element Render methods.
        /// </summary>
        public static StackPanel CreateTitledStack(string title, string description)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(CreateTitleTextBlock(title));
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(CreateDescriptionTextBlock(description));
            return stack;
        }

        public static System.Windows.Controls.TextBlock CreateTitleTextBlock(string text)
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = text ?? "",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            };
        }

        public static System.Windows.Controls.TextBlock CreateDescriptionTextBlock(string text)
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = text ?? "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 4)
            };
        }

        public static Border CreateInlineSeparator()
        {
            return new Border
            {
                Height = 1,
                Margin = new Thickness(0, 16, 0, 16),
                Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x45, 0x55)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }
    }
}
