using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
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
using FluentConfig.Core;
using FluentConfig.Components;
using FluentConfig.Elements;
using FluentConfig.Helpers;
using SettingsPathHelper = FluentConfig.Core.SettingsPathHelper;

namespace FluentConfig
{
    public partial class FluentConfig : IRenderContext
    {
        private Window _window;
        private readonly IInlineInvokeProxy _cph;
        private readonly string _extensionName;
        private readonly string _displayVersion;
        private readonly string _settingsKey;
        private JObject _existingSettings;
        private readonly bool _withUi;
        private readonly bool _plainWindow;
        private readonly SettingsManager _settingsManager;
        private TabManager _tabManager;
        private Grid _mainGrid;
        private ContentControl _headerPlaceholder;
        private System.Windows.Controls.ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private DockPanel _contentPanel;
        private bool _dirty;
        private readonly ControlRegistry _controlRegistry = new ControlRegistry();
        private readonly SettingsSynchronizer _synchronizer = new SettingsSynchronizer();
        private Dictionary<string, StackPanel> _dynamicTextboxPanels = new Dictionary<string, StackPanel>();

        private StackPanel _visibilityOverridePanel;
        internal StackPanel VisibilityOverridePanel => _visibilityOverridePanel;
        internal TabManager TabManagerInternal => _tabManager;
        internal ControlRegistry ControlRegistryInternal => _controlRegistry;
        internal JObject ExistingSettings => _existingSettings;
        internal void EnsureExistingSettings() { if (_existingSettings == null) _existingSettings = new JObject(); }
        private readonly FluentConfigPanelContext _panelContext;
        private PerformanceTracer _perfTracer;

        /// <summary>
        /// Performance tracer for instrumented startup profiling.
        /// Available after construction; call LogSummary() after Show() to see the full report.
        /// </summary>
        internal PerformanceTracer PerfTracer => _perfTracer;

        private static Action<string> _logCallback;

        public static void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }

        /// <summary>
        /// Optional callback when window closes - receives (width, height) for persistence (e.g. via CPH.SetGlobalVar).
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            FluentConfigWindowManager.SetWindowClosedCallback(callback);
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new FluentConfig to avoid duplicate windows.
        /// </summary>
        public static bool AlreadyOpened(string title = "FluentConfig", string version = "1.0")
        {
            return FluentConfigWindowManager.AlreadyOpened(title, version, LogInternal);
        }

        public static bool IsOpen => FluentConfigWindowManager.IsOpen;

        internal static void LogInternal(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                System.Diagnostics.Debug.WriteLine($"[FluentConfig] {message}");
        }

        StackPanel IRenderContext.GetPanel(string tabName)
        {
            var panel = _panelContext.GetTargetPanel(tabName);
            return panel as StackPanel ?? _tabManager?.GetPanel(tabName);
        }
        JObject IRenderContext.Settings => _existingSettings;
        JToken IRenderContext.GetSetting(string path)
        {
            if (_existingSettings == null || string.IsNullOrEmpty(path)) return null;
            return _existingSettings.SelectToken(path) ?? _existingSettings[path];
        }
        ControlRegistry IRenderContext.Registry => _controlRegistry;
        void IRenderContext.MarkDirty() => MarkDirty();
        Window IRenderContext.Window => _window;
        void IRenderContext.Log(string message) => LogInternal(message);
        void IRenderContext.UpdateDropdown(string key, string[] options, int selectedIndex) => UpdateDropdown(key, options, selectedIndex);
        void IRenderContext.UpdateDropdownWithPairValue(string displayKey, string valueKey, System.Collections.Generic.IEnumerable<(string Value, string Display)> options, string selectedValue) => UpdateDropdownWithPairValue(displayKey, valueKey, options, selectedValue);
        IDictionary<string, StackPanel> IRenderContext.DynamicTextboxPanels => _dynamicTextboxPanels;
        void IRenderContext.SetVisibilityOverridePanel(StackPanel panel) => _visibilityOverridePanel = panel;
        void IRenderContext.ClearVisibilityOverridePanel() => _visibilityOverridePanel = null;

        /// <summary>
        /// Gets the panel for a tab. Use when adding content to a specific tab (e.g. for pill callback sections).
        /// </summary>
        public StackPanel GetPanel(string tabName)
        {
            EnsureTabExists(tabName);
            return _tabManager?.GetPanel(tabName);
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
            DialogHelper.Toast(_window, message);
        }

