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
        /// <summary>
        /// Focus an existing window for <paramref name="title"/>, or reserve the title slot for a new one.
        /// Returns false when an existing window was focused (caller should not Create/Show).
        /// Returns true when the title was reserved (caller must Register the real window, or Unregister on failure).
        /// </summary>
        internal static bool TryBeginOpen(string title, string version, Action<string> log)
        {
            lock (LockObj)
            {
                if (!string.IsNullOrEmpty(title) && OpenWindows.TryGetValue(title, out var window) && window != null)
                {
                    log?.Invoke($"UI ({title} (v{version})) already open, focusing...");
                    try
                    {
                        FocusWindow(window);
                        return false;
                    }
                    catch
                    {
                        OpenWindows.Remove(title);
                        log?.Invoke($"UI ({title}) focus failed; clearing stale window entry.");
                    }
                }

                if (!string.IsNullOrEmpty(title))
                    OpenWindows[title] = null; // placeholder reservation
                return true;
            }
        }

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
                    return true;
                }
                catch
                {
                    // Stale / disposed window — drop registry entry so caller can open a fresh one.
                    OpenWindows.Remove(title);
                    log?.Invoke($"UI ({title}) focus failed; clearing stale window entry.");
                    return false;
                }
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
