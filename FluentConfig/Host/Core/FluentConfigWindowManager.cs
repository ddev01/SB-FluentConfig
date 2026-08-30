using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FluentConfig.Core
{
    /// <summary>
    /// Per-menu-id window registry and close callback. Thread-safe.
    /// Different ids may be open simultaneously; re-triggering the same id focuses the existing window.
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
        /// Focus an existing window for <paramref name="id"/>, or reserve the id slot for a new one.
        /// A placeholder (reserved, window still null) counts as reserved so a second opener
        /// during <c>build()</c> cannot create a second session.
        /// Returns false when an existing window was focused or the id is already reserved.
        /// Returns true when the id was reserved (caller must Register the real window, or Unregister on failure).
        /// </summary>
        internal static bool TryBeginOpen(string id, string version, Action<string> log)
        {
            lock (LockObj)
            {
                if (string.IsNullOrEmpty(id))
                    return true;

                if (OpenWindows.TryGetValue(id, out var window))
                {
                    if (window == null)
                    {
                        log?.Invoke($"UI ({id} (v{version})) already opening");
                        return false;
                    }

                    log?.Invoke($"UI ({id} (v{version})) already open, focusing...");
                    try
                    {
                        FocusWindow(window);
                        return false;
                    }
                    catch
                    {
                        OpenWindows.Remove(id);
                        log?.Invoke($"UI ({id}) focus failed; clearing stale window entry.");
                    }
                }

                OpenWindows[id] = null;
                return true;
            }
        }

        public static bool AlreadyOpened(string id, string version, Action<string> log)
        {
            lock (LockObj)
            {
                if (string.IsNullOrEmpty(id) || !OpenWindows.TryGetValue(id, out var window) || window == null)
                    return false;

                log?.Invoke($"UI ({id} (v{version})) already open, focusing...");
                try
                {
                    FocusWindow(window);
                    return true;
                }
                catch
                {
                    // Stale / disposed window — drop registry entry so caller can open a fresh one.
                    OpenWindows.Remove(id);
                    log?.Invoke($"UI ({id}) focus failed; clearing stale window entry.");
                    return false;
                }
            }
        }

        public static bool IsOpen
        {
            get { lock (LockObj) { return OpenWindows.Count > 0; } }
        }

        internal static void Register(string id, Window window)
        {
            if (string.IsNullOrEmpty(id) || window == null) return;
            lock (LockObj)
            {
                OpenWindows[id] = window;
            }
        }

        internal static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (LockObj)
            {
                OpenWindows.Remove(id);
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
