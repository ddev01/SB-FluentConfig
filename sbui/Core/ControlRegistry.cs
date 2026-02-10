using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Sbui.Helpers;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    /// <summary>
    /// Type-safe registration and lookup of controls by saveKey.
    /// </summary>
    public class ControlRegistry
    {
        private readonly Dictionary<string, FrameworkElement> _controls = new Dictionary<string, FrameworkElement>();

        public void Register<T>(string key, T control) where T : FrameworkElement
        {
            if (string.IsNullOrEmpty(key) || control == null) return;
            _controls[key] = control;
        }

        public T Get<T>(string key) where T : FrameworkElement
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_controls.TryGetValue(key, out var c) && c is T t)
                return t;
            return null;
        }

        public bool TryGetValue<T>(string key, out T control) where T : FrameworkElement
        {
            control = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (_controls.TryGetValue(key, out var c) && c is T t)
            {
                control = t;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _controls.Clear();
        }

        /// <summary>
        /// Gets the current value from a control by saveKey (for GetPendingValue). Must be called from UI thread or via Dispatcher.
        /// </summary>
        public T GetValue<T>(string key)
        {
            if (!TryGetValue(key, out var control))
                return default;
            try
            {
                if (control is System.Windows.Controls.TextBox tb)
                    return (T)(object)GetValueFromTextBox(tb, typeof(T));
                if (control is System.Windows.Controls.PasswordBox pb)
                    return (T)(object)(pb.Password ?? "");
                if (control is ToggleSwitch ts)
                    return (T)(object)(ts.IsChecked == true);
                if (control is System.Windows.Controls.Slider sl)
                    return (T)Convert.ChangeType(sl.Value, typeof(T));
                if (control is System.Windows.Controls.ComboBox cb)
                    return (T)(object)GetValueFromComboBox(cb, typeof(T));
                if (control is StackPanel stack && stack.Tag is string tag)
                {
                    var stackVal = GetValueFromTaggedStackPanel(stack, tag, typeof(T));
                    if (stackVal != null)
                        return (T)stackVal;
                }
            }
            catch (Exception) { }
            return default;
        }

        private static object GetValueFromTextBox(System.Windows.Controls.TextBox tb, Type targetType)
        {
            if (targetType == typeof(double) && double.TryParse(tb.Text, out var dVal))
                return dVal;
            if (targetType == typeof(int) && int.TryParse(tb.Text, out var iVal))
                return iVal;
            return tb.Text ?? "";
        }

        private static object GetValueFromComboBox(System.Windows.Controls.ComboBox cb, Type targetType)
        {
            if (targetType == typeof(int))
                return cb.SelectedIndex;
            return cb.SelectedItem?.ToString() ?? "";
        }

        private static object GetValueFromTaggedStackPanel(StackPanel stack, string tag, Type targetType)
        {
            if (tag.StartsWith(SbuiTags.DynamicPrefix) && targetType == typeof(string[]))
            {
                var listPanel = GetDynamicListPanel(stack);
                if (listPanel != null)
                {
                    var list = new List<string>();
                    foreach (var c in listPanel.Children)
                        if (c is System.Windows.Controls.TextBox tx)
                            list.Add(tx.Text ?? "");
                    return list.ToArray();
                }
            }
            if (tag.StartsWith(SbuiTags.PillPrefix) && targetType == typeof(string[]))
            {
                var pillsPanel = GetPillsPanel(stack);
                if (pillsPanel != null)
                {
                    var list = new List<string>();
                    foreach (var c in pillsPanel.Children)
                        if (c is System.Windows.Controls.Border b && b.Tag is string t)
                            list.Add(t);
                    return list.ToArray();
                }
            }
            if (tag.StartsWith(SbuiTags.DurationPrefix) && targetType == typeof(string))
            {
                var numBox = VisualTreeHelper.DescendantsOnly(stack).OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                var combo = VisualTreeHelper.DescendantsOnly(stack).OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
                if (numBox != null && combo != null)
                {
                    var idx = combo.SelectedIndex;
                    if (idx >= 0 && combo.Items != null && idx < combo.Items.Count)
                    {
                        var unit = combo.Items[idx]?.ToString() ?? "permanent";
                        if (unit == "permanent") return "permanent";
                        var num = int.TryParse(numBox.Text, out var n) ? n : 0;
                        return $"{num}{unit}";
                    }
                }
            }
            return null;
        }

        private static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            if (outer == null) return null;
            foreach (var c in outer.Children)
                if (c is StackPanel inner && inner.Children.OfType<System.Windows.Controls.TextBox>().Any())
                    return inner;
            return null;
        }

        private static WrapPanel GetPillsPanel(StackPanel outer)
        {
            if (outer == null) return null;
            foreach (var c in outer.Children)
                if (c is WrapPanel wp)
                    return wp;
            return null;
        }

        internal bool TryGetValue(string key, out FrameworkElement control)
        {
            return _controls.TryGetValue(key, out control);
        }
    }
}
