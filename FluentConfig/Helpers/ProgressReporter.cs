using System;
using System.Windows;

namespace FluentConfig.Helpers
{
    internal class ProgressReporterImpl : IProgressReporter
    {
        private readonly Window _window;
        private readonly System.Windows.Controls.ProgressBar _progressBar;
        private readonly int _total;

        public ProgressReporterImpl(Window window, System.Windows.Controls.ProgressBar progressBar, int total)
        {
            _window = window;
            _progressBar = progressBar;
            _total = total;
        }

        public void Report(int current)
        {
            if (_window == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                if (_progressBar != null)
                    _progressBar.Value = Math.Min(current, _total);
            });
        }

        public void Close()
        {
            if (_window == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                try
                {
                    _window.Close();
                }
                catch (Exception) { }
            });
        }
    }
}