        /// <summary>
        /// Constructor with CPH for loading/saving settings. Use extensionName for storage key FluentConfig_Settings_{extensionName}.
        /// </summary>
        public FluentConfig(IInlineInvokeProxy cph, string extensionName, bool withUi = true) : this(cph, extensionName, null, withUi)
        {
        }

        /// <summary>
        /// Constructor with CPH and optional display version for window title (e.g. "{extensionName} (v{displayVersion})").
        /// </summary>
        public FluentConfig(IInlineInvokeProxy cph, string extensionName, string displayVersion, bool withUi = true, bool plainWindow = false)
        {
            _perfTracer = new PerformanceTracer(LogInternal);
            _perfTracer.Start("Constructor.Init");

            _cph = cph;
            _extensionName = extensionName ?? "Settings";
            _displayVersion = string.IsNullOrEmpty(displayVersion) ? null : displayVersion;
            _settingsKey = cph != null ? "FluentConfig_Settings_" + extensionName : null;
            _withUi = withUi;
            _plainWindow = plainWindow;

            _perfTracer.BeginPhase("SettingsManager.Create+Load");
            _settingsManager = new SettingsManager(cph, _settingsKey, LogInternal);
            _existingSettings = _settingsManager.Load();
            _panelContext = new FluentConfigPanelContext(this);

            if (!_withUi) return;

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException(
                    "FluentConfig requires the extension thread to be STA. " +
                    "Current apartment state: " + Thread.CurrentThread.GetApartmentState());
            }

