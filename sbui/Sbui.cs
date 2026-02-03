using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using ColorPicker;

namespace Sbui
{
    public class Sbui
    {
        private FluentWindow _window;
        private readonly IInlineInvokeProxy _cph;
        private readonly string _extensionName;
        private readonly string _displayVersion;
        private readonly string _settingsKey;
        private JObject _existingSettings;
        private readonly bool _withUi;
        private Dictionary<string, StackPanel> _tabContentPanels;
        private Grid _mainGrid;
        private StackPanel _tabContainer;
        private System.Windows.Controls.ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private readonly Dictionary<string, double> _tabScrollOffsets = new Dictionary<string, double>();
        private readonly List<PendingItem> _pendingItems;
        private bool _dirty;
        private Dictionary<string, FrameworkElement> _controlRegistry = new Dictionary<string, FrameworkElement>();
        private Dictionary<string, StackPanel> _dynamicTextboxPanels = new Dictionary<string, StackPanel>();

        private enum PendingKind
        {
            Header, Title, Description, ToggleSwitch, Textbox, Slider,
            InlineSeparator, SliderWithToggleSwitch, RefreshableDropdown, Filepath, ClickableButton,
            ResponseBox, DecimalStepper, CompetingToggleSwitches, DynamicTextboxesWithPreset, ColorPicker
        }
        private class PendingItem
        {
            public PendingKind Kind;
            public object[] Args;
            public string VisibilityKey;
            public PendingItem(PendingKind kind, object[] args) { Kind = kind; Args = args; }
        }
        private string _currentVisibilityKey;
        private StackPanel _visibilityOverridePanel;
        private static Action<string> _logCallback;
        private static Action<double, double> _windowClosedCallback;
        private static bool _isOpen;
        private static Thread _uiThread;
        private static System.Windows.Threading.Dispatcher _dispatcher;
        private static readonly object _initLock = new object();
        private AutoResetEvent _windowReady;

        public static void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }

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
        public static bool AlreadyOpened(string title = "Sbui", string version = "1.0")
        {
            if (!_isOpen)
                return false;
            LogStatic($"UI ({title} (v{version})) already open, skipping...");
            return true;
        }

        public static bool IsOpen => _isOpen;

        private static void LogStatic(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                Debug.WriteLine($"[Sbui] {message}");
        }

        private void LogInternal(string message)
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

        /// <summary>
        /// Logs a message to the configured log callback or Debug output.
        /// </summary>
        public void Log(string message)
        {
            LogInternal(message);
        }

