using System;
using System.Threading;
using System.Windows;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    /// <summary>
    /// Centralizes STA thread, Dispatcher, window creation, and "is open" state.
    /// </summary>
    public class WindowLifecycleManager
    {
        private static Action<string> _logCallback;
        private static Action<double, double> _windowClosedCallback;
        private static bool _isOpen;
        private static Thread _uiThread;
        private static System.Windows.Threading.Dispatcher _dispatcher;
        private static readonly object _initLock = new object();
        private static FluentWindow _window;
        private static AutoResetEvent _windowReady;

        public static void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }

        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            _windowClosedCallback = callback;
        }

        /// <summary>
        /// Returns true if the UI is already open.
        /// </summary>
        public static bool IsOpen => _isOpen;

        /// <summary>
        /// Returns the current UI Dispatcher (null until InitializeWindow has been run).
        /// </summary>
        public static System.Windows.Threading.Dispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// Returns the current FluentWindow (null until initialized).
        /// </summary>
        public static FluentWindow Window => _window;

        public static void Log(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                System.Diagnostics.Debug.WriteLine($"[Sbui] {message}");
        }

        /// <summary>
        /// Runs createAndConfigure on the UI thread (starting STA if needed), then wires Closed and sets _isOpen.
        /// </summary>
        /// <param name="createAndConfigure">Invoked on UI thread; must create and configure the FluentWindow and return it.</param>
        /// <param name="windowReadyTimeoutMs">Timeout in ms for waiting for window ready (default 5000).</param>
        /// <returns>True if window was initialized within timeout.</returns>
        public static bool InitializeWindow(Func<FluentWindow> createAndConfigure, int windowReadyTimeoutMs = 5000)
        {
            _windowReady = new AutoResetEvent(false);
            _window = null;

            lock (_initLock)
            {
                if (_dispatcher != null)
                {
                    Log("Reusing existing UI thread and Application");
                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            _window = createAndConfigure();
                            if (_window != null)
                            {
                                _window.Closed += OnWindowClosed;
                                _isOpen = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"UI Thread ERROR (reuse): {ex.Message}");
                        }
                        _windowReady.Set();
                    });
                }
                else
                {
                    _uiThread = new Thread(() =>
                    {
                        try
                        {
                            Log("UI Thread: Starting STA thread");
                            if (Application.Current == null)
                            {
                                Log("UI Thread: Creating new Application");
                                var app = new Application();
                                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                            }
                            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                            Log("UI Thread: Initializing window");
                            _window = createAndConfigure();
                            if (_window != null)
                            {
                                _window.Closed += OnWindowClosed;
                                _isOpen = true;
                            }
                            Log("UI Thread: Window initialized");
                            _windowReady.Set();
                            System.Windows.Threading.Dispatcher.Run();
                        }
                        catch (Exception ex)
                        {
                            Log($"UI Thread ERROR: {ex.Message}");
                            Log($"Stack: {ex.StackTrace}");
                            _windowReady.Set();
                        }
                    });
                    _uiThread.SetApartmentState(ApartmentState.STA);
                    _uiThread.IsBackground = false;
                    _uiThread.Start();
                }
            }

            bool signaled = _windowReady.WaitOne(windowReadyTimeoutMs);
            if (!signaled)
                Log("WARNING: Window creation timed out");
            return signaled;
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            var w = _window;
            if (w != null && w.WindowState == WindowState.Normal && _windowClosedCallback != null)
                _windowClosedCallback(w.Width, w.Height);
            _isOpen = false;
            Log("Sbui UI has been closed.");
        }

        /// <summary>
        /// Returns true if the UI is already open and logs; use to skip opening a duplicate window.
        /// </summary>
        public static bool AlreadyOpened(string title = "Sbui", string version = "1.0")
        {
            if (!_isOpen)
                return false;
            Log($"UI ({title} (v{version})) already open, skipping...");
            return true;
        }
    }
}
