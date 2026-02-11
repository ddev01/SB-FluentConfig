using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Sbui.Elements;
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

        /// <summary>
        /// Updates a single control by key with the given value. Use for programmatic updates (e.g. SetValue).
        /// For pair combos, pass the full settings so the other key can be resolved.
        /// </summary>
        public void UpdateControl(DependencyObject root, string key, JToken value, JObject settings = null)
        {
            if (root == null || string.IsNullOrEmpty(key)) return;
            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    if (keyTb == key || (keyTb.StartsWith(SbuiTags.IntegerPrefix) && keyTb.Substring(SbuiTags.IntegerPrefix.Length) == key))
                    {
                        if (value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                            tb.Text = value.Value<int>().ToString();
                        else
                            tb.Text = value?.ToString() ?? "";
                        return;
                    }
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag is string keyPb && keyPb == key)
                {
                    pb.Password = value?.ToString() ?? "";
                    return;
                }
                else if (child is ToggleSwitch ts && ts.Tag is string keyTs && keyTs == key)
                {
                    ts.IsChecked = GetBoolValue(value);
                    return;
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag is string keySl && keySl == key)
                {
                    if (value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(value);
                    return;
                }
                else if (child is System.Windows.Controls.ComboBox combo && combo.Tag is string keyCombo)
                {
                    if (keyCombo == key)
                    {
                        SetComboBoxValue(combo, value);
                        return;
                    }
                    if (keyCombo.StartsWith(SbuiTags.PairPrefix))
                    {
                        var parts = keyCombo.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                        if (parts.Length == 2 && (parts[0] == key || parts[1] == key))
                        {
                            var displayVal = parts[0] == key ? value : (settings != null ? GetValue(settings, parts[0]) : null);
                            var valueVal = parts[1] == key ? value : (settings != null ? GetValue(settings, parts[1]) : null);
                            SetComboBoxPairValue(combo, displayVal, valueVal);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registry-based load: iterates registered controls instead of walking the visual tree. Faster when many controls.
        /// </summary>
        public void LoadSettingsIntoControls(ControlRegistry registry, JObject settings)
        {
            if (registry == null || settings == null) return;

            foreach (var kv in registry.GetAllRegistered())
            {
                var key = kv.Key;
                var control = kv.Value;

                if (control is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    if (keyTb.StartsWith(SbuiTags.IntegerPrefix))
                    {
                        var k = keyTb.Substring(SbuiTags.IntegerPrefix.Length);
                        if (k != key) continue;
                        var val = GetValue(settings, key);
                        if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                            tb.Text = val.Value<int>().ToString();
                    }
                    else
                    {
                        if (keyTb != key) continue;
                        var val = GetValue(settings, key);
                        tb.Text = val != null ? val.ToString() : "";
                    }
                }
                else if (control is System.Windows.Controls.PasswordBox pb)
                {
                    if (pb.Tag is string keyPb && keyPb != key) continue;
                    var val = GetValue(settings, key);
                    pb.Password = val != null ? val.ToString() : "";
                }
                else if (control is ToggleSwitch ts && ts.Tag is string keyTs && keyTs == key)
                {
                    var val = GetValue(settings, key);
                    ts.IsChecked = GetBoolValue(val);
                }
                else if (control is System.Windows.Controls.Slider sl && sl.Tag is string keySl && keySl == key)
                {
                    var val = GetValue(settings, key);
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(val);
                }
                else if (control is System.Windows.Controls.ComboBox combo && combo.Tag is string keyCombo)
                {
                    if (keyCombo.StartsWith(SbuiTags.PairPrefix))
                    {
                        var parts = keyCombo.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                        if (parts.Length == 2 && (parts[0] == key || parts[1] == key))
                            SetComboBoxPairValue(combo, GetValue(settings, parts[0]), GetValue(settings, parts[1]));
                    }
                    else if (keyCombo == key)
                    {
                        var val = GetValue(settings, key);
                        SetComboBoxValue(combo, val);
                    }
                }
                else if (control is StackPanel sp && sp.Tag is string tag)
                {
                    if (tag.StartsWith(SbuiTags.DynamicPrefix))
                    {
                        var k = tag.Substring(SbuiTags.DynamicPrefix.Length);
                        if (k != key) continue;
                        LoadDynamicTextboxes(sp, GetValue(settings, key));
                    }
                    else if (tag.StartsWith(SbuiTags.PillPrefix))
                    {
                        var k = tag.Substring(SbuiTags.PillPrefix.Length);
                        if (k != key) continue;
                        LoadPills(sp, GetValue(settings, key));
                    }
                    else if (tag.StartsWith(SbuiTags.DurationPrefix))
                    {
                        var k = tag.Substring(SbuiTags.DurationPrefix.Length);
                        if (k != key) continue;
                        ControlExtractionHelper.LoadDuration(sp, GetValue(settings, key)?.ToString() ?? "permanent");
                    }
                }
            }
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
                    if (keyCombo.StartsWith(SbuiTags.PairPrefix))
                    {
                        var parts = keyCombo.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                        if (parts.Length == 2)
                            SetComboBoxPairValue(combo, GetValue(settings, parts[0]), GetValue(settings, parts[1]));
                    }
                    else
                    {
                        var val = GetValue(settings, keyCombo);
                        SetComboBoxValue(combo, val);
                    }
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
                        ControlExtractionHelper.LoadDuration(sp, GetValue(settings, key)?.ToString() ?? "permanent");
                    }
                }
            }
        }

        /// <summary>
        /// Registry-based extract: iterates registered controls instead of walking the visual tree. Faster when many controls.
        /// </summary>
        public JObject ExtractSettingsFromControls(ControlRegistry registry)
        {
            if (registry == null) return new JObject();

            var settings = new JObject();
            var processedPairCombos = new HashSet<System.Windows.Controls.ComboBox>();

            foreach (var kv in registry.GetAllRegistered())
            {
                var key = kv.Key;
                var control = kv.Value;

                if (control is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    if (keyTb.StartsWith(SbuiTags.IntegerPrefix))
                    {
                        var k = keyTb.Substring(SbuiTags.IntegerPrefix.Length);
                        if (k != key) continue;
                        settings[key] = int.TryParse(tb.Text, out var v) ? v : 0;
                    }
                    else
                    {
                        if (keyTb != key) continue;
                        settings[key] = tb.Text ?? "";
                    }
                }
                else if (control is System.Windows.Controls.PasswordBox pb && pb.Tag is string keyPb && keyPb == key)
                {
                    settings[key] = pb.Password ?? "";
                }
                else if (control is ToggleSwitch ts && ts.Tag is string keyTs && keyTs == key)
                {
                    settings[key] = ts.IsChecked == true;
                }
                else if (control is System.Windows.Controls.Slider sl && sl.Tag is string keySl && keySl == key)
                {
                    settings[key] = (long)sl.Value;
                }
                else if (control is System.Windows.Controls.ComboBox cb && cb.Tag is string cbTag)
                {
                    if (cbTag.StartsWith(SbuiTags.PairPrefix))
                    {
                        if (processedPairCombos.Contains(cb)) continue;
                        processedPairCombos.Add(cb);
                        var parts = cbTag.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                        if (parts.Length == 2 && cb.SelectedItem is DropdownItem item)
                        {
                            settings[parts[0]] = item.Display ?? "";
                            settings[parts[1]] = item.Value ?? "";
                        }
                    }
                    else if (cbTag == key)
                    {
                        settings[key] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
                    }
                }
                else if (control is StackPanel sp && sp.Tag is string tag)
                {
                    if (tag.StartsWith(SbuiTags.DynamicPrefix))
                    {
                        var k = tag.Substring(SbuiTags.DynamicPrefix.Length);
                        if (k != key) continue;
                        var arr = ExtractDynamicTextboxes(sp);
                        if (arr != null)
                            settings[key] = arr;
                    }
                    else if (tag.StartsWith(SbuiTags.PillPrefix))
                    {
                        var k = tag.Substring(SbuiTags.PillPrefix.Length);
                        if (k != key) continue;
                        var arr = ExtractPills(sp);
                        if (arr != null)
                            settings[key] = arr;
                    }
                    else if (tag.StartsWith(SbuiTags.DurationPrefix))
                    {
                        var k = tag.Substring(SbuiTags.DurationPrefix.Length);
                        if (k != key) continue;
                        var val = ControlExtractionHelper.ExtractDuration(sp);
                        if (val != null)
                            settings[key] = val;
                    }
                }
            }
            return settings;
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
                else if (child is System.Windows.Controls.ComboBox cb && cb.Tag is string cbTag)
                {
                    if (cbTag.StartsWith(SbuiTags.PairPrefix))
                    {
                        var parts = cbTag.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                        if (parts.Length == 2 && cb.SelectedItem is DropdownItem item)
                        {
                            settings[parts[0]] = item.Display ?? "";
                            settings[parts[1]] = item.Value ?? "";
                        }
                    }
                    else
                        settings[cbTag] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
                }
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
                        var val = ControlExtractionHelper.ExtractDuration(sp);
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

        private static void SetComboBoxPairValue(System.Windows.Controls.ComboBox combo, JToken displayVal, JToken valueVal)
        {
            if (combo.ItemsSource == null) return;
            var valueStr = valueVal?.ToString();
            var displayStr = displayVal?.ToString();
            var idx = -1;
            var i = 0;
            foreach (var item in combo.ItemsSource)
            {
                if (item is DropdownItem di)
                {
                    if (!string.IsNullOrEmpty(valueStr) && string.Equals(di.Value, valueStr, StringComparison.OrdinalIgnoreCase))
                    { idx = i; break; }
                    if (!string.IsNullOrEmpty(displayStr) && string.Equals(di.Display, displayStr, StringComparison.OrdinalIgnoreCase))
                    { idx = i; break; }
                }
                i++;
            }
            if (idx >= 0)
                combo.SelectedIndex = Math.Min(idx, combo.Items.Count - 1);
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

        private static void LoadDynamicTextboxes(StackPanel sp, JToken token)
        {
            if (token is JArray arr)
            {
                var listPanel = ControlExtractionHelper.GetDynamicListPanel(sp);
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
            var listPanel = ControlExtractionHelper.GetDynamicListPanel(sp);
            if (listPanel == null) return null;
            var arr = new JArray();
            foreach (var c in listPanel.Children)
                if (c is System.Windows.Controls.TextBox t)
                    arr.Add(t.Text ?? "");
            return arr;
        }

        private static void LoadPills(StackPanel sp, JToken token)
        {
            var arr = token as JArray;
            if (arr == null) return;
            var pillsPanel = ControlExtractionHelper.GetPillsPanel(sp);
            if (pillsPanel == null) return;
            pillsPanel.Children.Clear();
            foreach (var item in arr)
            {
                var text = item?.ToString() ?? "";
                pillsPanel.Children.Add(ControlExtractionHelper.CreatePillBorder(text));
            }
        }

        private static JArray ExtractPills(StackPanel sp)
        {
            var pillsPanel = ControlExtractionHelper.GetPillsPanel(sp);
            if (pillsPanel == null) return null;
            var arr = new JArray();
            foreach (var c in pillsPanel.Children)
                if (c is Border b && b.Tag is string tag)
                    arr.Add(tag);
            return arr;
        }

    }
}