        /// <summary>
        /// Shows a non-blocking, auto-dismissing notification (toast).
        /// </summary>
        public void Toast(string message)
        {
            if (_window == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                var toast = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 300,
                    Height = 80,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = _window,
                    Topmost = true
                };
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 0x2d, 0x35, 0x4a)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Child = new System.Windows.Controls.TextBlock { Text = message ?? "", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White }
                };
                toast.Content = border;
                toast.Show();
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, e) => { timer.Stop(); try { toast.Close(); } catch { } };
                timer.Start();
            });
        }

        /// <summary>
        /// Parameterless constructor for backward compatibility. No CPH persistence.
        /// </summary>
        public Sbui() : this(null, "", null, true)
        {
        }

        /// <summary>
        /// Constructor with CPH for loading/saving settings. Use extensionName for storage key Sbui_Settings_{extensionName}.
        /// </summary>
        public Sbui(IInlineInvokeProxy cph, string extensionName, bool withUi = true) : this(cph, extensionName, null, withUi)
        {
        }

        /// <summary>
        /// Constructor with CPH and optional display version for window title (e.g. "{extensionName} (v{displayVersion})").
        /// </summary>
        public Sbui(IInlineInvokeProxy cph, string extensionName, string displayVersion, bool withUi = true)
        {
            _cph = cph;
            _extensionName = extensionName ?? "Settings";
            _displayVersion = string.IsNullOrEmpty(displayVersion) ? null : displayVersion;
            _settingsKey = cph != null ? "Sbui_Settings_" + extensionName : null;
            _withUi = withUi;
            _existingSettings = new JObject();
            _tabContentPanels = new Dictionary<string, StackPanel>();
            _pendingItems = new List<PendingItem>();

            if (_cph != null)
                LoadSettings();

            try
            {
                Log("Sbui constructor called");
                Log($"Application.Current is null: {Application.Current == null}");
                Log($"Current thread apartment state: {Thread.CurrentThread.GetApartmentState()}");

                if (!_withUi)
                {
                    Log("Constructor completed (no UI)");
                    return;
                }

                lock (_initLock)
                {
                    if (_dispatcher != null)
                    {
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

                if (!_windowReady.WaitOne(5000))
                    Log("WARNING: Window creation timed out");

                Log("Constructor completed");
            }
            catch (Exception ex)
            {
                Log($"ERROR in constructor: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Updates _existingSettings with current window width/height and persists to CPH when available.
        /// </summary>
        private void PersistWindowSize()
        {
            if (_window == null || _window.WindowState != WindowState.Normal) return;
            if (_existingSettings == null) _existingSettings = new JObject();
            _existingSettings["WindowWidth"] = _window.Width;
            _existingSettings["WindowHeight"] = _window.Height;
            if (_cph != null && !string.IsNullOrEmpty(_settingsKey))
            {
                var settings = BuildSettings();
                if (settings != null)
                {
                    foreach (var kv in settings)
                        _existingSettings[kv.Key] = kv.Value;
                }
                _cph.SetGlobalVar(_settingsKey, _existingSettings.ToString(), true);
            }
        }

        /// <summary>
        /// Saves current form values and window size to CPH; clears dirty flag.
        /// </summary>
        private void SaveValuesInternal()
        {
            PersistWindowSize();
            _dirty = false;
        }

        /// <summary>
        /// Repopulates all controls from _existingSettings (e.g. after Reset or reload).
        /// </summary>
        private void OverwriteUiWithSettings()
        {
            if (_mainGrid == null || _existingSettings == null) return;
            foreach (var child in Descendants(_mainGrid))
            {
                if (child is System.Windows.Controls.TextBox tb && tb.Tag is string keyTb)
                {
                    var val = _existingSettings[keyTb];
                    tb.Text = val != null ? val.ToString() : "";
                }
                else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag is string keyPb)
                {
                    var val = _existingSettings[keyPb];
                    pb.Password = val != null ? val.ToString() : "";
                }
                else if (child is ToggleSwitch ts && ts.Tag is string keyTs)
                {
                    var val = _existingSettings[keyTs];
                    ts.IsChecked = val != null && (val.Type == JTokenType.Boolean ? val.Value<bool>() : val.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                else if (child is System.Windows.Controls.Slider sl && sl.Tag is string keySl)
                {
                    var val = _existingSettings[keySl];
                    if (val != null && (val.Type == JTokenType.Integer || val.Type == JTokenType.Float))
                        sl.Value = val.Value<double>();
                }
                else if (child is System.Windows.Controls.ComboBox combo && combo.Tag is string keyCombo)
                {
                    var val = _existingSettings[keyCombo];
                    if (val != null)
                    {
                        if (val.Type == JTokenType.Integer)
                            combo.SelectedIndex = Math.Max(0, Math.Min(val.Value<int>(), combo.Items?.Count > 0 ? combo.Items.Count - 1 : 0));
                        else
                        {
                            var str = val.ToString();
                            if (combo.Items != null)
                                for (int i = 0; i < combo.Items.Count; i++)
                                    if (string.Equals(combo.Items[i]?.ToString(), str, StringComparison.OrdinalIgnoreCase))
                                    { combo.SelectedIndex = i; break; }
                        }
                    }
                }
                else if (child is StackPanel sp && sp.Tag is string tag && tag.StartsWith("dynamic:"))
                {
                    var key = tag.Substring(8);
                    var val = _existingSettings[key];
                    if (val is JArray arr)
                    {
                        var listPanel = GetDynamicListPanel(sp);
                        if (listPanel != null)
                        {
                            var boxes = listPanel.Children.OfType<System.Windows.Controls.TextBox>().ToList();
                            for (int i = 0; i < boxes.Count && i < arr.Count; i++)
                                boxes[i].Text = arr[i]?.ToString() ?? "";
                        }
                    }
                }
            }
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void LoadSettings()
        {
            if (_cph == null || string.IsNullOrEmpty(_settingsKey))
            {
                _existingSettings = new JObject();
                return;
            }
            try
            {
                string json = _cph.GetGlobalVar<string>(_settingsKey, true);
                if (!string.IsNullOrEmpty(json))
                    _existingSettings = JObject.Parse(json);
                else
                    _existingSettings = new JObject();
            }
            catch
            {
                _existingSettings = new JObject();
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
                    Title = !string.IsNullOrEmpty(_displayVersion) ? $"{_extensionName} (v{_displayVersion})" : _extensionName,
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                // Restore window size from saved settings
                if (_existingSettings != null)
                {
                    var w = _existingSettings["WindowWidth"];
                    var h = _existingSettings["WindowHeight"];
                    if (w != null && h != null)
                    {
                        double wd, hd;
                        if (double.TryParse(w.ToString(), out wd) && double.TryParse(h.ToString(), out hd) && wd > 0 && hd > 0)
                        {
                            _window.Width = wd;
                            _window.Height = hd;
                        }
                    }
                }
                Log("InitializeWindow: FluentWindow created");

                // Apply WPF-UI theme - matching original pattern
                ApplicationThemeManager.Apply((ApplicationTheme)1, (WindowBackdropType)2, true);
                Log("InitializeWindow: Theme applied (first call)");
                ApplicationThemeManager.Apply((FrameworkElement)_window);
                Log("InitializeWindow: Theme applied (second call)");

                // Main layout: DockPanel avoids Grid+ScrollViewer sizing bugs (content area fills, footer docks to bottom)
                var mainDock = new DockPanel
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    LastChildFill = true
                };

                var footer = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(20, 12, 20, 20),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                DockPanel.SetDock(footer, Dock.Bottom);
                mainDock.Children.Add(footer);

                // Decompile-style layout: Row 0 = header (full width), Row 1 = sidebar | content
                _mainGrid = new Grid
                {
                    Margin = new Thickness(20, 20, 20, 0),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Row 1: two columns - fixed sidebar (ListBox) | content (ScrollViewer with tab panels)
                var sidebarContentGrid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 160 });
                sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                _sidebar = new System.Windows.Controls.ListBox
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    Padding = new Thickness(4, 2, 8, 2),
                    MinWidth = 140
                };
                // Rounded tabs with smooth active/hover colors
                var listItemStyle = new Style(typeof(ListBoxItem));
                listItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 10, 12, 10)));
                listItemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(4, 2, 4, 2)));
                listItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Colors.White)));
                listItemStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
                listItemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                listItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                listItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
                // Template: rounded Border so the whole item is rounded
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.Name = "Bd";
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                borderFactory.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                borderFactory.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                borderFactory.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentFactory.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                borderFactory.AppendChild(contentFactory);
                listItemStyle.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(ListBoxItem)) { VisualTree = borderFactory }));
                // Hover: subtle muted gray
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x26, 0x2d, 0x3d))));
                listItemStyle.Triggers.Add(hoverTrigger);
                // Selected: smooth gradient (soft indigo/slate) + subtle left accent bar
                var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
                var selectedGradient = new LinearGradientBrush(
                    Color.FromRgb(0x2d, 0x35, 0x4a),
                    Color.FromRgb(0x22, 0x28, 0x38),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
                selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedGradient));
                selectedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x63, 0x6b, 0x9a))));
                selectedTrigger.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(3, 0, 0, 0)));
                listItemStyle.Triggers.Add(selectedTrigger);
                _sidebar.ItemContainerStyle = listItemStyle;
                _sidebar.SelectionChanged += (s, e) =>
                {
                    if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ListBoxItem removed && removed.Tag is string prevTab && _tabContentPanels.TryGetValue(prevTab, out var prevPanel))
                    {
                        if (_contentScrollViewer != null)
                            _tabScrollOffsets[prevTab] = _contentScrollViewer.VerticalOffset;
                        prevPanel.Visibility = Visibility.Collapsed;
                    }
                    if (_sidebar.SelectedItem is ListBoxItem selected && selected.Tag is string tabName && _tabContentPanels.TryGetValue(tabName, out var panel))
                    {
                        panel.Visibility = Visibility.Visible;
                        if (_contentScrollViewer != null && _tabScrollOffsets.TryGetValue(tabName, out var offset))
                            _contentScrollViewer.ScrollToVerticalOffset(offset);
                    }
                };

                _tabContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top
                };
                _contentScrollViewer = new ScrollViewer
                {
                    Content = _tabContainer,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };

                Grid.SetColumn(_sidebar, 0);
                Grid.SetColumn(_contentScrollViewer, 1);
                sidebarContentGrid.Children.Add(_sidebar);
                sidebarContentGrid.Children.Add(_contentScrollViewer);

                Grid.SetRow(sidebarContentGrid, 1);
                _mainGrid.Children.Add(sidebarContentGrid);

                mainDock.Children.Add(_mainGrid);

                var saveBtn = new Wpf.Ui.Controls.Button { Content = "Save", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
                saveBtn.Click += (s, e) => SaveValuesInternal();
                var saveExitBtn = new Wpf.Ui.Controls.Button { Content = "Save & Exit", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
                saveExitBtn.Click += (s, e) => { SaveValuesInternal(); if (_window != null) ((Window)_window).Close(); };
                var resetBtn = new Wpf.Ui.Controls.Button { Content = "Reset", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
                resetBtn.Click += (s, e) =>
                {
                    if (System.Windows.MessageBox.Show(_window, "Reset all values to last saved? Unsaved changes will be lost.", "Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                    LoadSettings();
                    OverwriteUiWithSettings();
                    _dirty = false;
                    if (_window != null) ((Window)_window).Close();
                };
                var exitBtn = new Wpf.Ui.Controls.Button { Content = "Exit", Padding = new Thickness(16, 8, 16, 8) };
                exitBtn.Click += (s, e) =>
                {
                    if (_dirty && System.Windows.MessageBox.Show(_window, "Discard unsaved changes?", "Exit", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                    if (_window != null) ((Window)_window).Close();
                };
                footer.Children.Add(saveBtn);
                footer.Children.Add(saveExitBtn);
                footer.Children.Add(resetBtn);
                footer.Children.Add(exitBtn);

                _window.Content = mainDock;
                Log("InitializeWindow: Window content set");

                // Handle window closed - do NOT auto-save; only notify and clear _isOpen
                _window.Closed += (s, e) =>
                {
                    if (_window != null && _window.WindowState == WindowState.Normal && _windowClosedCallback != null)
                        _windowClosedCallback(_window.Width, _window.Height);
                    _isOpen = false;
                    Log("Sbui UI has been closed.");
                };

                _isOpen = true;
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

        private void EnsureTabExists(string tabName)
        {
            if (_tabContentPanels == null || _tabContainer == null || _sidebar == null)
                return;
            if (_tabContentPanels.ContainsKey(tabName))
                return;

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };

            var listItem = new ListBoxItem
            {
                Content = tabName,
                Tag = tabName,
                Padding = new Thickness(12, 10, 12, 10)
            };

            _tabContentPanels[tabName] = panel;
            _tabContainer.Children.Add(panel);
            _sidebar.Items.Add(listItem);

            if (_sidebar.SelectedItem == null)
            {
                _sidebar.SelectedItem = listItem;
                panel.Visibility = Visibility.Visible;
            }
        }

        private void BuildContentFromPending(IReadOnlyList<PendingItem> items)
        {
            if (_mainGrid == null || _tabContentPanels == null || items == null) return;
            _controlRegistry?.Clear();
            _dynamicTextboxPanels?.Clear();

            // Header: only one; last AddHeader wins. Add to row 0 of _mainGrid (full width above sidebar|content).
            string headerUrl = null;
            foreach (var item in items)
                if (item.Kind == PendingKind.Header && item.Args != null && item.Args.Length > 0 && item.Args[0] is string url && !string.IsNullOrEmpty(url))
                    headerUrl = url;
            if (!string.IsNullOrEmpty(headerUrl))
            {
                var headerPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(headerUrl, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    var img = new System.Windows.Controls.Image
                    {
                        Source = bi,
                        MaxHeight = 120,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    headerPanel.Children.Add(img);
                }
                catch
                {
                    var placeholder = new System.Windows.Controls.TextBlock
                    {
                        Text = "[Header image]",
                        Foreground = new SolidColorBrush(Colors.Gray),
                        FontSize = 12,
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    headerPanel.Children.Add(placeholder);
                }
                Grid.SetRow(headerPanel, 0);
                _mainGrid.Children.Insert(0, headerPanel);
            }

            var groups = new List<(string visibilityKey, List<PendingItem> groupItems)>();
            string currentKey = null;
            List<PendingItem> currentGroup = null;
            foreach (var item in items)
            {
                if (item.Kind == PendingKind.Header) continue;
                var key = item.VisibilityKey ?? "";
                var useKey = string.IsNullOrEmpty(key) ? null : key;
                if (useKey != currentKey || currentGroup == null)
                {
                    if (currentGroup != null && currentGroup.Count > 0)
                        groups.Add((currentKey, currentGroup));
                    currentKey = useKey;
                    currentGroup = new List<PendingItem>();
                }
                currentGroup.Add(item);
            }
            if (currentGroup != null && currentGroup.Count > 0)
                groups.Add((currentKey, currentGroup));

            foreach (var (visibilityKey, groupItems) in groups)
            {
                if (!string.IsNullOrEmpty(visibilityKey))
                {
                    var container = new StackPanel { Orientation = Orientation.Vertical };
                    _visibilityOverridePanel = container;
                    foreach (var item in groupItems)
                    {
                        var args = item.Args;
                        switch (item.Kind)
                        {
                            case PendingKind.Title: AddTitleToPanel((string)args[0], (string)args[1]); break;
                            case PendingKind.Description: AddDescriptionToPanel((string)args[0], (string)args[1]); break;
                            case PendingKind.ToggleSwitch: AddToggleSwitchToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (bool)args[4]); break;
                            case PendingKind.Textbox: AddTextboxToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (bool)args[5]); break;
                            case PendingKind.Slider: AddSliderToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6]); break;
                            case PendingKind.InlineSeparator: AddInlineSeparatorToPanel((string)args[0]); break;
                            case PendingKind.SliderWithToggleSwitch: AddSliderWithToggleSwitchToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], (bool)args[7]); break;
                            case PendingKind.Filepath: AddFilepathToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            case PendingKind.ClickableButton: AddClickableButtonToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (Action)args[5]); break;
                            case PendingKind.RefreshableDropdown: AddRefreshableDropdownToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (Func<string[]>)args[5], (int)args[6]); break;
                            case PendingKind.ResponseBox: AddResponseBoxToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            case PendingKind.DecimalStepper: AddDecimalStepperToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (double)args[4], (double)args[5], (double)args[6], (double)args[7]); break;
                            case PendingKind.CompetingToggleSwitches: AddCompetingToggleSwitchesToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (int)args[5]); break;
                            case PendingKind.DynamicTextboxesWithPreset: AddDynamicTextboxesWithPresetToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4]); break;
                            case PendingKind.ColorPicker: AddColorPickerToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            default: break;
                        }
                    }
                    _visibilityOverridePanel = null;
                    if (_controlRegistry != null && _controlRegistry.TryGetValue(visibilityKey, out var toggleControl) && toggleControl is ToggleSwitch toggle)
                    {
                        container.Visibility = toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                        toggle.Checked += (s, e) => container.Visibility = Visibility.Visible;
                        toggle.Unchecked += (s, e) => container.Visibility = Visibility.Collapsed;
                    }
                    string tabName = GetTabNameFromItem(groupItems[0]);
                    EnsureTabExists(tabName);
                    if (_tabContentPanels != null && _tabContentPanels.TryGetValue(tabName, out var tabPanel))
                        tabPanel.Children.Add(container);
                }
                else
                {
                    foreach (var item in groupItems)
                    {
                        var args = item.Args;
                        switch (item.Kind)
                        {
                            case PendingKind.Header: break;
                            case PendingKind.Title: AddTitleToPanel((string)args[0], (string)args[1]); break;
                            case PendingKind.Description: AddDescriptionToPanel((string)args[0], (string)args[1]); break;
                            case PendingKind.ToggleSwitch: AddToggleSwitchToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (bool)args[4]); break;
                            case PendingKind.Textbox: AddTextboxToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (bool)args[5]); break;
                            case PendingKind.Slider: AddSliderToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6]); break;
                            case PendingKind.InlineSeparator: AddInlineSeparatorToPanel((string)args[0]); break;
                            case PendingKind.SliderWithToggleSwitch: AddSliderWithToggleSwitchToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], (bool)args[7]); break;
                            case PendingKind.Filepath: AddFilepathToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            case PendingKind.ClickableButton: AddClickableButtonToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (Action)args[5]); break;
                            case PendingKind.RefreshableDropdown: AddRefreshableDropdownToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (Func<string[]>)args[5], (int)args[6]); break;
                            case PendingKind.ResponseBox: AddResponseBoxToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            case PendingKind.DecimalStepper: AddDecimalStepperToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (double)args[4], (double)args[5], (double)args[6], (double)args[7]); break;
                            case PendingKind.CompetingToggleSwitches: AddCompetingToggleSwitchesToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (int)args[5]); break;
                            case PendingKind.DynamicTextboxesWithPreset: AddDynamicTextboxesWithPresetToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4]); break;
                            case PendingKind.ColorPicker: AddColorPickerToPanel((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4]); break;
                            default: break;
                        }
                    }
                }
            }
            _mainGrid?.UpdateLayout();
        }

        private static string GetTabNameFromItem(PendingItem item)
        {
            if (item?.Args == null || item.Args.Length == 0) return "";
            switch (item.Kind)
            {
                case PendingKind.Title:
                case PendingKind.Description: return (item.Args.Length > 1 ? item.Args[1] : null) as string ?? "";
                case PendingKind.InlineSeparator: return (item.Args[0] as string) ?? "";
                case PendingKind.ClickableButton: return (item.Args.Length > 4 ? item.Args[4] : null) as string ?? "";
                default: return (item.Args.Length > 2 ? item.Args[2] : null) as string ?? "";
            }
        }

        private void AddTitleToPanel(string text, string tabName)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 8),
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }

        private void AddDescriptionToPanel(string text, string tabName)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var tb = new System.Windows.Controls.TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            foreach (var inline in ParseRichText(text))
                tb.Inlines.Add(inline);
            panel.Children.Add(tb);
        }

        private static IEnumerable<Inline> ParseRichText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) { yield return new Run(""); yield break; }
            var regex = new Regex(@"(\*\*(?<bold>.+?)\*\*)|_(?<italic>.+?)_|\{(?<col>#[0-9a-fA-F]{6,8})\|(?<colTxt>.+?)\}(?!\})|\{size:(?<size>\d+)\|(?<sizeTxt>.+?)\}(?!\})|\+\+(?<glow>.+?)\+\+|\[link:(?<linkText>.+?)\|(?<linkUrl>.+?)\]", RegexOptions.Singleline);
            var matches = regex.Matches(raw);
            int start = 0;
            foreach (Match m in matches)
            {
                if (m.Index > start)
                    foreach (var i in AddPlain(raw.Substring(start, m.Index - start))) yield return i;
                if (m.Groups["bold"].Success)
                {
                    var span = new Span { FontWeight = FontWeights.Bold };
                    foreach (var i in ParseRichText(m.Groups["bold"].Value)) span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["italic"].Success)
                {
                    var span = new Span { FontStyle = FontStyles.Italic };
                    foreach (var i in ParseRichText(m.Groups["italic"].Value)) span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["col"].Success)
                {
                    var span = new Span { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(m.Groups["col"].Value)) };
                    foreach (var i in ParseRichText(m.Groups["colTxt"].Value)) span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["size"].Success)
                {
                    var span = new Span { FontSize = double.Parse(m.Groups["size"].Value) };
                    foreach (var i in ParseRichText(m.Groups["sizeTxt"].Value)) span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["glow"].Success)
                {
                    var run = new Run(m.Groups["glow"].Value) { FontWeight = FontWeights.SemiBold };
                    yield return run;
                }
                else if (m.Groups["linkText"].Success && m.Groups["linkUrl"].Success)
                {
                    var linkText = m.Groups["linkText"].Value;
                    var linkUrl = m.Groups["linkUrl"].Value;
                    var hyperlink = new Hyperlink(new Run(linkText)) { NavigateUri = new Uri(linkUrl, UriKind.RelativeOrAbsolute) };
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        e.Handled = true;
                        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
                        catch { }
                    };
                    yield return hyperlink;
                }
                start = m.Index + m.Length;
            }
            if (start < raw.Length)
                foreach (var i in AddPlain(raw.Substring(start))) yield return i;
        }

        private static IEnumerable<Inline> AddPlain(string segment)
        {
            var lines = segment.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                yield return new Run(lines[i]);
                if (i < lines.Length - 1) yield return new LineBreak();
            }
        }

        private void AddToggleSwitchToPanel(string title, string description, string tabName, string saveKey, bool defaultValue)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            var ts = new ToggleSwitch { Tag = saveKey };
            ts.IsChecked = _existingSettings[saveKey] != null ? _existingSettings[saveKey].ToObject<bool>() : defaultValue;
            ts.Checked += (s, e) => MarkDirty();
            ts.Unchecked += (s, e) => MarkDirty();
            _controlRegistry[saveKey] = ts;
            stack.Children.Add(ts);
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            panel.Children.Add(stack);
        }

        private void AddTextboxToPanel(string title, string description, string tabName, string saveKey, string defaultText, bool isPassword)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var initial = _existingSettings[saveKey]?.ToString() ?? defaultText ?? "";
            if (isPassword)
            {
                var pb = new System.Windows.Controls.PasswordBox { Tag = saveKey, Password = initial, MinWidth = 200, Height = 28, Margin = new Thickness(0, 4, 0, 0) };
                pb.PasswordChanged += (s, e) => MarkDirty();
                _controlRegistry[saveKey] = pb;
                stack.Children.Add(pb);
            }
            else
            {
                var tb = new System.Windows.Controls.TextBox { Tag = saveKey, Text = initial, MinWidth = 200, Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 0, 0) };
                tb.TextChanged += (s, e) => MarkDirty();
                _controlRegistry[saveKey] = tb;
                stack.Children.Add(tb);
            }
            panel.Children.Add(stack);
        }

        private void AddSliderToPanel(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var val = _existingSettings[saveKey] != null ? _existingSettings[saveKey].ToObject<int>() : defaultValue;
            val = Math.Max(min, Math.Min(max, val));
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var valueBox = new System.Windows.Controls.TextBox { Text = val.ToString(), Width = 60, Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            var slider = new System.Windows.Controls.Slider { Tag = saveKey, Minimum = min, Maximum = max, Value = val, TickFrequency = 1, IsSnapToTickEnabled = true, Height = 24, VerticalAlignment = VerticalAlignment.Center };
            _controlRegistry[saveKey] = slider;
            Grid.SetColumn(valueBox, 0); Grid.SetColumn(slider, 1);
            grid.Children.Add(valueBox); grid.Children.Add(slider);
            slider.ValueChanged += (s, e) => { MarkDirty(); if (!valueBox.IsFocused) valueBox.Text = ((int)slider.Value).ToString(); };
            valueBox.TextChanged += (s, e) => { MarkDirty(); if (int.TryParse(valueBox.Text, out var v)) slider.Value = Math.Max(min, Math.Min(max, v)); };
            stack.Children.Add(grid);
            panel.Children.Add(stack);
        }

        private void AddInlineSeparatorToPanel(string tabName)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var line = new Border
            {
                Height = 1,
                Margin = new Thickness(0, 16, 0, 16),
                Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x45, 0x55)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            panel.Children.Add(line);
        }

        private void AddSliderWithToggleSwitchToPanel(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue, bool toggleDefault)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            string enabledKey = saveKey + "_enabled";
            bool enabled = _existingSettings[enabledKey] != null ? _existingSettings[enabledKey].ToObject<bool>() : toggleDefault;
            int val = _existingSettings[saveKey] != null ? _existingSettings[saveKey].ToObject<int>() : defaultValue;
            val = Math.Max(min, Math.Min(max, val));
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var valueBox = new System.Windows.Controls.TextBox { Text = val.ToString(), Width = 60, Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            var slider = new System.Windows.Controls.Slider { Tag = saveKey, Minimum = min, Maximum = max, Value = val, TickFrequency = 1, IsSnapToTickEnabled = true, Height = 24, VerticalAlignment = VerticalAlignment.Center, IsEnabled = enabled };
            _controlRegistry[saveKey] = slider;
            var ts = new ToggleSwitch { Tag = enabledKey };
            ts.IsChecked = enabled;
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(valueBox, 0); Grid.SetColumn(slider, 1);
            grid.Children.Add(valueBox); grid.Children.Add(slider);
            grid.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            ts.Checked += (s, e) => { MarkDirty(); grid.Visibility = Visibility.Visible; slider.IsEnabled = true; };
            ts.Unchecked += (s, e) => { MarkDirty(); grid.Visibility = Visibility.Collapsed; slider.IsEnabled = false; };
            _controlRegistry[enabledKey] = ts;
            stack.Children.Add(ts);
            slider.ValueChanged += (s, e) => { MarkDirty(); if (!valueBox.IsFocused) valueBox.Text = ((int)slider.Value).ToString(); };
            valueBox.TextChanged += (s, e) => { MarkDirty(); if (int.TryParse(valueBox.Text, out var v)) slider.Value = Math.Max(min, Math.Min(max, v)); };
            stack.Children.Add(grid);
            panel.Children.Add(stack);
        }

        private void AddFilepathToPanel(string title, string description, string tabName, string saveKey, string defaultPath)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var initial = _existingSettings[saveKey]?.ToString() ?? defaultPath ?? "";
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tb = new System.Windows.Controls.TextBox { Tag = saveKey, Text = initial, MinWidth = 200, Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            tb.TextChanged += (s, e) => MarkDirty();
            _controlRegistry[saveKey] = tb;
            var browseBtn = new Wpf.Ui.Controls.Button { Content = "Browse", Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(12, 6, 12, 6) };
            browseBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog { FileName = tb.Text };
                if (dlg.ShowDialog() == true)
                {
                    tb.Text = dlg.FileName;
                    MarkDirty();
                }
            };
            Grid.SetColumn(tb, 0); Grid.SetColumn(browseBtn, 1);
            pathGrid.Children.Add(tb); pathGrid.Children.Add(browseBtn);
            stack.Children.Add(pathGrid);
            panel.Children.Add(stack);
        }

        private void AddClickableButtonToPanel(string title, string description, string confirmText, string color, string tabName, Action callback)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) });
            var btn = new Wpf.Ui.Controls.Button { Content = confirmText ?? "OK", Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(16, 8, 16, 8) };
            try
            {
                if (!string.IsNullOrEmpty(color) && color.StartsWith("#"))
                {
                    var brush = (Brush)new BrushConverter().ConvertFrom(color);
                    if (brush != null) btn.Background = brush;
                }
            }
            catch { }
            btn.Click += (s, e) =>
            {
                try
                {
                    if (_window != null && !_window.Dispatcher.CheckAccess())
                        _window.Dispatcher.Invoke(callback);
                    else
                        callback?.Invoke();
                }
                catch (Exception ex) { Log($"Button callback error: {ex.Message}"); }
            };
            stack.Children.Add(btn);
            panel.Children.Add(stack);
        }

        private void AddRefreshableDropdownToPanel(string title, string description, string tabName, string saveKey, string[] options, Func<string[]> refreshCallback, int defaultIndex)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            options = options ?? Array.Empty<string>();
            var savedIndex = _existingSettings[saveKey] != null && _existingSettings[saveKey].Type == JTokenType.Integer ? _existingSettings[saveKey].Value<int>() : defaultIndex;
            savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cb = new System.Windows.Controls.ComboBox { Tag = saveKey, ItemsSource = options, SelectedIndex = savedIndex, MinWidth = 200, Height = 30, Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            cb.SelectionChanged += (s, e) => MarkDirty();
            _controlRegistry[saveKey] = cb;
            var refreshIcon = new System.Windows.Controls.TextBlock
            {
                Text = "\uE117",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var refreshBtn = new Wpf.Ui.Controls.Button { Content = refreshIcon, ToolTip = "Refresh", Width = 30, Height = 30, Padding = new Thickness(0), Margin = new Thickness(0, 4, 0, 0) };
            refreshBtn.Click += (s, e) =>
            {
                try
                {
                    var newOptions = refreshCallback?.Invoke() ?? Array.Empty<string>();
                    var idx = cb.SelectedIndex;
                    if (idx < 0 || idx >= (newOptions?.Length ?? 0)) idx = 0;
                    UpdateDropdown(saveKey, newOptions ?? Array.Empty<string>(), idx);
                }
                catch (Exception ex) { Log($"Refresh callback error: {ex.Message}"); }
            };
            Grid.SetColumn(cb, 0); Grid.SetColumn(refreshBtn, 1);
            row.Children.Add(cb); row.Children.Add(refreshBtn);
            stack.Children.Add(row);
            panel.Children.Add(stack);
        }

        /// <summary>
        /// Updates a dropdown (e.g. from AddRefreshableDropdown) with new options and selected index. Must be called on UI thread or via Dispatcher.
        /// </summary>
        public void UpdateDropdown(string key, string[] options, int selectedIndex)
        {
            if (_window == null || _controlRegistry == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                if (!_controlRegistry.TryGetValue(key, out var c) || !(c is System.Windows.Controls.ComboBox cb)) return;
                options = options ?? Array.Empty<string>();
                cb.ItemsSource = options;
                cb.SelectedIndex = Math.Max(0, Math.Min(selectedIndex, options.Length > 0 ? options.Length - 1 : 0));
            });
        }

        private void AddResponseBoxToPanel(string title, string description, string tabName, string saveKey, string defaultText)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var initial = _existingSettings[saveKey]?.ToString() ?? defaultText ?? "";
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var tb = new System.Windows.Controls.TextBox
            {
                Tag = saveKey,
                Text = initial,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinWidth = 200,
                MinHeight = 80,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            tb.TextChanged += (s, e) => MarkDirty();
            _controlRegistry[saveKey] = tb;
            stack.Children.Add(tb);
            panel.Children.Add(stack);
        }

        private void AddDecimalStepperToPanel(string title, string description, string tabName, string saveKey, double min, double max, double step, double defaultValue)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var val = _existingSettings[saveKey] != null && (_existingSettings[saveKey].Type == JTokenType.Float || _existingSettings[saveKey].Type == JTokenType.Integer)
                ? _existingSettings[saveKey].Value<double>() : defaultValue;
            val = Math.Max(min, Math.Min(max, val));
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tb = new System.Windows.Controls.TextBox { Tag = saveKey, Text = val.ToString("G"), Width = 80, Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            tb.TextChanged += (s, e) =>
            {
                MarkDirty();
                if (double.TryParse(tb.Text, out var v))
                    tb.Text = Math.Max(min, Math.Min(max, v)).ToString("G");
            };
            _controlRegistry[saveKey] = tb;
            var minusBtn = new Wpf.Ui.Controls.Button
            {
                Content = new System.Windows.Controls.TextBlock { Text = "−", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                Width = 28, Height = 28, Margin = new Thickness(0, 4, 2, 0), Padding = new Thickness(0)
            };
            minusBtn.Click += (s, e) => { if (double.TryParse(tb.Text, out var v)) { v = Math.Max(min, v - step); tb.Text = v.ToString("G"); MarkDirty(); } };
            var plusBtn = new Wpf.Ui.Controls.Button
            {
                Content = new System.Windows.Controls.TextBlock { Text = "+", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                Width = 28, Height = 28, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(0)
            };
            plusBtn.Click += (s, e) => { if (double.TryParse(tb.Text, out var v)) { v = Math.Min(max, v + step); tb.Text = v.ToString("G"); MarkDirty(); } };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
            buttonPanel.Children.Add(minusBtn);
            buttonPanel.Children.Add(plusBtn);
            Grid.SetColumn(tb, 0); Grid.SetColumn(buttonPanel, 1);
            row.Children.Add(tb); row.Children.Add(buttonPanel);
            stack.Children.Add(row);
            panel.Children.Add(stack);
        }

        private void AddCompetingToggleSwitchesToPanel(string title, string description, string tabName, string saveKey, string[] options, int defaultIndex)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            options = options ?? Array.Empty<string>();
            var savedIndex = _existingSettings[saveKey] != null && _existingSettings[saveKey].Type == JTokenType.Integer ? _existingSettings[saveKey].Value<int>() : defaultIndex;
            savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) });
            var selector = new System.Windows.Controls.ComboBox { Tag = saveKey, Visibility = Visibility.Collapsed, Width = 0, Height = 0 };
            selector.ItemsSource = Enumerable.Range(0, options.Length).Select(i => i.ToString()).ToArray();
            selector.SelectedIndex = savedIndex;
            _controlRegistry[saveKey] = selector;
            stack.Children.Add(selector);
            for (int i = 0; i < options.Length; i++)
            {
                var idx = i;
                var ts = new ToggleSwitch { Tag = saveKey + "_opt_" + i, Content = options[i], IsChecked = (i == savedIndex) };
                ts.Checked += (s, e) =>
                {
                    MarkDirty();
                    selector.SelectedIndex = idx;
                    for (int j = 0; j < stack.Children.Count; j++)
                    {
                        if (stack.Children[j] is ToggleSwitch other && other != ts && other.Tag is string t && t.StartsWith(saveKey + "_opt_"))
                            other.IsChecked = false;
                    }
                };
                stack.Children.Add(ts);
            }
            panel.Children.Add(stack);
        }

        private void AddDynamicTextboxesWithPresetToPanel(string title, string description, string tabName, string saveKey, string[] presetValues)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            JArray existing = null;
            if (_existingSettings[saveKey] != null && _existingSettings[saveKey] is JArray arr)
                existing = arr;
            var count = existing?.Count ?? (presetValues?.Length ?? 1);
            if (count < 1) count = 1;
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0), Tag = "dynamic:" + saveKey };
            _dynamicTextboxPanels[saveKey] = stack;
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) });
            var listPanel = new StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < count; i++)
            {
                var text = existing != null && i < existing.Count ? existing[i]?.ToString() ?? "" : (presetValues != null && i < presetValues.Length ? presetValues[i] : "");
                var tb = new System.Windows.Controls.TextBox { Tag = saveKey + "|" + i, Text = text, MinWidth = 200, Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 0, 0) };
                tb.TextChanged += (s, e) => MarkDirty();
                listPanel.Children.Add(tb);
            }
            stack.Children.Add(listPanel);
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var addBtn = new Wpf.Ui.Controls.Button { Content = "Add", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
            addBtn.Click += (s, e) =>
            {
                var tb = new System.Windows.Controls.TextBox { Tag = saveKey + "|" + listPanel.Children.Count, Text = "", MinWidth = 200, Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 0, 0) };
                tb.TextChanged += (se, ev) => MarkDirty();
                listPanel.Children.Add(tb);
                MarkDirty();
            };
            var removeBtn = new Wpf.Ui.Controls.Button { Content = "Remove", Padding = new Thickness(12, 6, 12, 6) };
            removeBtn.Click += (s, e) =>
            {
                if (listPanel.Children.Count > 1) { listPanel.Children.RemoveAt(listPanel.Children.Count - 1); MarkDirty(); }
            };
            btnRow.Children.Add(addBtn); btnRow.Children.Add(removeBtn);
            stack.Children.Add(btnRow);
            _controlRegistry[saveKey] = stack;
            panel.Children.Add(stack);
        }

        private static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            foreach (var c in outer.Children)
                if (c is StackPanel inner && inner.Children.OfType<System.Windows.Controls.TextBox>().Any())
                    return inner;
            return null;
        }

        private static string ToColorHex(byte r, byte g, byte b, byte a)
        {
            if (a == 255) return $"#{r:X2}{g:X2}{b:X2}";
            return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        }

        private void AddColorPickerToPanel(string title, string description, string tabName, string saveKey, string defaultColor)
        {
            EnsureTabExists(tabName);
            StackPanel panel = _visibilityOverridePanel ?? (_tabContentPanels.TryGetValue(tabName, out var p) ? p : null);
            if (panel == null) return;
            var initial = _existingSettings[saveKey]?.ToString() ?? defaultColor ?? "#000000";
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
            if (!string.IsNullOrEmpty(description))
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = description, FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var preview = new System.Windows.Shapes.Rectangle { Width = 32, Height = 28, Margin = new Thickness(0, 4, 0, 0), Stroke = Brushes.Gray, StrokeThickness = 1, Cursor = System.Windows.Input.Cursors.Hand };
            try { preview.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(initial)); } catch { preview.Fill = Brushes.Gray; }
            var tb = new System.Windows.Controls.TextBox { Tag = saveKey, Text = initial, MinWidth = 120, Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            preview.MouseDown += (s, e) =>
            {
                Color initialColor;
                try { initialColor = (Color)ColorConverter.ConvertFromString((tb.Text ?? "").Trim()); } catch { initialColor = Colors.Black; }
                var colorPickerRd = new ResourceDictionary();
                try { colorPickerRd.Source = new Uri("pack://application:,,,/ColorPicker;component/Styles/DefaultColorPickerStyle.xaml", UriKind.RelativeOrAbsolute); } catch { }
                var standardColorPicker = new StandardColorPicker
                {
                    SelectedColor = initialColor,
                    ShowAlpha = true,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                if (colorPickerRd["DefaultColorPickerStyle"] is Style darkStyle)
                    standardColorPicker.Style = darkStyle;
                var okBtn = new Wpf.Ui.Controls.Button { Content = "OK", Width = 72, Height = 32, Margin = new Thickness(0, 0, 8, 0) };
                var undoBtn = new Wpf.Ui.Controls.Button { Content = "Undo", Width = 72, Height = 32 };
                var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
                btnRow.Children.Add(okBtn); btnRow.Children.Add(undoBtn);
                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(standardColorPicker, 0);
                Grid.SetRow(btnRow, 1);
                mainGrid.Children.Add(standardColorPicker);
                mainGrid.Children.Add(btnRow);
                var picker = new Window
                {
                    Title = "Pick color",
                    Width = 400,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(preview),
                    Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
                    ResizeMode = ResizeMode.NoResize,
                    Content = new Border { Padding = new Thickness(14), Child = mainGrid }
                };
                picker.Resources.MergedDictionaries.Add(colorPickerRd);
                bool applyOnClose = false;
                okBtn.Click += (_, __) => { applyOnClose = true; picker.Close(); };
                undoBtn.Click += (_, __) => picker.Close();
                picker.Closed += (_, __) =>
                {
                    if (applyOnClose)
                    {
                        var c = standardColorPicker.SelectedColor;
                        tb.Text = ToColorHex(c.R, c.G, c.B, c.A);
                        preview.Fill = new SolidColorBrush(c);
                        MarkDirty();
                    }
                };
                picker.ShowDialog();
            };
            tb.TextChanged += (s, e) =>
            {
                MarkDirty();
                try
                {
                    var c = (Color)ColorConverter.ConvertFromString(tb.Text);
                    preview.Fill = new SolidColorBrush(c);
                }
                catch { }
            };
            _controlRegistry[saveKey] = tb;
            Grid.SetColumn(tb, 0); Grid.SetColumn(preview, 1);
            row.Children.Add(tb); row.Children.Add(preview);
            stack.Children.Add(row);
            panel.Children.Add(stack);
        }

        private JObject BuildSettings()
        {
            if (_mainGrid != null)
            {
                var settings = new JObject();
                foreach (var child in Descendants(_mainGrid))
                {
                    if (child is System.Windows.Controls.TextBox tb && tb.Tag != null)
                        settings[tb.Tag.ToString()] = tb.Text ?? "";
                    else if (child is System.Windows.Controls.PasswordBox pb && pb.Tag != null)
                        settings[pb.Tag.ToString()] = pb.Password ?? "";
                    else if (child is ToggleSwitch ts && ts.Tag != null)
                        settings[ts.Tag.ToString()] = ts.IsChecked == true;
                    else if (child is System.Windows.Controls.Slider sl && sl.Tag != null)
                        settings[sl.Tag.ToString()] = (long)sl.Value;
                    else if (child is System.Windows.Controls.ComboBox cb && cb.Tag != null)
                        settings[cb.Tag.ToString()] = cb.SelectedIndex >= 0 && cb.Items != null && cb.SelectedIndex < cb.Items.Count ? cb.SelectedIndex : 0;
                    else if (child is StackPanel sp && sp.Tag is string tag && tag.StartsWith("dynamic:"))
                    {
                        var key = tag.Substring(8);
                        var listPanel = GetDynamicListPanel(sp);
                        if (listPanel != null)
                        {
                            var arr = new JArray();
                            foreach (var c in listPanel.Children)
                                if (c is System.Windows.Controls.TextBox t) arr.Add(t.Text ?? "");
                            settings[key] = arr;
                        }
                    }
                }
                return settings;
            }
            return new JObject();
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            if (root == null) yield break;
            yield return root;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                foreach (var d in Descendants(child))
                    yield return d;
            }
        }

        private void AddPending(PendingKind kind, object[] args, string showWhenEnabled = null)
        {
            var item = new PendingItem(kind, args) { VisibilityKey = showWhenEnabled ?? _currentVisibilityKey };
            _pendingItems.Add(item);
        }

        /// <summary>
        /// All AddXXX calls inside the action are visible only when the toggle with saveKey is on. Toggle must be added before this block.
        /// </summary>
        public void WithVisibility(string toggleSaveKey, Action content)
        {
            if (content == null) return;
            var prev = _currentVisibilityKey;
            _currentVisibilityKey = toggleSaveKey ?? "";
            try { content(); }
            finally { _currentVisibilityKey = prev; }
        }

        /// <summary>
        /// Add a header image at the top of the UI. Only one header is shown; multiple calls result in the last URL being used.
        /// </summary>
        public void AddHeader(string imageUrl)
        {
            _pendingItems.Add(new PendingItem(PendingKind.Header, new object[] { imageUrl ?? "" }));
        }

        public void AddTitle(string text, string tabName, string showWhenEnabled = null)
        {
            AddPending(PendingKind.Title, new object[] { text, tabName }, showWhenEnabled);
        }

        public void AddDescription(string text, string tabName, string showWhenEnabled = null)
        {
            AddPending(PendingKind.Description, new object[] { text, tabName }, showWhenEnabled);
        }

        public void AddToggleSwitch(string title, string description, string tabName, string saveKey, bool defaultValue = false, string showWhenEnabled = null)
        {
            AddPending(PendingKind.ToggleSwitch, new object[] { title, description ?? "", tabName, saveKey, defaultValue }, showWhenEnabled);
        }

        public void AddTextbox(string title, string description, string tabName, string saveKey, string defaultText, bool isPassword = false, string showWhenEnabled = null)
        {
            AddPending(PendingKind.Textbox, new object[] { title, description ?? "", tabName, saveKey, defaultText ?? "", isPassword }, showWhenEnabled);
        }

        public void AddSlider(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue, string showWhenEnabled = null)
        {
            AddPending(PendingKind.Slider, new object[] { title, description ?? "", tabName, saveKey, min, max, defaultValue }, showWhenEnabled);
        }

        /// <summary>Adds a horizontal separator line in the given tab.</summary>
        public void AddInlineSeparator(string tabName, string showWhenEnabled = null)
        {
            AddPending(PendingKind.InlineSeparator, new object[] { tabName ?? "" }, showWhenEnabled);
        }

        /// <summary>Adds a toggle switch and slider; when toggle is off the slider is disabled. Persists as saveKey (slider value) and saveKey + "_enabled" (toggle).</summary>
        public void AddSliderWithToggleSwitch(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue, bool toggleDefault, string showWhenEnabled = null)
        {
            AddPending(PendingKind.SliderWithToggleSwitch, new object[] { title, description ?? "", tabName, saveKey, min, max, defaultValue, toggleDefault }, showWhenEnabled);
        }

        /// <summary>Adds a file path textbox with Browse button (OpenFileDialog).</summary>
        public void AddFilepath(string title, string description, string tabName, string saveKey, string defaultPath, string showWhenEnabled = null)
        {
            AddPending(PendingKind.Filepath, new object[] { title, description ?? "", tabName, saveKey, defaultPath ?? "" }, showWhenEnabled);
        }

        /// <summary>Adds a clickable button with colored background; click invokes the callback.</summary>
        public void AddClickableButton(string title, string description, string confirmText, string color, string tabName, Action callback, string showWhenEnabled = null)
        {
            AddPending(PendingKind.ClickableButton, new object[] { title, description ?? "", confirmText ?? "OK", color ?? "", tabName ?? "", callback }, showWhenEnabled);
        }

        /// <summary>Adds a dropdown with Refresh button; refresh callback returns new options and UpdateDropdown is called.</summary>
        public void AddRefreshableDropdown(string title, string description, string tabName, string saveKey, string[] options, Func<string[]> refreshCallback, int defaultIndex, string showWhenEnabled = null)
        {
            AddPending(PendingKind.RefreshableDropdown, new object[] { title, description ?? "", tabName, saveKey, options ?? Array.Empty<string>(), refreshCallback, defaultIndex }, showWhenEnabled);
        }

        /// <summary>Adds a multiline textbox for chat response templates.</summary>
        public void AddResponseBox(string title, string description, string tabName, string saveKey, string defaultText, string showWhenEnabled = null)
        {
            AddPending(PendingKind.ResponseBox, new object[] { title, description ?? "", tabName, saveKey, defaultText ?? "" }, showWhenEnabled);
        }

        /// <summary>Adds a decimal value input with up/down stepper.</summary>
        public void AddDecimalStepper(string title, string description, string tabName, string saveKey, double min, double max, double step, double defaultValue, string showWhenEnabled = null)
        {
            AddPending(PendingKind.DecimalStepper, new object[] { title, description ?? "", tabName, saveKey, min, max, step, defaultValue }, showWhenEnabled);
        }

        /// <summary>Adds multiple toggle switches where only one can be active (radio group). saveKey persists selected index.</summary>
        public void AddCompetingToggleSwitches(string title, string description, string tabName, string saveKey, string[] options, int defaultIndex, string showWhenEnabled = null)
        {
            AddPending(PendingKind.CompetingToggleSwitches, new object[] { title, description ?? "", tabName, saveKey, options ?? Array.Empty<string>(), defaultIndex }, showWhenEnabled);
        }

        /// <summary>Adds a list of textboxes with Add/Remove buttons; persists as JSON array.</summary>
        public void AddDynamicTextboxesWithPreset(string title, string description, string tabName, string saveKey, string[] presetValues, string showWhenEnabled = null)
        {
            AddPending(PendingKind.DynamicTextboxesWithPreset, new object[] { title, description ?? "", tabName, saveKey, presetValues ?? Array.Empty<string>() }, showWhenEnabled);
        }

        /// <summary>Adds a color picker (hex textbox with color preview).</summary>
        public void AddColorPicker(string title, string description, string tabName, string saveKey, string defaultColor, string showWhenEnabled = null)
        {
            AddPending(PendingKind.ColorPicker, new object[] { title, description ?? "", tabName, saveKey, defaultColor ?? "#000000" }, showWhenEnabled);
        }

        public T GetValue<T>(string key)
        {
            var token = _existingSettings?[key];
            if (token == null) return default;
            try { return token.ToObject<T>(); }
            catch { return default; }
        }

        /// <summary>
        /// Returns the current value of a control by saveKey (used in button callbacks). Must be called from UI thread or will marshal via Dispatcher.
        /// </summary>
        public T GetPendingValue<T>(string key)
        {
            if (_window == null || _controlRegistry == null) return default;
            T result = default;
            _window.Dispatcher.Invoke(() =>
            {
                if (_controlRegistry == null || !_controlRegistry.TryGetValue(key, out var control)) return;
                try
                {
                    if (control is System.Windows.Controls.TextBox tb)
                    {
                        if (typeof(T) == typeof(double) && double.TryParse(tb.Text, out var dVal))
                            result = (T)(object)dVal;
                        else
                            result = (T)(object)(tb.Text ?? "");
                    }
                    else if (control is System.Windows.Controls.PasswordBox pb)
                        result = (T)(object)(pb.Password ?? "");
                    else if (control is ToggleSwitch ts)
                        result = (T)(object)(ts.IsChecked == true);
                    else if (control is System.Windows.Controls.Slider sl)
                        result = (T)Convert.ChangeType(sl.Value, typeof(T));
                    else if (control is System.Windows.Controls.ComboBox cb)
                    {
                        if (typeof(T) == typeof(int))
                            result = (T)(object)cb.SelectedIndex;
                        else
                            result = (T)(object)(cb.SelectedItem?.ToString() ?? "");
                    }
                    else if (control is StackPanel stack && stack.Tag is string dynTag && dynTag.StartsWith("dynamic:"))
                    {
                        if (typeof(T) == typeof(string[]))
                        {
                            var listPanel = GetDynamicListPanel(stack);
                            if (listPanel != null)
                            {
                                var list = new List<string>();
                                foreach (var c in listPanel.Children)
                                    if (c is System.Windows.Controls.TextBox tx) list.Add(tx.Text ?? "");
                                result = (T)(object)list.ToArray();
                            }
                        }
                    }
                }
                catch { }
            });
            return result;
        }

        /// <summary>
        /// Shows a modal info/success/error message with OK.
        /// </summary>
        public void AddPopupWindow(string title, string message)
        {
            if (_window == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(_window, message ?? "", title ?? "", System.Windows.MessageBoxButton.OK);
            });
        }

        /// <summary>
        /// Shows a confirm dialog; returns MessageBoxResult (Yes/No). Call from non-UI thread via Dispatcher.
        /// </summary>
        public System.Windows.MessageBoxResult ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_window == null) return System.Windows.MessageBoxResult.None;
            System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.None;
            _window.Dispatcher.Invoke(() =>
            {
                result = System.Windows.MessageBox.Show(_window, message ?? "", title ?? "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            });
            return result;
        }

        /// <summary>
        /// Shows a progress window for long-running work. Returns an object with Report(int) and Close(); thread-safe (marshal to UI thread).
        /// </summary>
        public IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            if (_window == null) return null;
            IProgressReporter reporter = null;
            _window.Dispatcher.Invoke(() =>
            {
                var progressWindow = new Window
                {
                    Title = title ?? "Progress",
                    Width = 400,
                    Height = 140,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = _window
                };
                var stack = new StackPanel { Margin = new Thickness(20) };
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = message ?? "", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                var labelBlock = new System.Windows.Controls.TextBlock { Text = progressLabel ?? "Progress", Margin = new Thickness(0, 0, 0, 4) };
                stack.Children.Add(labelBlock);
                var progressBar = new System.Windows.Controls.ProgressBar { Minimum = 0, Maximum = Math.Max(1, total), Value = 0, Height = 24 };
                stack.Children.Add(progressBar);
                progressWindow.Content = stack;
                progressWindow.Show();
                reporter = new ProgressReporterImpl(progressWindow, progressBar, total);
            });
            return reporter;
        }

        /// <summary>
        /// Get a setting by extension name and key. If extensionName matches this instance, delegates to GetValue&lt;T&gt;(key); otherwise returns default.
        /// </summary>
        public T GetValue<T>(string extensionName, string key)
        {
            if (string.IsNullOrEmpty(extensionName) || string.IsNullOrEmpty(key)) return default;
            if (!string.Equals(extensionName, _extensionName, StringComparison.OrdinalIgnoreCase))
                return default;
            return GetValue<T>(key);
        }

        /// <summary>
        /// Returns the DLL version (e.g. from AssemblyVersion). Use for version checks with Streamer.bot extensions.
        /// </summary>
        public static string GetVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Logs the current loaded settings (e.g. for debug). No-op when _existingSettings is null.
        /// </summary>
        public void LogExistingSettings()
        {
            if (_existingSettings == null) return;
            Log(_existingSettings.ToString());
        }

        public void ShowUI()
        {
            var snapshot = _pendingItems.ToList();
            ShowUIInternal(snapshot);
        }

        /// <summary>Fallback when fluent API isn't called. Pass config directly. Order: titles, descriptions, toggleSwitches, textboxes, sliders.</summary>
        public void ShowUI(
            IReadOnlyList<(string text, string tabName)> titles = null,
            IReadOnlyList<(string text, string tabName)> descriptions = null,
            IReadOnlyList<(string title, string description, string tabName, string saveKey, bool defaultValue)> toggleSwitches = null,
            IReadOnlyList<(string title, string description, string tabName, string saveKey, string defaultText, bool isPassword)> textboxes = null,
            IReadOnlyList<(string title, string description, string tabName, string saveKey, int min, int max, int defaultValue)> sliders = null)
        {
            var items = new List<PendingItem>();
            if (titles != null) foreach (var t in titles) items.Add(new PendingItem(PendingKind.Title, new object[] { t.text, t.tabName }));
            if (descriptions != null) foreach (var t in descriptions) items.Add(new PendingItem(PendingKind.Description, new object[] { t.text, t.tabName }));
            if (toggleSwitches != null) foreach (var t in toggleSwitches) items.Add(new PendingItem(PendingKind.ToggleSwitch, new object[] { t.title, t.description ?? "", t.tabName, t.saveKey, t.defaultValue }));
            if (textboxes != null) foreach (var t in textboxes) items.Add(new PendingItem(PendingKind.Textbox, new object[] { t.title, t.description ?? "", t.tabName, t.saveKey, t.defaultText ?? "", t.isPassword }));
            if (sliders != null) foreach (var t in sliders) items.Add(new PendingItem(PendingKind.Slider, new object[] { t.title, t.description ?? "", t.tabName, t.saveKey, t.min, t.max, t.defaultValue }));
            if (items.Count == 0) items = _pendingItems.ToList();
            ShowUIInternal(items);
        }

        private void ShowUIInternal(IReadOnlyList<PendingItem> itemsSnapshot)
        {
    try
    {
        Log($"ShowUI: Called | items={itemsSnapshot?.Count ?? 0}");

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

                // Build from snapshot BEFORE showing window
                BuildContentFromPending(itemsSnapshot ?? new List<PendingItem>());
                _pendingItems.Clear();
                
                Log($"ShowUI: Content built, mainGrid children = {_mainGrid?.Children.Count}");
                Log($"ShowUI: Tab panels count = {_tabContentPanels?.Count}");
                
                // Ensure window shows normally (not minimized) and is activated
                _window.WindowState = WindowState.Normal;
                ((Window)_window).Show();
                _window.Activate();
                
                if (_mainGrid != null)
                {
                    _mainGrid.UpdateLayout();
                    Log($"ShowUI: Content updated, Sidebar = {_sidebar != null}");
                }
                
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

    /// <summary>
    /// Progress reporter for ShowProgressWindow: Report(int) and Close(); thread-safe.
    /// </summary>
    public interface IProgressReporter
    {
        void Report(int current);
        void Close();
    }

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
                try { _window.Close(); } catch { }
            });
        }
    }
}
