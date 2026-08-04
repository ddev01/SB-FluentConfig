using System;

namespace FluentConfig.Core
{
    /// <summary>
    /// Manages window-open state and close callback. Thread-safe.
    /// </summary>
    public static class FluentConfigWindowManager
    {
        private static readonly object LockObj = new object();
        private static bool _anyWindowOpen;
        private static Action<double, double> _windowClosedCallback;

        /// <summary>
        /// Optional callback when window closes — receives (width, height) for persistence.
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            lock (LockObj) { _windowClosedCallback = callback; }
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new window.
        /// </summary>
        public static bool AlreadyOpened(string title, string version, Action<string> log)
        {
            lock (LockObj)
            {
                if (!_anyWindowOpen) return false;
                log?.Invoke($"UI ({title} (v{version})) already open, skipping...");
                return true;
            }
        }

        public static bool IsOpen
        {
            get { lock (LockObj) { return _anyWindowOpen; } }
        }

        internal static void SetOpened(bool opened)
        {
            lock (LockObj) { _anyWindowOpen = opened; }
        }

        internal static void InvokeWindowClosedCallback(double width, double height)
        {
            Action<double, double> cb;
            lock (LockObj) { cb = _windowClosedCallback; }
            cb?.Invoke(width, height);
        }
    }
}
