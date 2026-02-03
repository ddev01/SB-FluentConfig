using System;
using System.Windows;
using System.Windows.Media;

namespace Sbui.Helpers
{
    /// <summary>
    /// Toast, popup, and confirm dialog helpers; require a Window owner.
    /// </summary>
    public static class DialogHelper
    {
        public const int ToastDismissSeconds = 3;

        /// <summary>
        /// Shows a non-blocking, auto-dismissing toast notification.
        /// </summary>
        public static void Toast(Window owner, string message)
        {
            if (owner == null) return;
            owner.Dispatcher.Invoke(() =>
            {
                var toast = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 300,
                    Height = 80,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = owner,
                    Topmost = true
                };
                var border = new System.Windows.Controls.Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 0x2d, 0x35, 0x4a)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Child = new System.Windows.Controls.TextBlock
                    {
                        Text = message ?? "",
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        Foreground = Brushes.White
                    }
                };
                toast.Content = border;
                toast.Show();
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(ToastDismissSeconds) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    try { toast.Close(); }
                    catch (Exception) { }
                };
                timer.Start();
            });
        }

        /// <summary>
        /// Shows a modal info/success/error message with OK.
        /// </summary>
        public static void AddPopupWindow(Window owner, string title, string message)
        {
            if (owner == null) return;
            owner.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(owner, message ?? "", title ?? "", MessageBoxButton.OK);
            });
        }

        /// <summary>
        /// Shows a confirm dialog; returns MessageBoxResult (Yes/No).
        /// </summary>
        public static MessageBoxResult ShowConfirmDialog(Window owner, string title, string message, string yesButton, string noButton)
        {
            if (owner == null) return MessageBoxResult.None;
            MessageBoxResult result = MessageBoxResult.None;
            owner.Dispatcher.Invoke(() =>
            {
                result = MessageBox.Show(owner, message ?? "", title ?? "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            });
            return result;
        }
    }
}
