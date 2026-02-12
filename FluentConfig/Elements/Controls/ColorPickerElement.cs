using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ColorPicker;
using FluentConfig.Components;
using FluentConfig.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace FluentConfig.Elements
{
    public class ColorPickerElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string DefaultColor { get; set; }

        public ColorPickerElement(string title, string description, string tabName, string saveKey, string defaultColor, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            DefaultColor = defaultColor ?? "#000000";
            VisibilityKey = visibilityKey;
        }

        private static string ToColorHex(byte r, byte g, byte b, byte a)
        {
            if (a == 255) return $"#{r:X2}{g:X2}{b:X2}";
            return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var initial = context.GetSetting(SaveKey)?.ToString() ?? DefaultColor ?? "#000000";
            var stack = FluentConfigComponentFactory.CreateTitledStack(Title, Description);
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var preview = new Rectangle
            {
                Width = 32,
                Height = 28,
                Margin = new System.Windows.Thickness(0, 4, 0, 0),
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            try
            {
                preview.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(initial));
            }
            catch
            {
                preview.Fill = Brushes.Gray;
            }
            var tb = FluentConfigComponentFactory.CreateColorPickerTextBox(SaveKey, initial);
            tb.Tag = FluentConfigTags.ColorPrefix + SaveKey;
            preview.MouseDown += (s, e) =>
            {
                Color initialColor;
                try
                {
                    initialColor = (Color)ColorConverter.ConvertFromString((tb.Text ?? "").Trim());
                }
                catch
                {
                    initialColor = Colors.Black;
                }
                var colorPickerRd = new ResourceDictionary();
                try
                {
                    colorPickerRd.Source = new Uri("pack://application:,,,/ColorPicker;component/Styles/DefaultColorPickerStyle.xaml", UriKind.RelativeOrAbsolute);
                }
                catch (Exception ex)
                {
                    context.Log($"ColorPickerElement: failed to load ColorPicker resource dictionary: {ex.Message}");
                }
                var standardColorPicker = new StandardColorPicker
                {
                    SelectedColor = initialColor,
                    ShowAlpha = true,
                    Margin = new System.Windows.Thickness(0, 0, 0, 12)
                };
                if (colorPickerRd["DefaultColorPickerStyle"] is Style darkStyle)
                    standardColorPicker.Style = darkStyle;
                var okBtn = new Button { Content = "OK", Width = 72, Height = 32, Margin = new System.Windows.Thickness(0, 0, 8, 0) };
                var undoBtn = new Button { Content = "Undo", Width = 72, Height = 32 };
                var btnRow = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
                btnRow.Children.Add(okBtn);
                btnRow.Children.Add(undoBtn);
                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(standardColorPicker, 0);
                Grid.SetRow(btnRow, 1);
                mainGrid.Children.Add(standardColorPicker);
                mainGrid.Children.Add(btnRow);
                var picker = new Window
                {
                    Title = "Pick color",
                    Width = 400,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(preview),
                    Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
                    ResizeMode = ResizeMode.NoResize,
                    Content = new Border { Padding = new System.Windows.Thickness(14), Child = mainGrid }
                };
                picker.Resources.MergedDictionaries.Add(colorPickerRd);
                bool applyOnClose = false;
                okBtn.Click += (_, __) => { applyOnClose = true; picker.Close(); };
                undoBtn.Click += (_, __) => picker.Close();
                picker.Closed += (_, __) =>
                {
                    if (applyOnClose)
                    {
                        var c = standardColorPicker.SelectedColor;
                        tb.Text = ToColorHex(c.R, c.G, c.B, c.A);
                        preview.Fill = new SolidColorBrush(c);
                        context.MarkDirty();
                    }
                };
                picker.ShowDialog();
            };
            tb.TextChanged += (s, e) =>
            {
                context.MarkDirty();
                if (InputValidation.IsValidHexColor(tb.Text))
                {
                    try
                    {
                        var c = (Color)ColorConverter.ConvertFromString(tb.Text);
                        preview.Fill = new SolidColorBrush(c);
                        tb.BorderBrush = null;
                        tb.BorderThickness = new Thickness(0);
                    }
                    catch { preview.Fill = Brushes.Gray; }
                }
                else
                {
                    preview.Fill = Brushes.Gray;
                    tb.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 80, 80));
                    tb.BorderThickness = new Thickness(1);
                }
            };
            context.Registry.Register(SaveKey, tb);
            Grid.SetColumn(tb, 0);
            Grid.SetColumn(preview, 1);
            row.Children.Add(tb);
            row.Children.Add(preview);
            stack.Children.Add(row);
            panel.Children.Add(stack);
        }
    }
}
