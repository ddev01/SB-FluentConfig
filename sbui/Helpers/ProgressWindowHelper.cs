using System;
using System.Windows;
using System.Windows.Controls;

namespace Sbui.Helpers
{
    /// <summary>
    /// Creates a progress window and returns IProgressReporter. Used by Sbui.ShowProgressWindow.
    /// </summary>
    public static class ProgressWindowHelper
    {
        /// <summary>
        /// Shows a progress window owned by the given window. Returns an IProgressReporter with Report(int) and Close().
        /// </summary>
        public static global::Sbui.IProgressReporter Show(Window owner, string title, string message, string progressLabel, int total)
        {
            if (owner == null) return null;
            var progressWindow = new Window
            {
                Title = title ?? "Progress",
                Width = 400,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner
            };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = message ?? "", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
            stack.Children.Add(new TextBlock { Text = progressLabel ?? "Progress", Margin = new Thickness(0, 0, 0, 4) });
            var progressBar = new ProgressBar { Minimum = 0, Maximum = Math.Max(1, total), Value = 0, Height = 24 };
            stack.Children.Add(progressBar);
            progressWindow.Content = stack;
            progressWindow.Show();
            return new ProgressReporterImpl(progressWindow, progressBar, total);
        }
    }
}
