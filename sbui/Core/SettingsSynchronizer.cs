using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Sbui.Helpers;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    /// <summary>
    /// Synchronizes UI controls with settings (bi-directional: load from JObject into controls, extract from controls into JObject).
    /// </summary>
    public class SettingsSynchronizer
    {
        public void LoadSettingsIntoControls(DependencyObject root, JObject settings)
        {
            if (root == null || settings == null) return;

            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    var val = settings[keyTb];
                    tb.Text = val != null ? val.ToString() : "";
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag is string keyPb)
                {
                    var val = settings[keyPb];
                    pb.Password = val != null ? val.ToString() : "";
                }
                else if (child is ToggleSwitch ts && ts.Tag is string keyTs)
                {
                    var val = settings[keyTs];
                    ts.IsChecked = GetBoolValue(val);
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag is string keySl)
                {
                    var val = settings[keySl];
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(val);
                }
                else if (child is System.Windows.Controls.ComboBox combo && combo.Tag is string keyCombo)
                {
                    var val = settings[keyCombo];
                    SetComboBoxValue(combo, val);
                }
                else if (child is StackPanel sp && sp.Tag is string tag && tag.StartsWith("dynamic:"))
                {
                    var key = tag.Substring(8);
                    LoadDynamicTextboxes(sp, settings[key]);
                }
            }
        }

        public JObject ExtractSettingsFromControls(DependencyObject root)
        {
            if (root == null) return new JObject();

            var settings = new JObject();
            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag != null)
                    settings[tb.Tag.ToString()] = tb.Text ?? "";
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag != null)
                    settings[pb.Tag.ToString()] = pb.Password ?? "";
                else if (child is ToggleSwitch ts && ts.Tag != null)
                    settings[ts.Tag.ToString()] = ts.IsChecked == true;
                else if (child is System.Windows.Controls.Slider sl && sl.Tag != null)
                    settings[sl.Tag.ToString()] = (long)sl.Value;
                else if (child is System.Windows.Controls.ComboBox cb && cb.Tag != null)
                    settings[cb.Tag.ToString()] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
                else if (child is StackPanel sp && sp.Tag is string tag && tag.StartsWith("dynamic:"))
                {
                    var key = tag.Substring(8);
                    var arr = ExtractDynamicTextboxes(sp);
                    if (arr != null)
                        settings[key] = arr;
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
    }
}