            _perfTracer.BeginPhase("WPF Application.Create");
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                // WPF UI requires ThemesDictionary + ControlsDictionary for dark styling of TextBox, ScrollViewer, etc.
                Application.Current.Resources.MergedDictionaries.Add(
                    new Wpf.Ui.Markup.ThemesDictionary { Theme = ApplicationTheme.Dark });
                Application.Current.Resources.MergedDictionaries.Add(
                    new Wpf.Ui.Markup.ControlsDictionary());
            }

            _perfTracer.BeginPhase("ThemeManager.Apply (global)");
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);

            _perfTracer.BeginPhase("WindowBuilder.Build");
            var builder = new WindowBuilder(_extensionName, _displayVersion ?? "", _existingSettings, _perfTracer, _plainWindow);
            _window = builder.Build(
                out _mainGrid,
                out _headerPlaceholder,
                out _tabManager,
                out _sidebar,
                out _contentScrollViewer,
                out _contentPanel,
                onSave: SaveValuesInternal,
                onSaveAndExit: () => { SaveValuesInternal(); _window?.Close(); },
                onReset: HandleReset,
                onExit: HandleExit
            );

            // Per-window theme apply is needed for FluentWindow dark title bar + Mica backdrop.
            // Skip for plain Window — it doesn't need Mica/DWM setup and uses explicit Background brush.
            if (!_plainWindow)
            {
                _perfTracer.BeginPhase("ThemeManager.Apply (window)");
                ApplicationThemeManager.Apply((FrameworkElement)_window);
            }

            _window.Closed += (s, e) =>
            {
                if (_window?.WindowState == WindowState.Normal)
                    FluentConfigWindowManager.InvokeWindowClosedCallback(_window.Width, _window.Height);
                FluentConfigWindowManager.SetOpened(false);
                LogInternal("FluentConfig UI has been closed.");
            };

            FluentConfigWindowManager.SetOpened(true);
            _perfTracer.BeginPhase("Post-InitializeUI");
        }

        /// <summary>
        /// Saves current form values and window size to CPH; clears dirty flag.
        /// </summary>
        private void SaveValuesInternal()
        {
            if (_window?.WindowState == WindowState.Normal)
            {
                if (_existingSettings == null) _existingSettings = new JObject();
                _existingSettings["WindowWidth"] = _window.Width;
                _existingSettings["WindowHeight"] = _window.Height;
            }
            if (_existingSettings == null) _existingSettings = new JObject();
            var settings = BuildSettings();
            if (settings != null)
            {
                foreach (var kv in settings)
                    SettingsPathHelper.SetNestedValue(_existingSettings, kv.Key, kv.Value);
            }
            _settingsManager.Save(_existingSettings);
            _dirty = false;
        }

        /// <summary>
        /// Repopulates all controls from _existingSettings (e.g. after Reset or reload).
        /// </summary>
        private void OverwriteUiWithSettings()
        {
            if (_mainGrid == null || _existingSettings == null) return;
            _synchronizer.LoadSettingsIntoControls(_controlRegistry, _existingSettings);
        }

        internal void MarkDirty()
        {
            _dirty = true;
        }

        private void LoadSettings()
        {
            _existingSettings = _settingsManager.Load();
        }

        private void HandleReset()
        {
            if (System.Windows.MessageBox.Show(_window, "Reset all values to last saved? Unsaved changes will be lost.", "Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            LoadSettings();
            OverwriteUiWithSettings();
            _dirty = false;
            _window?.Close();
        }

        private void HandleExit()
        {
            if (_dirty && System.Windows.MessageBox.Show(_window, "Discard unsaved changes?", "Exit", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            _window?.Close();
        }

        internal void EnsureTabExists(string tabName)
        {
            _tabManager?.EnsureTab(tabName);
        }

        /// <summary>
        /// Builds all deferred sections synchronously. Used by tests.
        /// </summary>
        internal void BuildAllDeferredSections()
        {
            _tabManager?.BuildAllDeferred();
        }

        /// <summary>
        /// Registers a deferred section builder. The tab/sidebar item is created immediately,
        /// but the section content is only built on first tab switch (lazy loading).
        /// </summary>
        internal void RegisterDeferredSection(string tabId, string title, Action<SectionBuilder> build)
        {
            if (_tabManager == null || build == null) return;
            _tabManager.RegisterDeferredBuilder(tabId, () =>
            {
                _perfTracer?.BeginPhase($"Section (lazy): {tabId}");
                var titleEl = new Elements.TitleElement(title ?? "", tabId ?? "");
                AddElement(tabId ?? "", titleEl, null);
                var section = new SectionBuilder(this, tabId ?? "");
                build(section);
                section.FlushPending();
                _perfTracer?.EndPhase();
            });
        }

        /// <summary>
        /// Temporarily pushes a panel so all Add* calls add to it. Use for dynamic content (e.g. when adding new alias sections from pill callback).
        /// </summary>
        public void WithPanel(Panel panel, Action content)
        {
            _panelContext.WithPanel(panel, content);
        }

        /// <summary>
        /// Gets the full saveKey including any context prefix (e.g. repeatable row).
        /// </summary>
        internal string GetFullSaveKey(string saveKey)
        {
            return _panelContext.GetFullSaveKey(saveKey);
        }

        /// <summary>
        /// Updates a dropdown with new options and selected index.
        /// </summary>
        public void UpdateDropdown(string key, string[] options, int selectedIndex)
        {
            if (_window == null) return;
            var cb = _controlRegistry.Get<System.Windows.Controls.ComboBox>(key);
            if (cb == null) return;
            options = options ?? Array.Empty<string>();
            cb.ItemsSource = options;
            cb.SelectedIndex = Math.Max(0, Math.Min(selectedIndex, options.Length > 0 ? options.Length - 1 : 0));
        }

        /// <summary>
        /// Updates a pair-value dropdown with new options and selected value.
        /// </summary>
        public void UpdateDropdownWithPairValue(string displayKey, string valueKey, System.Collections.Generic.IEnumerable<(string Value, string Display)> options, string selectedValue)
        {
            if (_window == null) return;
            var cb = _controlRegistry.Get<System.Windows.Controls.ComboBox>(displayKey);
            if (cb == null) return;
            var list = options != null ? new System.Collections.Generic.List<Elements.DropdownItem>() : null;
            if (list != null)
            {
                foreach (var p in options)
                    list.Add(new Elements.DropdownItem { Value = p.Value, Display = p.Display });
            }
            cb.ItemsSource = list ?? new System.Collections.Generic.List<Elements.DropdownItem>();
            var idx = 0;
            if (list != null && !string.IsNullOrEmpty(selectedValue))
            {
                var i = list.FindIndex(x => string.Equals(x.Value, selectedValue, StringComparison.OrdinalIgnoreCase));
                if (i >= 0) idx = i;
            }
            cb.SelectedIndex = Math.Max(0, Math.Min(idx, list?.Count - 1 ?? 0));
        }

        private JObject BuildSettings()
        {
            return _synchronizer.ExtractSettingsFromControls(_controlRegistry);
        }

        /// <summary>
        /// Wraps addContent in a visibility container when showWhenEnabled is set. Toggle must exist.
        /// </summary>
        private void AddWithOptionalVisibility(string showWhenEnabled, string tabName, Action addContent)
        {
            if (string.IsNullOrEmpty(showWhenEnabled))
            {
                addContent();
                return;
            }
            var toggle = _controlRegistry.Get<ToggleSwitch>(showWhenEnabled);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{showWhenEnabled}' must be added before showWhenEnabled reference");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            var panel = (Panel)_panelContext.GetTargetPanel(tabName);
            _panelContext.PushPanel(container);
            addContent();
            _panelContext.PopPanel();

            toggle.Checked += (s, e) => container.Visibility = Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = Visibility.Collapsed;

            if (panel != null)
                panel.Children.Add(container);
        }

        /// <summary>
        /// All AddXXX calls inside the action are visible only when the toggle with saveKey is on (or off when inverted). Toggle must be added before this block.
        /// </summary>
        /// <param name="inverted">When true, content is visible when toggle is OFF instead of ON.</param>
        public void WithVisibility(string toggleSaveKey, string tabName, Action content, bool inverted = false)
        {
            _panelContext.WithVisibility(toggleSaveKey, tabName, content, inverted);
        }

        /// <summary>
        /// Single code path: add an element to the given tab (or current panel when inside WithPanel). Used by fluent builders.
        /// Logs per-element timing when PerformanceTracer is active and render takes > 5ms.
        /// </summary>
        internal void AddElement(string tabName, Elements.UIElement element, string showWhenKey = null)
        {
            if (element == null) return;
            element.TabName = tabName ?? "";
            if (!string.IsNullOrEmpty(showWhenKey))
            {
                var sw1 = _perfTracer != null ? Stopwatch.StartNew() : null;
                AddWithOptionalVisibility(showWhenKey, tabName, () => element.Render(this));
                if (sw1 != null && sw1.ElapsedMilliseconds > 5)
                    LogInternal($"[PerfTrace]   └ {element.GetType().Name} (+visibility): {sw1.ElapsedMilliseconds}ms");
                return;
            }
            var sw = _perfTracer != null ? Stopwatch.StartNew() : null;
            EnsureTabExists(tabName);
            element.Render(this);
            if (sw != null && sw.ElapsedMilliseconds > 5)
                LogInternal($"[PerfTrace]   └ {element.GetType().Name}: {sw.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Visibility block that builds content with a PanelBuilder (fluent DSL). Toggle must exist.
        /// </summary>
        public void WithVisibility(string toggleSaveKey, string tabName, bool inverted, Action<PanelBuilder> build)
        {
            _panelContext.WithVisibility(toggleSaveKey, tabName, inverted, build);
        }

        /// <summary>
        /// Repeatable rows: buildRow receives a PanelBuilder for each row's content (fluent DSL).
        /// </summary>
        public void WithRepeatableRows(string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            _panelContext.WithRepeatableRows(saveKey, tabName, buildRow);
        }

        /// <summary>
        /// Add a header image at the top of the UI. Only one header is shown; multiple calls result in the last URL being used.
        /// Loads asynchronously by default — shows a placeholder while the image downloads.
        /// </summary>
        public void AddHeader(string imageUrl)
        {
            if (_headerPlaceholder == null || string.IsNullOrEmpty(imageUrl)) return;
            _perfTracer?.BeginPhase("AddHeader (async image)");
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            try
            {
                var img = new System.Windows.Controls.Image
                {
                    MaxHeight = 120,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // Load asynchronously — WPF downloads in background, updates Image when done.
                // No BitmapCacheOption.OnLoad so we don't block the UI thread.
                var bi = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));
                img.Source = bi;

                headerPanel.Children.Add(img);
            }
            catch (Exception ex)
            {
                LogInternal($"AddHeader image load failed: {ex.Message}");
                headerPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "[Header image]",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 4)
                });
            }
            _headerPlaceholder.Content = headerPanel;
        }

        public T GetValue<T>(string key)
        {
            var token = _existingSettings?[key];
            if (token == null) return default;
            try { return token.ToObject<T>(); }
            catch (Exception ex) { LogInternal($"GetValue<{typeof(T).Name}> error for key '{key}': {ex.Message}"); return default; }
        }

        /// <summary>
        /// Sets a value programmatically. Updates in-memory settings and, when the window is open, the corresponding control.
        /// Call MarkDirty if you need the change persisted on next Save.
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_existingSettings == null) _existingSettings = new JObject();
            _existingSettings[key] = value != null ? JToken.FromObject(value) : JValue.CreateNull();
            if (_mainGrid != null)
                _synchronizer.UpdateControl(_mainGrid, key, _existingSettings[key], _existingSettings);
        }

        /// <summary>
        /// Removes the given top-level keys from the current settings (e.g. when an alias is deleted).
        /// Call MarkDirty so the next Save will persist the change.
        /// </summary>
        public void RemoveSettingsKeys(params string[] keys)
        {
            if (_existingSettings == null || keys == null) return;
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key))
                    _existingSettings.Remove(key);
            }
            MarkDirty();
        }

        /// <summary>
        /// Returns the current value of a control by saveKey (used in button callbacks).
        /// </summary>
        public T GetPendingValue<T>(string key)
        {
            if (_window == null) return default;
            return _controlRegistry.GetValue<T>(key);
        }

        /// <summary>
        /// Shows a modal info/success/error message with OK.
        /// </summary>
        public void AddPopupWindow(string title, string message)
        {
            if (_window == null) return;
            DialogHelper.AddPopupWindow(_window, title, message);
        }

        /// <summary>
        /// Shows a confirm dialog; returns MessageBoxResult (Yes/No). Call from non-UI thread via Dispatcher.
        /// </summary>
        public System.Windows.MessageBoxResult ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_window == null) return System.Windows.MessageBoxResult.None;
            return DialogHelper.ShowConfirmDialog(_window, title, message, yesButton, noButton);
        }

        /// <summary>
        /// Shows a progress window for long-running work. Returns an object with Report(int) and Close().
        /// </summary>
        public IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            if (_window == null) return null;
            return ProgressWindowHelper.Show(_window, title, message, progressLabel, total);
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
            catch (Exception ex)
            {
                LogInternal($"GetVersion failed: {ex.Message}");
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

        /// <summary>
        /// Shows the window immediately with a minimal loading overlay (tiny visual tree = fast Show),
        /// then swaps in the real content beneath the overlay and builds the first section.
        /// The overlay stays visible until the first section is fully built, then is removed.
        /// Uses DispatcherFrame to pump messages so the spinner animates during the build.
        /// Remaining sections are built lazily on tab switch.
        /// </summary>
        internal void ShowUIWithLoadingOverlay(string firstTabId)
        {
            if (_window == null)
            {
                Log("ShowUIWithLoadingOverlay called but window is null. Was constructor called with withUi=false?");
                return;
            }

            // Create a minimal loading overlay — this is the ONLY content shown at first
            _perfTracer?.BeginPhase("CreateLoadingOverlay");
            var overlay = new System.Windows.Controls.Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 0x1e, 0x1e, 0x2e)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var loadingPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var progressRing = new Wpf.Ui.Controls.ProgressRing
            {
                IsIndeterminate = true,
                Width = 48,
                Height = 48
            };
            var loadingText = new System.Windows.Controls.TextBlock
            {
                Text = "Loading...",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            loadingPanel.Children.Add(progressRing);
            loadingPanel.Children.Add(loadingText);
            overlay.Children.Add(loadingPanel);

            // Set window content to ONLY the loading overlay — tiny visual tree for fast Show()
            _window.Content = overlay;

            // Show window immediately — with just the spinner, this should be much faster
            // than showing the full sidebar+grid+footer+scrollviewer tree
            _perfTracer?.BeginPhase("Window.Show+Activate (overlay only)");
            _window.Show();
            _window.Activate();
            _perfTracer?.EndPhase();

            // Use DispatcherFrame to pump the message loop — the spinner stays animated
            // while we swap in content and build the first section
            var frame = new System.Windows.Threading.DispatcherFrame();

            _window.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    // Swap in the real content — replaces the loading overlay.
                    // WPF doesn't render mid-callback, so the user goes directly
                    // from seeing the spinner to seeing the completed first tab.
                    _perfTracer?.BeginPhase("SwapContent (real layout)");
                    if (_contentPanel != null)
                        _window.Content = _contentPanel;

                    // Build the first (eager) section
                    if (!string.IsNullOrEmpty(firstTabId) && _tabManager != null)
                    {
                        _perfTracer?.BeginPhase($"Section (first tab): {firstTabId}");
                        _tabManager.BuildFirstDeferred(firstTabId);
                    }

                    _perfTracer?.EndPhase();
                    _perfTracer?.LogSummary();

                    // Exit the frame — this lets ShowUIWithLoadingOverlay return
                    frame.Continue = false;
                }));

            // Pump messages until the content is built — spinner animates during this time
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }
    }
}
