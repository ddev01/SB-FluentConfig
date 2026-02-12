using System;
using System.Windows.Controls;

namespace FluentConfig
{
    /// <summary>
    /// Context passed to button OnClick callbacks. Provides Pending (live control values), Toast, Popup.
    /// </summary>
    public class UiContext
    {
        private readonly FluentConfig _config;

        internal UiContext(FluentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Gets the current value of a control by saveKey (e.g. in button callbacks).</summary>
        public T Pending<T>(string key)
        {
            return _config.GetPendingValue<T>(key);
        }

        /// <summary>Shows a non-blocking toast notification.</summary>
        public void Toast(string message)
        {
            _config.Toast(message);
        }

        /// <summary>Shows a modal popup with title and message.</summary>
        public void Popup(string title, string message)
        {
            _config.AddPopupWindow(title, message);
        }

        /// <summary>Shows a confirm dialog; returns Yes/No result. Call from UI thread or via Dispatcher.</summary>
        public System.Windows.MessageBoxResult ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            return _config.ShowConfirmDialog(title, message, yesButton, noButton);
        }

        /// <summary>Shows a progress window; returns an object with Report(int) and Close().</summary>
        public IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            return _config.ShowProgressWindow(title, message, progressLabel, total);
        }

        /// <summary>Logs a message to the configured log callback.</summary>
        public void Log(string message)
        {
            _config.Log(message);
        }
    }
}
