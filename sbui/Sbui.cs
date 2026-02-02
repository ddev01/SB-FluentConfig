using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace Sbui
{
    public class Sbui
    {
        private FluentWindow _window;
        private static Action<string> _logCallback;
        private static Thread _uiThread;
        private static System.Windows.Threading.Dispatcher _dispatcher;
        private static readonly object _initLock = new object();
        private AutoResetEvent _windowReady;

        public static void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }

        private void Log(string message)
        {
            if (_logCallback != null)
            {
                _logCallback(message);
            }
            else
            {
                Debug.WriteLine($"[Sbui] {message}");
            }
        }

        public Sbui()
        {
            try
            {
                Log("Sbui constructor called");
                Log($"Application.Current is null: {Application.Current == null}");
                Log($"Current thread apartment state: {Thread.CurrentThread.GetApartmentState()}");

                lock (_initLock)
                {
                    if (_dispatcher != null)
                    {
                        // Reuse existing Application/Dispatcher - create window on UI thread
                        Log("Reusing existing UI thread and Application");
                        _windowReady = new AutoResetEvent(false);
                        _dispatcher.Invoke(() =>
                        {
                            try
                            {
                                InitializeWindow();
                                _windowReady.Set();
                            }
                            catch (Exception ex)
                            {
                                Log($"UI Thread ERROR (reuse): {ex.Message}");
                                _windowReady.Set();
                            }
                        });
                    }
                    else
                    {
                        // First run - create STA thread and Application
                        _windowReady = new AutoResetEvent(false);
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
                                InitializeWindow();
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

                // Wait for window to be created (with timeout)
                if (!_windowReady.WaitOne(5000))
                {
                    Log("WARNING: Window creation timed out");
                }

                Log("Constructor completed");
            }
            catch (Exception ex)
            {
                Log($"ERROR in constructor: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void InitializeWindow()
        {
            try
            {
                Log("InitializeWindow: Starting window creation");
                Log($"InitializeWindow: Thread apartment state: {Thread.CurrentThread.GetApartmentState()}");

                // Create the FluentWindow - matching original TawmaeUI pattern
                _window = new FluentWindow
                {
                    Title = "Settings UI Test",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                Log("InitializeWindow: FluentWindow created");

                // Apply WPF-UI theme - matching original pattern
                ApplicationThemeManager.Apply((ApplicationTheme)1, (WindowBackdropType)2, true);
                Log("InitializeWindow: Theme applied (first call)");
                ApplicationThemeManager.Apply((FrameworkElement)_window);
                Log("InitializeWindow: Theme applied (second call)");

                // Create main content
                var mainPanel = new StackPanel
                {
                    Margin = new Thickness(20),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Add title text
                var titleText = new System.Windows.Controls.TextBlock
                {
                    Text = "Settings UI Test Window",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Add description text
                var descriptionText = new System.Windows.Controls.TextBlock
                {
                    Text = "This is a simple test window using WPF-UI.\nIf you can see this, the UI is working!",
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                // Add status text
                var statusText = new System.Windows.Controls.TextBlock
                {
                    Text = "Status: Ready",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Green),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Add all elements to panel
                mainPanel.Children.Add(titleText);
                mainPanel.Children.Add(descriptionText);
                mainPanel.Children.Add(statusText);
                Log("InitializeWindow: Content elements added to panel");

                // Set window content
                _window.Content = mainPanel;
                Log("InitializeWindow: Window content set");

                // Handle window closed - do NOT shutdown dispatcher so we can reopen later
                _window.Closed += (s, e) =>
                {
                    Log("Window closed event fired");
                };

                Log($"InitializeWindow: Window.IsLoaded = {_window.IsLoaded}");
                Log($"InitializeWindow: Window.Visibility = {_window.Visibility}");
                Log($"InitializeWindow: Window.IsVisible = {_window.IsVisible}");
            }
            catch (Exception ex)
            {
                Log($"ERROR in InitializeWindow: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void ShowUI()
        {
            try
            {
                Log("ShowUI: Called");

                if (_window == null)
                {
                    Log("ShowUI: ERROR - _window is null!");
                    return;
                }

                // Show window on the UI thread
                _window.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Log($"ShowUI: Window is not null, Title = {_window.Title}");
                        Log($"ShowUI: Window.Visibility before = {_window.Visibility}");
                        Log($"ShowUI: Window.IsLoaded = {_window.IsLoaded}");

                        // Ensure window shows normally (not minimized) and is activated
                        _window.WindowState = WindowState.Normal;
                        ((Window)_window).Show();
                        _window.Activate();
                        Log("ShowUI: Show() called");

                        Log($"ShowUI: Window.Visibility after = {_window.Visibility}");
                        Log($"ShowUI: Window.IsVisible = {_window.IsVisible}");
                        Log($"ShowUI: Window.IsLoaded = {_window.IsLoaded}");
                    }
                    catch (Exception ex)
                    {
                        Log($"ERROR in ShowUI (on UI thread): {ex.Message}");
                        Log($"Stack: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"ERROR in ShowUI: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
