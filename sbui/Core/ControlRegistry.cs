using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                {
                    if (typeof(T) == typeof(double) && double.TryParse(tb.Text, out var dVal))
                        return (T)(object)dVal;
                    return (T)(object)(tb.Text ?? "");
                }
                if (control is System.Windows.Controls.PasswordBox pb)
                    return (T)(object)(pb.Password ?? "");
                if (control is ToggleSwitch ts)
                    return (T)(object)(ts.IsChecked == true);
                if (control is System.Windows.Controls.Slider sl)
                    return (T)Convert.ChangeType(sl.Value, typeof(T));
                if (control is System.Windows.Controls.ComboBox cb)
                {
                    if (typeof(T) == typeof(int))
                        return (T)(object)cb.SelectedIndex;
                    return (T)(object)(cb.SelectedItem?.ToString() ?? "");
                }
                if (control is StackPanel stack && stack.Tag is string dynTag && dynTag.StartsWith("dynamic:"))
                {
                    if (typeof(T) == typeof(string[]))
                    {
                        var listPanel = GetDynamicListPanel(stack);
                        if (listPanel != null)
                        {
                            var list = new List<string>();
                            foreach (var c in listPanel.Children)
                                if (c is System.Windows.Controls.TextBox tx)
                                    list.Add(tx.Text ?? "");
                            return (T)(object)list.ToArray();
                        }
                    }
                }
            }
            catch (Exception) { }
            return default;
        }

        private static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            if (outer == null) return null;
            foreach (var c in outer.Children)
                if (c is StackPanel inner && inner.Children.OfType<System.Windows.Controls.TextBox>().Any())
                    return inner;
            return null;
        }

        internal bool TryGetValue(string key, out FrameworkElement control)
        {
            return _controls.TryGetValue(key, out control);
        }
    }
}
