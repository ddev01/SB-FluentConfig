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

        // ── Update single control ──────────────────────────────────────────

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
                    var realKey = SbuiTags.StripPrefix(keyTb);
                    if (realKey == key)
                    {
                        UpdateTextBox(tb, keyTb, value);
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

        private static void UpdateTextBox(System.Windows.Controls.TextBox tb, string tag, JToken value)
        {
            if (tag.StartsWith(SbuiTags.DoublePrefix))
            {
                if (value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                    tb.Text = InputValidation.FormatDouble(value.Value<double>());
                else
                    tb.Text = value?.ToString() ?? "";
            }
            else if (tag.StartsWith(SbuiTags.FloatPrefix))
            {
                if (value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                    tb.Text = InputValidation.FormatFloat(value.Value<float>());
                else
                    tb.Text = value?.ToString() ?? "";
            }
            else if (tag.StartsWith(SbuiTags.ColorPrefix))
            {
                var val = value?.ToString() ?? "";
                tb.Text = InputValidation.IsValidHexColor(val) ? val : "#000000";
            }
            else
            {
                // Plain text or integer-prefixed
                if (value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                    tb.Text = value.Value<int>().ToString();
                else
                    tb.Text = value?.ToString() ?? "";
            }
        }

        // ── Load settings into controls ────────────────────────────────────

        /// <summary>
        /// Registry-based load: iterates registered controls instead of walking the visual tree.
        /// </summary>
        public void LoadSettingsIntoControls(ControlRegistry registry, JObject settings)
        {
            if (registry == null || settings == null) return;

            foreach (var kv in registry.GetAllRegistered())
            {
                var registryKey = kv.Key;
                var control = kv.Value;

                if (control is System.Windows.Controls.TextBox tb && tb.Tag is string tbTag)
                {
                    var realKey = SbuiTags.StripPrefix(tbTag);
                    if (realKey != registryKey) continue;
                    LoadTextBox(tb, tbTag, realKey, settings);
                }
                else if (control is System.Windows.Controls.PasswordBox pb)
                {
                    if (pb.Tag is string pbTag && pbTag != registryKey) continue;
                    pb.Password = GetValue(settings, registryKey)?.ToString() ?? "";
                }
                else if (control is ToggleSwitch ts && ts.Tag is string tsTag && tsTag == registryKey)
                {
                    ts.IsChecked = GetBoolValue(GetValue(settings, registryKey));
                }
                else if (control is System.Windows.Controls.Slider sl && sl.Tag is string slTag && slTag == registryKey)
                {
                    var val = GetValue(settings, registryKey);
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(val);
                }
                else if (control is System.Windows.Controls.ComboBox combo && combo.Tag is string comboTag)
                {
                    LoadComboBox(combo, comboTag, registryKey, settings);
                }
                else if (control is StackPanel sp && sp.Tag is string spTag)
                {
                    var realKey = SbuiTags.StripPrefix(spTag);
                    if (realKey != registryKey) continue;
                    LoadStackPanel(sp, spTag, realKey, settings);
                }
            }
        }

        /// <summary>
        /// Visual-tree-based load: walks all descendants of root.
        /// </summary>
        public void LoadSettingsIntoControls(DependencyObject root, JObject settings)
        {
            if (root == null || settings == null) return;

            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string tbTag)
                {
                    if (tbTag.StartsWith(SbuiTags.CompetingPrefix)) continue; // handled by parent StackPanel
                    var key = SbuiTags.StripPrefix(tbTag);
                    if (key != null) LoadTextBox(tb, tbTag, key, settings);
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag is string pbTag)
                {
                    pb.Password = GetValue(settings, pbTag)?.ToString() ?? "";
                }
                else if (child is ToggleSwitch ts && ts.Tag is string tsTag)
                {
                    ts.IsChecked = GetBoolValue(GetValue(settings, tsTag));
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag is string slTag)
                {
                    var val = GetValue(settings, slTag);
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = GetDoubleValue(val);
                }
                else if (child is System.Windows.Controls.ComboBox combo && combo.Tag is string comboTag)
                {
                    LoadComboBox(combo, comboTag, comboTag, settings);
                }
                else if (child is StackPanel sp && sp.Tag is string spTag)
                {
                    var key = SbuiTags.StripPrefix(spTag);
                    if (key != null) LoadStackPanel(sp, spTag, key, settings);
                }
            }
        }

        // ── Extract settings from controls ─────────────────────────────────

        /// <summary>
        /// Registry-based extract: iterates registered controls instead of walking the visual tree.
        /// </summary>
        public JObject ExtractSettingsFromControls(ControlRegistry registry)
        {
            if (registry == null) return new JObject();

            var settings = new JObject();
            var processedPairCombos = new HashSet<System.Windows.Controls.ComboBox>();

            foreach (var kv in registry.GetAllRegistered())
            {
                var registryKey = kv.Key;
                var control = kv.Value;

                if (control is System.Windows.Controls.TextBox tb && tb.Tag is string tbTag)
                {
                    var realKey = SbuiTags.StripPrefix(tbTag);
                    if (realKey != registryKey) continue;
                    ExtractTextBox(tb, tbTag, realKey, settings);
                }
                else if (control is System.Windows.Controls.PasswordBox pb && pb.Tag is string pbTag && pbTag == registryKey)
                {
                    settings[registryKey] = pb.Password ?? "";
                }
                else if (control is ToggleSwitch ts && ts.Tag is string tsTag && tsTag == registryKey)
                {
                    settings[registryKey] = ts.IsChecked == true;
                }
                else if (control is System.Windows.Controls.Slider sl && sl.Tag is string slTag && slTag == registryKey)
                {
                    settings[registryKey] = (long)sl.Value;
                }
                else if (control is System.Windows.Controls.ComboBox cb && cb.Tag is string cbTag)
                {
                    ExtractComboBox(cb, cbTag, registryKey, settings, processedPairCombos);
                }
                else if (control is StackPanel sp && sp.Tag is string spTag)
                {
                    var realKey = SbuiTags.StripPrefix(spTag);
                    if (realKey != registryKey) continue;
                    ExtractStackPanel(sp, spTag, realKey, settings);
                }
            }
            return settings;
        }

        /// <summary>
        /// Visual-tree-based extract: walks all descendants of root.
        /// </summary>
        public JObject ExtractSettingsFromControls(DependencyObject root)
        {
            if (root == null) return new JObject();

            var settings = new JObject();
            var processedPairCombos = new HashSet<System.Windows.Controls.ComboBox>();

            foreach (var child in VisualTreeHelper.Descendants(root))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string tbTag)
                {
                    // CompetingPrefix TextBoxes store a JSON array directly
                    if (tbTag.StartsWith(SbuiTags.CompetingPrefix))
                    {
                        ExtractCompetingTextBox(tb, tbTag.Substring(SbuiTags.CompetingPrefix.Length), settings);
                        continue;
                    }
                    var key = SbuiTags.StripPrefix(tbTag);
                    if (key != null) ExtractTextBox(tb, tbTag, key, settings);
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag != null)
                {
                    settings[pb.Tag.ToString()] = pb.Password ?? "";
                }
                else if (child is ToggleSwitch ts && ts.Tag != null)
                {
                    settings[ts.Tag.ToString()] = ts.IsChecked == true;
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag != null)
                {
                    settings[sl.Tag.ToString()] = (long)sl.Value;
                }
                else if (child is System.Windows.Controls.ComboBox cb && cb.Tag is string cbTag)
                {
                    ExtractComboBox(cb, cbTag, cbTag, settings, processedPairCombos);
                }
                else if (child is StackPanel sp && sp.Tag is string spTag)
                {
                    var key = SbuiTags.StripPrefix(spTag);
                    if (key != null) ExtractStackPanel(sp, spTag, key, settings);
                }
            }
            return settings;
        }

        // ── Shared dispatch: load ──────────────────────────────────────────

        private void LoadTextBox(System.Windows.Controls.TextBox tb, string tag, string key, JObject settings)
        {
            var val = GetValue(settings, key);
            if (tag.StartsWith(SbuiTags.IntegerPrefix))
            {
                if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                    tb.Text = val.Value<int>().ToString();
            }
            else if (tag.StartsWith(SbuiTags.DoublePrefix))
            {
                if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                    tb.Text = InputValidation.FormatDouble(val.Value<double>());
                else
                    tb.Text = val != null ? val.ToString() : "";
            }
            else if (tag.StartsWith(SbuiTags.FloatPrefix))
            {
                if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                    tb.Text = InputValidation.FormatFloat(val.Value<float>());
                else
                    tb.Text = val != null ? val.ToString() : "";
            }
            else if (tag.StartsWith(SbuiTags.ColorPrefix))
            {
                var v = val?.ToString() ?? "";
                tb.Text = InputValidation.IsValidHexColor(v) ? v : "#000000";
            }
            else
            {
                tb.Text = val != null ? val.ToString() : "";
            }
        }

        private void LoadComboBox(System.Windows.Controls.ComboBox combo, string tag, string key, JObject settings)
        {
            if (tag.StartsWith(SbuiTags.PairPrefix))
            {
                var parts = tag.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                if (parts.Length == 2)
                    SetComboBoxPairValue(combo, GetValue(settings, parts[0]), GetValue(settings, parts[1]));
            }
            else if (tag == key)
            {
                SetComboBoxValue(combo, GetValue(settings, key));
            }
        }

        private void LoadStackPanel(StackPanel sp, string tag, string key, JObject settings)
        {
            if (tag.StartsWith(SbuiTags.DynamicPrefix))
                LoadDynamicTextboxes(sp, GetValue(settings, key));
            else if (tag.StartsWith(SbuiTags.PillPrefix))
                LoadPills(sp, GetValue(settings, key));
            else if (tag.StartsWith(SbuiTags.DurationPrefix))
                ControlExtractionHelper.LoadDuration(sp, GetValue(settings, key)?.ToString() ?? "permanent");
            else if (tag.StartsWith(SbuiTags.CompetingPrefix))
                LoadCompetingIndices(sp, GetValue(settings, key));
        }

        // ── Shared dispatch: extract ───────────────────────────────────────

        private static void ExtractTextBox(System.Windows.Controls.TextBox tb, string tag, string key, JObject settings)
        {
            if (tag.StartsWith(SbuiTags.IntegerPrefix))
                settings[key] = InputValidation.TryParseInt(tb.Text, out var iv, int.MinValue, int.MaxValue) ? iv : 0;
            else if (tag.StartsWith(SbuiTags.DoublePrefix))
                settings[key] = InputValidation.TryParseDouble(tb.Text, out var dv, double.MinValue, double.MaxValue) ? dv : 0.0;
            else if (tag.StartsWith(SbuiTags.FloatPrefix))
                settings[key] = InputValidation.TryParseFloat(tb.Text, out var fv, float.MinValue, float.MaxValue) ? fv : 0f;
            else if (tag.StartsWith(SbuiTags.ColorPrefix))
                settings[key] = InputValidation.IsValidHexColor(tb.Text ?? "") ? (tb.Text ?? "#000000") : "#000000";
            else
                settings[key] = tb.Text ?? "";
        }

        private static void ExtractComboBox(System.Windows.Controls.ComboBox cb, string tag, string key, JObject settings,
            HashSet<System.Windows.Controls.ComboBox> processedPairCombos)
        {
            if (tag.StartsWith(SbuiTags.PairPrefix))
            {
                if (processedPairCombos.Contains(cb)) return;
                processedPairCombos.Add(cb);
                var parts = tag.Substring(SbuiTags.PairPrefix.Length).Split(new[] { ',' }, 2);
                if (parts.Length == 2 && cb.SelectedItem is DropdownItem item)
                {
                    settings[parts[0]] = item.Display ?? "";
                    settings[parts[1]] = item.Value ?? "";
                }
            }
            else
            {
                settings[key] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
            }
        }

        private static void ExtractStackPanel(StackPanel sp, string tag, string key, JObject settings)
        {
            JToken value = null;
            if (tag.StartsWith(SbuiTags.DynamicPrefix))
                value = ExtractDynamicTextboxes(sp);
            else if (tag.StartsWith(SbuiTags.PillPrefix))
                value = ExtractPills(sp);
            else if (tag.StartsWith(SbuiTags.DurationPrefix))
                value = ControlExtractionHelper.ExtractDuration(sp);
            else if (tag.StartsWith(SbuiTags.CompetingPrefix))
                value = ExtractCompetingIndices(sp);

            if (value != null)
                settings[key] = value;
        }

        /// <summary>
        /// Tree-walk only: CompetingPrefix TextBoxes store a JSON array directly in their Text.
        /// </summary>
        private static void ExtractCompetingTextBox(System.Windows.Controls.TextBox tb, string key, JObject settings)
        {
            try
            {
                var parsed = JToken.Parse(tb.Text ?? "[]");
                settings[key] = parsed is JArray a ? a : new JArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sbui] SettingsSynchronizer: failed to parse competing prefix for key '{key}': {ex.Message}");
                settings[key] = new JArray();
            }
        }

        // ── Value helpers ──────────────────────────────────────────────────

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

        // ── Dynamic / Pill / Competing helpers ─────────────────────────────

        private static void LoadDynamicTextboxes(StackPanel sp, JToken token)
        {
            if (token is JArray arr)
            {
                var listPanel = ControlExtractionHelper.GetDynamicListPanel(sp);
                if (listPanel != null)
                {
                    var boxes = GetDynamicTextboxList(listPanel);
                    for (int i = 0; i < boxes.Count && i < arr.Count; i++)
                        boxes[i].Text = arr[i]?.ToString() ?? "";
                }
            }
        }

        private static List<System.Windows.Controls.TextBox> GetDynamicTextboxList(StackPanel listPanel)
        {
            var list = new List<System.Windows.Controls.TextBox>();
            foreach (var c in listPanel.Children)
            {
                if (c is System.Windows.Controls.TextBox t)
                    list.Add(t);
                else if (c is Grid g)
                {
                    var tb = g.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                    if (tb != null) list.Add(tb);
                }
            }
            return list;
        }

        private static JArray ExtractDynamicTextboxes(StackPanel sp)
        {
            var listPanel = ControlExtractionHelper.GetDynamicListPanel(sp);
            if (listPanel == null) return null;
            var arr = new JArray();
            foreach (var c in listPanel.Children)
            {
                if (c is System.Windows.Controls.TextBox t)
                    arr.Add(t.Text ?? "");
                else if (c is Grid g)
                {
                    var tb = g.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                    if (tb != null) arr.Add(tb.Text ?? "");
                }
            }
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

        private static JArray ExtractCompetingIndices(StackPanel sp)
        {
            var tb = sp.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            if (tb == null || string.IsNullOrEmpty(tb.Text)) return new JArray();
            try
            {
                var parsed = JToken.Parse(tb.Text);
                return parsed as JArray ?? new JArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sbui] SettingsSynchronizer.ExtractCompetingIndices: failed to parse: {ex.Message}");
                return new JArray();
            }
        }

        private static void LoadCompetingIndices(StackPanel sp, JToken token)
        {
            var tb = sp.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            if (tb == null) return;
            var indices = new List<int>();
            if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item != null && (item.Type == JTokenType.Integer || item.Type == JTokenType.Float))
                        indices.Add(item.Value<int>());
                }
            }
            tb.Text = "[" + string.Join(",", indices) + "]";
            if (sp.Tag is string tag && tag.StartsWith(SbuiTags.CompetingPrefix))
            {
                var saveKey = tag.Substring(SbuiTags.CompetingPrefix.Length);
                var prefix = saveKey + "_opt_";
                if (sp.Parent is StackPanel parent)
                {
                    foreach (var child in parent.Children)
                    {
                        if (child is ToggleSwitch ts && ts.Tag is string t && t.StartsWith(prefix))
                        {
                            if (int.TryParse(t.Substring(prefix.Length), out var i))
                                ts.IsChecked = indices.Contains(i);
                        }
                    }
                }
            }
        }
    }
}
