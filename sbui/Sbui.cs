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

namespace Sbui
{
    public class Sbui : IRenderContext
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
        private System.Windows.Controls.ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private readonly List<PendingItem> _pendingItems;
        private bool _dirty;
        private readonly ControlRegistry _controlRegistry = new ControlRegistry();
        private readonly SettingsSynchronizer _synchronizer = new SettingsSynchronizer();
        private Dictionary<string, StackPanel> _dynamicTextboxPanels = new Dictionary<string, StackPanel>();

        private string _currentVisibilityKey;
        private StackPanel _visibilityOverridePanel;

        public static void SetLogCallback(Action<string> callback)
        {
            WindowLifecycleManager.SetLogCallback(callback);
        }

        /// <summary>
        /// Optional callback when window closes - receives (width, height) for persistence (e.g. via CPH.SetGlobalVar).
        /// </summary>
        public static void SetWindowClosedCallback(Action<double, double> callback)
        {
            WindowLifecycleManager.SetWindowClosedCallback(callback);
        }

        /// <summary>
        /// Returns true if the UI is already open. Call before creating a new Sbui to avoid duplicate windows.
        /// </summary>
        public static bool AlreadyOpened(string title = "Sbui", string version = "1.0")
        {
            return WindowLifecycleManager.AlreadyOpened(title, version);
        }

        public static bool IsOpen => WindowLifecycleManager.IsOpen;

        private void LogInternal(string message)
        {
            WindowLifecycleManager.Log(message);
        }

        StackPanel IRenderContext.GetPanel(string tabName)
        {
            EnsureTabExists(tabName);
            return _visibilityOverridePanel ?? _tabManager?.GetPanel(tabName);
        }
        JObject IRenderContext.Settings => _existingSettings;
        ControlRegistry IRenderContext.Registry => _controlRegistry;
        void IRenderContext.MarkDirty() => MarkDirty();
        FluentWindow IRenderContext.Window => _window;
        void IRenderContext.Log(string message) => LogInternal(message);
        void IRenderContext.UpdateDropdown(string key, string[] options, int selectedIndex) => UpdateDropdown(key, options, selectedIndex);
        IDictionary<string, StackPanel> IRenderContext.DynamicTextboxPanels => _dynamicTextboxPanels;
        void IRenderContext.SetVisibilityOverridePanel(StackPanel panel) => _visibilityOverridePanel = panel;
        void IRenderContext.ClearVisibilityOverridePanel() => _visibilityOverridePanel = null;

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
            _settingsManager = new SettingsManager(cph, _settingsKey);
            _existingSettings = _settingsManager.Load();
            _pendingItems = new List<PendingItem>();

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

                if (!WindowLifecycleManager.InitializeWindow(CreateAndConfigureWindow, 5000))
                    Log("WARNING: Window creation timed out");

                _window = WindowLifecycleManager.Window;
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
                    _existingSettings[kv.Key] = kv.Value;
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
            _synchronizer.LoadSettingsIntoControls(_mainGrid, _existingSettings);
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void LoadSettings()
        {
            _existingSettings = _settingsManager.Load();
        }

        private FluentWindow CreateAndConfigureWindow()
        {
            try
            {
                Log("InitializeWindow: Starting window creation");
                var builder = new WindowBuilder(_extensionName, _displayVersion ?? "", _existingSettings);
                _window = builder.Build(
                    out _mainGrid,
                    out _tabManager,
                    out _sidebar,
                    out _contentScrollViewer,
                    onSave: SaveValuesInternal,
                    onSaveAndExit: () => { SaveValuesInternal(); _window?.Close(); },
                    onReset: HandleReset,
                    onExit: HandleExit
                );
                ApplicationThemeManager.Apply((ApplicationTheme)1, (WindowBackdropType)2, true);
                ApplicationThemeManager.Apply((FrameworkElement)_window);
                Log("InitializeWindow: Window created");
                return _window;
            }
            catch (Exception ex)
            {
                Log($"ERROR in CreateAndConfigureWindow: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
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

        private void EnsureTabExists(string tabName)
        {
            _tabManager?.EnsureTab(tabName);
        }

        private void BuildContentFromPending(IReadOnlyList<PendingItem> items)
        {
            if (_mainGrid == null || _tabManager == null || items == null) return;
            _controlRegistry.Clear();
            _dynamicTextboxPanels?.Clear();

            var builder = new ContentBuilder(this);
            builder.BuildFromPendingItems(items, _mainGrid, out _);
            _mainGrid?.UpdateLayout();
        }

        /// <summary>
        /// Updates a dropdown (e.g. from AddRefreshableDropdown) with new options and selected index. Must be called on UI thread or via Dispatcher.
        /// </summary>
        public void UpdateDropdown(string key, string[] options, int selectedIndex)
        {
            if (_window == null) return;
            _window.Dispatcher.Invoke(() =>
            {
                var cb = _controlRegistry.Get<System.Windows.Controls.ComboBox>(key);
                if (cb == null) return;
                options = options ?? Array.Empty<string>();
                cb.ItemsSource = options;
                cb.SelectedIndex = Math.Max(0, Math.Min(selectedIndex, options.Length > 0 ? options.Length - 1 : 0));
            });
        }

        private JObject BuildSettings()
        {
            return _mainGrid != null ? _synchronizer.ExtractSettingsFromControls(_mainGrid) : new JObject();
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
            if (_window == null) return default;
            T result = default;
            _window.Dispatcher.Invoke(() => result = _controlRegistry.GetValue<T>(key));
            return result;
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
                Log($"ShowUI: Tab panels count = {_tabManager?.TabContentPanels?.Count}");
                
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
}
