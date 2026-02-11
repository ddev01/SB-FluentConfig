using System;

namespace Sbui.Core
{
    /// <summary>
    /// Manages window-open state and close callback. Extracted from Sbui for clarity.
    /// </summary>
    public static class SbuiWindowManager
    {
        private static bool _anyWindowOpen;
        private static Action<double, double> _windowClosedCallback;

        /// <summary>
        /// Optional callback when window closes - receives (width, height) for persistence (e.g. via CPH.SetGlobalVar).
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            _windowClosedCallback = callback;
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new Sbui to avoid duplicate windows.
        /// </summary>
        public static bool AlreadyOpened(string title, string version, Action<string> log)
        {
            if (!_anyWindowOpen) return false;
            log?.Invoke($"UI ({title} (v{version})) already open, skipping...");
            return true;
        }

        public static bool IsOpen => _anyWindowOpen;

        internal static void SetOpened(bool opened) => _anyWindowOpen = opened;

        internal static void InvokeWindowClosedCallback(double width, double height)
        {
            _windowClosedCallback?.Invoke(width, height);
        }
    }
}
