using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FluentConfig.Core;
using FluentConfig.Helpers;
using Wpf.Ui.Controls;

namespace FluentConfig.Helpers
{
    /// <summary>
    /// Attaches change handlers to controls in the registry so a single Action runs whenever any dependent control changes.
    /// Used for predicate-based visibility to re-evaluate when dependencies change.
    /// </summary>
    internal static class ControlChangeNotifier
    {
        /// <summary>
        /// Subscribes to change events on all controls matching the given save keys.
        /// When any of them changes, onChanged is invoked.
        /// Skips keys with no registered control (e.g. deferred tab).
        /// </summary>
        public static void Subscribe(ControlRegistry registry, string[] dependencyKeys, Action onChanged)
        {
            if (registry == null || dependencyKeys == null || dependencyKeys.Length == 0 || onChanged == null)
                return;

            foreach (var key in dependencyKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (!registry.TryGetValue(key, out var control)) continue;
                AttachToControl(control, onChanged);
            }
        }

        private static void AttachToControl(FrameworkElement control, Action onChanged)
        {
            if (control is ToggleSwitch ts)
            {
                ts.Checked += OnChanged;
                ts.Unchecked += OnChanged;
                return;
            }
            if (control is System.Windows.Controls.ComboBox cb)
            {
                cb.SelectionChanged += OnSelectionChanged;
                return;
            }
            if (control is System.Windows.Controls.TextBox tb)
            {
                tb.TextChanged += OnChanged;
                return;
            }
            if (control is System.Windows.Controls.Slider sl)
            {
                sl.ValueChanged += OnValueChanged;
                return;
            }
            if (control is System.Windows.Controls.PasswordBox pb)
            {
                pb.PasswordChanged += OnChanged;
                return;
            }
            if (control is StackPanel stack)
            {
                AttachToStackPanelChildren(stack, onChanged);
                return;
            }

            void OnChanged(object s, EventArgs e) => onChanged();
            void OnSelectionChanged(object s, SelectionChangedEventArgs e) => onChanged();
            void OnValueChanged(object s, RoutedPropertyChangedEventArgs<double> e) => onChanged();
        }

        private static void AttachToStackPanelChildren(Panel panel, Action onChanged)
        {
            foreach (var child in VisualTreeHelper.DescendantsOnly(panel))
            {
                if (child is System.Windows.Controls.TextBox tx)
                {
                    tx.TextChanged += (s, e) => onChanged();
                    continue;
                }
                if (child is System.Windows.Controls.ComboBox combo)
                {
                    combo.SelectionChanged += (s, e) => onChanged();
                    continue;
                }
                if (child is System.Windows.Controls.Slider slider)
                {
                    slider.ValueChanged += (s, e) => onChanged();
                    continue;
                }
            }
        }
    }
}
