using System;

namespace Sbui.Core
{
    /// <summary>
    /// Manages window-open state and close callback. Extracted from Sbui for clarity.
    /// Thread-safe: all static state is guarded by a lock.
    /// </summary>
    public static class SbuiWindowManager
    {
        private static readonly object _lock = new object();
        private static bool _anyWindowOpen;
        private static Action<double, double> _windowClosedCallback;

        /// <summary>
        /// Optional callback when window closes - receives (width, height) for persistence (e.g. via CPH.SetGlobalVar).
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            lock (_lock) { _windowClosedCallback = callback; }
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new Sbui to avoid duplicate windows.
        /// </summary>
        public static bool AlreadyOpened(string title, string version, Action<string> log)
        {
            lock (_lock)
            {
                if (!_anyWindowOpen) return false;
                log?.Invoke($"UI ({title} (v{version})) already open, skipping...");
                return true;
            }
        }

        public static bool IsOpen
        {
            get { lock (_lock) { return _anyWindowOpen; } }
        }

        internal static void SetOpened(bool opened)
        {
            lock (_lock) { _anyWindowOpen = opened; }
        }

        internal static void InvokeWindowClosedCallback(double width, double height)
        {
            Action<double, double> cb;
            lock (_lock) { cb = _windowClosedCallback; }
            cb?.Invoke(width, height);
        }
    }
}
