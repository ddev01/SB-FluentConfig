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
using Sbui.Core;
using Sbui.Components;
using Sbui.Elements;
using Sbui.Helpers;
using SettingsPathHelper = Sbui.Core.SettingsPathHelper;

namespace Sbui
{
    public partial class Sbui : IRenderContext
    {
        private FluentWindow _window;
        private readonly IInlineInvokeProxy _cph;
        private readonly string _extensionName;
        private readonly string _displayVersion;
        private readonly string _settingsKey;
        private JObject _existingSettings;
        private readonly bool _withUi;
        private readonly SettingsManager _settingsManager;
        private TabManager _tabManager;
        private Grid _mainGrid;
        private ContentControl _headerPlaceholder;
        private System.Windows.Controls.ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
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
        private readonly SbuiPanelContext _sbuiPanelContext;


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
            SbuiWindowManager.SetWindowClosedCallback(callback);
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new Sbui to avoid duplicate windows.
        /// </summary>
        public static bool AlreadyOpened(string title = "Sbui", string version = "1.0")
        {
            return SbuiWindowManager.AlreadyOpened(title, version, LogInternal);
        }

        public static bool IsOpen => SbuiWindowManager.IsOpen;

        internal static void LogInternal(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                System.Diagnostics.Debug.WriteLine($"[Sbui] {message}");
        }

        StackPanel IRenderContext.GetPanel(string tabName)
        {
            var panel = _sbuiPanelContext.GetTargetPanel(tabName);
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
        FluentWindow IRenderContext.Window => _window;
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
            DialogHelper.Toast((Window)_window, message);
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
            _settingsManager = new SettingsManager(cph, _settingsKey, LogInternal);
            _existingSettings = _settingsManager.Load();
            _sbuiPanelContext = new SbuiPanelContext(this);

            if (!_withUi) return;

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException(
                    "Sbui requires the extension thread to be STA. " +
                    "Current apartment state: " + Thread.CurrentThread.GetApartmentState());
            }

            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            var builder = new WindowBuilder(_extensionName, _displayVersion ?? "", _existingSettings);
            _window = builder.Build(
                out _mainGrid,
                out _headerPlaceholder,
                out _tabManager,
                out _sidebar,
                out _contentScrollViewer,
                onSave: SaveValuesInternal,
                onSaveAndExit: () => { SaveValuesInternal(); _window?.Close(); },
                onReset: HandleReset,
                onExit: HandleExit
            );
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
            ApplicationThemeManager.Apply((FrameworkElement)_window);

            _window.Closed += (s, e) =>
            {
                if (_window?.WindowState == WindowState.Normal)
                    SbuiWindowManager.InvokeWindowClosedCallback(_window.Width, _window.Height);
                SbuiWindowManager.SetOpened(false);
                LogInternal("Sbui UI has been closed.");
            };

            SbuiWindowManager.SetOpened(true);
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
        /// Temporarily pushes a panel so all Add* calls add to it. Use for dynamic content (e.g. when adding new alias sections from pill callback).
        /// </summary>
        public void WithPanel(Panel panel, Action content)
        {
            _sbuiPanelContext.WithPanel(panel, content);
        }

        /// <summary>
        /// Gets the full saveKey including any context prefix (e.g. repeatable row).
        /// </summary>
        internal string GetFullSaveKey(string saveKey)
        {
            return _sbuiPanelContext.GetFullSaveKey(saveKey);
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

            var panel = (Panel)_sbuiPanelContext.GetTargetPanel(tabName);
            _sbuiPanelContext.PushPanel(container);
            addContent();
            _sbuiPanelContext.PopPanel();

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
            _sbuiPanelContext.WithVisibility(toggleSaveKey, tabName, content, inverted);
        }

        /// <summary>
        /// Single code path: add an element to the given tab (or current panel when inside WithPanel). Used by fluent builders.
        /// </summary>
        internal void AddElement(string tabName, Elements.UIElement element, string showWhenKey = null)
        {
            if (element == null) return;
            element.TabName = tabName ?? "";
            if (!string.IsNullOrEmpty(showWhenKey))
            {
                AddWithOptionalVisibility(showWhenKey, tabName, () => element.Render(this));
                return;
            }
            EnsureTabExists(tabName);
            element.Render(this);
        }

        /// <summary>
        /// Visibility block that builds content with a PanelBuilder (fluent DSL). Toggle must exist.
        /// </summary>
        public void WithVisibility(string toggleSaveKey, string tabName, bool inverted, Action<PanelBuilder> build)
        {
            _sbuiPanelContext.WithVisibility(toggleSaveKey, tabName, inverted, build);
        }

        /// <summary>
        /// Repeatable rows: buildRow receives a PanelBuilder for each row's content (fluent DSL).
        /// </summary>
        public void WithRepeatableRows(string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            _sbuiPanelContext.WithRepeatableRows(saveKey, tabName, buildRow);
        }

        /// <summary>
        /// Add a header image at the top of the UI. Only one header is shown; multiple calls result in the last URL being used.
        /// </summary>
        public void AddHeader(string imageUrl)
        {
            if (_headerPlaceholder == null || string.IsNullOrEmpty(imageUrl)) return;
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            try
            {
                var bi = new System.Windows.Media.Imaging.BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bi.EndInit();
                headerPanel.Children.Add(new System.Windows.Controls.Image
                {
                    Source = bi,
                    MaxHeight = 120,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                });
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
            DialogHelper.AddPopupWindow((Window)_window, title, message);
        }

        /// <summary>
        /// Shows a confirm dialog; returns MessageBoxResult (Yes/No). Call from non-UI thread via Dispatcher.
        /// </summary>
        public System.Windows.MessageBoxResult ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_window == null) return System.Windows.MessageBoxResult.None;
            return DialogHelper.ShowConfirmDialog((Window)_window, title, message, yesButton, noButton);
        }

        /// <summary>
        /// Shows a progress window for long-running work. Returns an object with Report(int) and Close().
        /// </summary>
        public IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            if (_window == null) return null;
            return ProgressWindowHelper.Show((Window)_window, title, message, progressLabel, total);
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

        public void ShowUI()
        {
            if (_window == null)
            {
                Log("ShowUI called but window is null. Was constructor called with withUi=false?");
                return;
            }
            _window.Show();
            _window.Activate();
        }
    }
}
