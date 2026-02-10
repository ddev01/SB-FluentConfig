using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Sbui.Helpers;
using VisualTreeHelper = Sbui.Helpers.VisualTreeHelper;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    /// <summary>
    /// Synchronizes UI controls with settings (bi-directional: load from JObject into controls, extract from controls into JObject).
    /// </summary>
    public class SettingsSynchronizer
    {
        private static JToken GetValue(JObject settings, string key)
        {
            if (settings == null || string.IsNullOrEmpty(key)) return null;
            if (key.Contains("[") || key.Contains("."))
            {
                var token = settings.SelectToken(key);
                return token ?? settings[key];
            }
            return settings[key];
        }

        public void LoadSettingsIntoControls(DependencyObject root, JObject settings)
        {
            if (root == null || settings == null) return;

            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    if (keyTb.StartsWith(SbuiTags.IntegerPrefix))
                    {
                        var key = keyTb.Substring(SbuiTags.IntegerPrefix.Length);
                        var val = GetValue(settings, key);
                        if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                            tb.Text = val.Value<int>().ToString();
                    }
                    else
                    {
                        var val = GetValue(settings, keyTb);
                        tb.Text = val != null ? val.ToString() : "";
                    }
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag is string keyPb)
                {
                    var val = GetValue(settings, keyPb);
                    pb.Password = val != null ? val.ToString() : "";
                }
                else if (child is ToggleSwitch ts && ts.Tag is string keyTs)
                {
                    var val = GetValue(settings, keyTs);
                    ts.IsChecked = GetBoolValue(val);
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag is string keySl)
                {
                    var val = GetValue(settings, keySl);
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(val);
                }
                else if (child is System.Windows.Controls.ComboBox combo && combo.Tag is string keyCombo)
                {
                    var val = GetValue(settings, keyCombo);
                    SetComboBoxValue(combo, val);
                }
                else if (child is StackPanel sp && sp.Tag is string tag)
                {
                    if (tag.StartsWith(SbuiTags.DynamicPrefix))
                    {
                        var key = tag.Substring(SbuiTags.DynamicPrefix.Length);
                        LoadDynamicTextboxes(sp, GetValue(settings, key));
                    }
                    else if (tag.StartsWith(SbuiTags.PillPrefix))
                    {
                        var key = tag.Substring(SbuiTags.PillPrefix.Length);
                        LoadPills(sp, GetValue(settings, key));
                    }
                    else if (tag.StartsWith(SbuiTags.DurationPrefix))
                    {
                        var key = tag.Substring(SbuiTags.DurationPrefix.Length);
                        LoadDuration(sp, GetValue(settings, key));
                    }
                }
            }
        }

        public JObject ExtractSettingsFromControls(DependencyObject root)
        {
            if (root == null) return new JObject();

            var settings = new JObject();
            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    if (keyTb.StartsWith(SbuiTags.IntegerPrefix))
                    {
                        var key = keyTb.Substring(SbuiTags.IntegerPrefix.Length);
                        settings[key] = int.TryParse(tb.Text, out var v) ? v : 0;
                    }
                    else
                        settings[keyTb] = tb.Text ?? "";
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag != null)
                    settings[pb.Tag.ToString()] = pb.Password ?? "";
                else if (child is ToggleSwitch ts && ts.Tag != null)
                    settings[ts.Tag.ToString()] = ts.IsChecked == true;
                else if (child is System.Windows.Controls.Slider sl && sl.Tag != null)
                    settings[sl.Tag.ToString()] = (long)sl.Value;
                else if (child is System.Windows.Controls.ComboBox cb && cb.Tag != null)
                    settings[cb.Tag.ToString()] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
                else if (child is StackPanel sp && sp.Tag is string tag)
                {
                    if (tag.StartsWith(SbuiTags.DynamicPrefix))
                    {
                        var key = tag.Substring(SbuiTags.DynamicPrefix.Length);
                        var arr = ExtractDynamicTextboxes(sp);
                        if (arr != null)
                            settings[key] = arr;
                    }
                    else if (tag.StartsWith(SbuiTags.PillPrefix))
                    {
                        var key = tag.Substring(SbuiTags.PillPrefix.Length);
                        var arr = ExtractPills(sp);
                        if (arr != null)
                            settings[key] = arr;
                    }
                    else if (tag.StartsWith(SbuiTags.DurationPrefix))
                    {
                        var key = tag.Substring(SbuiTags.DurationPrefix.Length);
                        var val = ExtractDuration(sp);
                        if (val != null)
                            settings[key] = val;
                    }
                }
            }
            return settings;
        }

        private static bool GetBoolValue(JToken token)
        {
            if (token == null) return false;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            return token.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static double GetDoubleValue(JToken token)
        {
            if (token == null) return 0;
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.Value<double>();
            return 0;
        }

        private static void SetComboBoxValue(System.Windows.Controls.ComboBox combo, JToken val)
        {
            if (val == null) return;
            if (val.Type == JTokenType.Integer)
            {
                var maxIdx = combo.Items?.Count > 0 ? combo.Items.Count - 1 : 0;
                combo.SelectedIndex = Math.Max(0, Math.Min(val.Value<int>(), maxIdx));
            }
            else
            {
                var str = val.ToString();
                if (combo.Items != null)
                {
                    for (int i = 0; i < combo.Items.Count; i++)
                    {
                        if (string.Equals(combo.Items[i]?.ToString(), str, StringComparison.OrdinalIgnoreCase))
                        {
                            combo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            foreach (var c in outer.Children)
                if (c is StackPanel inner && inner.Children.OfType<System.Windows.Controls.TextBox>().Any())
                    return inner;
            return null;
        }

        private static void LoadDynamicTextboxes(StackPanel sp, JToken token)
        {
            if (token is JArray arr)
            {
                var listPanel = GetDynamicListPanel(sp);
                if (listPanel != null)
                {
                    var boxes = listPanel.Children.OfType<System.Windows.Controls.TextBox>().ToList();
                    for (int i = 0; i < boxes.Count && i < arr.Count; i++)
                        boxes[i].Text = arr[i]?.ToString() ?? "";
                }
            }
        }

        private static JArray ExtractDynamicTextboxes(StackPanel sp)
        {
            var listPanel = GetDynamicListPanel(sp);
            if (listPanel == null) return null;
            var arr = new JArray();
            foreach (var c in listPanel.Children)
                if (c is System.Windows.Controls.TextBox t)
                    arr.Add(t.Text ?? "");
            return arr;
        }

        private static WrapPanel GetPillsPanel(StackPanel outer)
        {
            foreach (var c in outer.Children)
                if (c is WrapPanel wp)
                    return wp;
            return null;
        }

        private static void LoadPills(StackPanel sp, JToken token)
        {
            var arr = token as JArray;
            if (arr == null) return;
            var pillsPanel = GetPillsPanel(sp);
            if (pillsPanel == null) return;
            pillsPanel.Children.Clear();
            foreach (var item in arr)
            {
                var text = item?.ToString() ?? "";
                var pillBorder = CreatePillBorder(text);
                pillsPanel.Children.Add(pillBorder);
            }
        }

        private static JArray ExtractPills(StackPanel sp)
        {
            var pillsPanel = GetPillsPanel(sp);
            if (pillsPanel == null) return null;
            var arr = new JArray();
            foreach (var c in pillsPanel.Children)
                if (c is Border b && b.Tag is string tag)
                    arr.Add(tag);
            return arr;
        }

        private static Border CreatePillBorder(string text)
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

        private static readonly string[] DurationUnits = { "s", "m", "h", "d", "w", "permanent" };

        private static void LoadDuration(Panel panel, JToken token)
        {
            var val = token?.ToString() ?? "permanent";
            var (num, unitIndex) = ParseDuration(val);
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

        private static string ExtractDuration(Panel panel)
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

        private static (int num, int unitIndex) ParseDuration(string value)
        {
            if (string.IsNullOrEmpty(value)) return (0, 5);
            value = value.Trim().ToLowerInvariant();
            if (value == "permanent") return (0, 5);
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
    }
}
