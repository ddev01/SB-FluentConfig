using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FluentConfig.Helpers
{
    /// <summary>
    /// Shared helpers for extracting values from and loading values into composite controls (dynamic textboxes, pills, duration).
    /// Used by ControlRegistry and SettingsSynchronizer.
    /// </summary>
    public static class ControlExtractionHelper
    {
        public static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            if (outer == null) return null;
            foreach (var c in outer.Children)
            {
                if (c is StackPanel inner)
                {
                    foreach (var ch in inner.Children)
                    {
                        if (ch is System.Windows.Controls.TextBox)
                            return inner;
                        if (ch is Grid g && g.Children.OfType<System.Windows.Controls.TextBox>().Any())
                            return inner;
                    }
                }
            }
            return null;
        }

        public static WrapPanel GetPillsPanel(StackPanel outer)
        {
            if (outer == null) return null;
            foreach (var c in outer.Children)
                if (c is WrapPanel wp)
                    return wp;
            return null;
        }

        public static (int num, int unitIndex) ParseDuration(string value)
        {
            if (string.IsNullOrEmpty(value)) return (0, 5);
            value = value.Trim().ToLowerInvariant();
            if (value == "permanent") return (0, 5);

            var fullUnits = new[] { "seconds", "minutes", "hours", "days", "weeks" };
            for (int u = 0; u < fullUnits.Length; u++)
            {
                if (value.EndsWith(fullUnits[u]))
                {
                    var numStr = value.Substring(0, value.Length - fullUnits[u].Length).Trim();
                    int.TryParse(numStr, out var num);
                    return (num, u);
                }
            }

            var unitChars = "smhdw";
            for (int i = value.Length - 1; i >= 0; i--)
            {
                var c = value[i];
                if (char.IsDigit(c) || c == ' ') continue;
                if (unitChars.IndexOf(c) >= 0)
                {
                    var numStr = value.Substring(0, i).Trim();
                    int.TryParse(numStr, out var num);
                    int unitIndex;
                    switch (c) { case 's': unitIndex = 0; break; case 'm': unitIndex = 1; break; case 'h': unitIndex = 2; break; case 'd': unitIndex = 3; break; case 'w': unitIndex = 4; break; default: unitIndex = 5; break; }
                    return (num, unitIndex);
                }
                break;
            }
            int.TryParse(value, out var n);
            return (n, 1);
        }

        public static string ExtractDuration(Panel panel)
        {
            var tb = VisualTreeHelper.DescendantsOnly(panel).OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            var combo = VisualTreeHelper.DescendantsOnly(panel).OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
            if (tb == null || combo == null) return null;
            var idx = combo.SelectedIndex;
            if (idx < 0 || combo.Items == null || idx >= combo.Items.Count) return "permanent";
            var unit = combo.Items[idx]?.ToString() ?? "permanent";
            if (unit == "permanent") return "permanent";
            var num = int.TryParse(tb.Text, out var n) ? n : 0;
            return $"{num}{unit}";
        }

        public static void LoadDuration(Panel panel, string value)
        {
            var (num, unitIndex) = ParseDuration(value);
            var tb = VisualTreeHelper.DescendantsOnly(panel).OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            var combo = VisualTreeHelper.DescendantsOnly(panel).OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
            if (tb != null) tb.Text = num.ToString();
            if (combo != null)
            {
                var idx = Math.Max(0, Math.Min(unitIndex, combo.Items?.Count - 1 ?? 0));
                combo.SelectedIndex = idx;
                var isPerm = idx >= 0 && idx < (combo.Items?.Count ?? 0) && combo.Items[idx]?.ToString() == "permanent";
                tb.IsEnabled = !isPerm;
            }
        }

        public static Border CreatePillBorder(string text)
        {
            var pillBorder = new Border
            {
                Tag = text,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 4, 4, 4),
                Margin = new Thickness(0, 0, 6, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            var pillRow = new StackPanel { Orientation = Orientation.Horizontal };
            pillRow.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            var removeBtn = new Wpf.Ui.Controls.Button
            {
                Content = "×",
                Width = 22,
                Height = 22,
                FontSize = 14,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Click += (s, e) =>
            {
                if (pillBorder.Parent is Panel parent)
                    parent.Children.Remove(pillBorder);
            };
            pillRow.Children.Add(removeBtn);
            pillBorder.Child = pillRow;
            return pillBorder;
        }
    }
}
