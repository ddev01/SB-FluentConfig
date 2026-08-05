using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FluentConfig.Core
{
    /// <summary>
    /// Per-title window registry and close callback. Thread-safe.
    /// Different titles may be open simultaneously; re-triggering the same title focuses the existing window.
    /// </summary>
    public static class FluentConfigWindowManager
    {
        private static readonly object LockObj = new object();
        // Stored as Window to avoid FluentConfig.FluentConfig static-class name clash with the namespace.
        private static readonly Dictionary<string, Window> OpenWindows =
            new Dictionary<string, Window>(StringComparer.Ordinal);
        private static Action<double, double> _windowClosedCallback;
        private static Action<double, double, double, double> _windowClosedGeometryCallback;

        /// <summary>
        /// Optional callback when window closes — receives (width, height) for persistence.
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            lock (LockObj) { _windowClosedCallback = callback; }
        }

        /// <summary>
        /// Optional callback when window closes — receives (left, top, width, height).
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double, double, double> callback)
        {
            lock (LockObj) { _windowClosedGeometryCallback = callback; }
        }

        /// <summary>
        /// Returns true if a window with this title is already open (and focuses it).
        /// Call before creating a new window.
        /// </summary>
        public static bool AlreadyOpened(string title, string version, Action<string> log)
        {
            lock (LockObj)
            {
                if (string.IsNullOrEmpty(title) || !OpenWindows.TryGetValue(title, out var window) || window == null)
                    return false;

                log?.Invoke($"UI ({title} (v{version})) already open, focusing...");
                try
                {
                    FocusWindow(window);
                }
                catch
                {
                    // Focus is best-effort; still report already-open so callers skip Create.
                }
                return true;
            }
        }

        public static bool IsOpen
        {
            get { lock (LockObj) { return OpenWindows.Count > 0; } }
        }

        internal static void Register(string title, Window window)
        {
            if (string.IsNullOrEmpty(title) || window == null) return;
            lock (LockObj)
            {
                OpenWindows[title] = window;
            }
        }

        internal static void Unregister(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            lock (LockObj)
            {
                OpenWindows.Remove(title);
            }
        }

        /// <summary>
        /// Snapshot and clear all registered windows (host-exit teardown).
        /// </summary>
        internal static Window[] TakeAllWindows()
        {
            lock (LockObj)
            {
                var windows = OpenWindows.Values.Where(w => w != null).ToArray();
                OpenWindows.Clear();
                return windows;
            }
        }

        /// <summary>Snapshot of currently open windows without clearing.</summary>
        internal static Window[] SnapshotWindows()
        {
            lock (LockObj)
            {
                return OpenWindows.Values.Where(w => w != null).ToArray();
            }
        }

        internal static void InvokeWindowClosedCallback(double left, double top, double width, double height)
        {
            Action<double, double> sizeCb;
            Action<double, double, double, double> geomCb;
            lock (LockObj)
            {
                sizeCb = _windowClosedCallback;
                geomCb = _windowClosedGeometryCallback;
            }

            sizeCb?.Invoke(width, height);
            geomCb?.Invoke(left, top, width, height);
        }

        private static void FocusWindow(Window window)
        {
            void Do()
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                window.Activate();
            }

            if (window.Dispatcher.CheckAccess())
                Do();
            else
                window.Dispatcher.Invoke(Do);
        }
    }
}
