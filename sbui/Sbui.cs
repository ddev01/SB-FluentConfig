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
        private bool _dirty;
        private readonly ControlRegistry _controlRegistry = new ControlRegistry();
        private readonly SettingsSynchronizer _synchronizer = new SettingsSynchronizer();
        private Dictionary<string, StackPanel> _dynamicTextboxPanels = new Dictionary<string, StackPanel>();

        private static bool _anyWindowOpen;
        private StackPanel _visibilityOverridePanel;

        // Context stacks for nested content (WithRepeatableRows, WithVisibility)
        private Stack<Panel> _panelContext = new Stack<Panel>();
        private Stack<string> _saveKeyContext = new Stack<string>();

        private static Action<string> _logCallback;
        private static Action<double, double> _windowClosedCallback;

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
            if (!_anyWindowOpen) return false;
            LogInternal($"UI ({title} (v{version})) already open, skipping...");
            return true;
        }

        public static bool IsOpen => _anyWindowOpen;

        private static void LogInternal(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                System.Diagnostics.Debug.WriteLine($"[Sbui] {message}");
        }

        StackPanel IRenderContext.GetPanel(string tabName)
        {
            var panel = GetTargetPanel(tabName);
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

            _window.Closed += (s, e) =>
            {
                if (_window?.WindowState == WindowState.Normal && _windowClosedCallback != null)
                    _windowClosedCallback(_window.Width, _window.Height);
                _anyWindowOpen = false;
                LogInternal("Sbui UI has been closed.");
            };

            _anyWindowOpen = true;
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
                    SetNestedValue(_existingSettings, kv.Key, kv.Value);
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

        /// <summary>
        /// Temporarily pushes a panel so all Add* calls add to it. Use for dynamic content (e.g. when adding new alias sections from pill callback).
        /// </summary>
        public void WithPanel(Panel panel, Action content)
        {
            if (panel == null || content == null) return;
            _panelContext.Push(panel);
            try { content(); }
            finally { _panelContext.Pop(); }
        }

        /// <summary>
        /// Gets the target panel for the next Add* call.
        /// Returns the top of the context stack if it exists,
        /// otherwise returns the current tab's panel.
        /// </summary>
        private Panel GetTargetPanel(string tabName)
        {
            if (_panelContext.Count > 0)
                return _panelContext.Peek();
            EnsureTabExists(tabName);
            return _visibilityOverridePanel ?? (Panel)_tabManager?.GetPanel(tabName);
        }

        /// <summary>
        /// Gets the full saveKey including any context prefix (e.g. repeatable row).
        /// </summary>
        internal string GetFullSaveKey(string saveKey)
        {
            return _saveKeyContext.Count > 0
                ? $"{_saveKeyContext.Peek()}.{saveKey}"
                : saveKey;
        }

        /// <summary>
        /// Updates a dropdown (e.g. from AddRefreshableDropdown) with new options and selected index.
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

        private JObject BuildSettings()
        {
            return _mainGrid != null ? _synchronizer.ExtractSettingsFromControls(_mainGrid) : null;
        }

        private static void SetNestedValue(JObject root, string path, JToken value)
        {
            if (root == null || string.IsNullOrEmpty(path)) return;
            if (!path.Contains("[") && !path.Contains("."))
            {
                root[path] = value;
                return;
            }
            var parts = ParseNestedPath(path);
            if (parts.Length == 0) return;
            JToken current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                var nextIsIndex = i + 1 < parts.Length && int.TryParse(parts[i + 1], out _);
                if (int.TryParse(part, out int index))
                {
                    if (!(current is JArray arr)) return;
                    while (arr.Count <= index) arr.Add(new JObject());
                    current = arr[index];
                }
                else if (current is JObject obj)
                {
                    if (obj[part] == null)
                        obj[part] = nextIsIndex ? (JToken)new JArray() : new JObject();
                    current = obj[part];
                }
                else return;
            }
            var last = parts[parts.Length - 1];
            if (int.TryParse(last, out int lastIdx))
            {
                if (!(current is JArray arr)) return;
                while (arr.Count <= lastIdx) arr.Add(null);
                arr[lastIdx] = value;
            }
            else if (current is JObject obj)
            {
                obj[last] = value;
            }
        }

        private static string[] ParseNestedPath(string path)
        {
            var parts = new List<string>();
            var current = "";
            bool inBracket = false;
            foreach (var ch in path)
            {
                if (ch == '[')
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                    inBracket = true;
                }
                else if (ch == ']')
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                    inBracket = false;
                }
                else if (ch == '.' && !inBracket)
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                }
                else
                    current += ch;
            }
            if (!string.IsNullOrEmpty(current)) parts.Add(current);
            return parts.ToArray();
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

            var panel = (Panel)GetTargetPanel(tabName);
            _panelContext.Push(container);
            addContent();
            _panelContext.Pop();

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
            if (content == null) return;
            var toggle = _controlRegistry.Get<ToggleSwitch>(toggleSaveKey);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{toggleSaveKey}' must be added before WithVisibility block");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = (toggle.IsChecked == true) != inverted ? Visibility.Visible : Visibility.Collapsed;

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            content();
            _panelContext.Pop();

            toggle.Checked += (s, e) => container.Visibility = inverted ? Visibility.Collapsed : Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = inverted ? Visibility.Visible : Visibility.Collapsed;

            if (currentPanel != null)
                currentPanel.Children.Add(container);
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
            if (build == null) return;
            var toggle = _controlRegistry.Get<ToggleSwitch>(toggleSaveKey);
            if (toggle == null)
                throw new InvalidOperationException($"Toggle '{toggleSaveKey}' must be added before WithVisibility block");
            var container = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            container.Visibility = (toggle.IsChecked == true) != inverted ? Visibility.Visible : Visibility.Collapsed;

            var currentPanel = GetTargetPanel(tabName);
            _panelContext.Push(container);
            var pb = new PanelBuilder(this, container, tabName);
            build(pb);
            pb.FlushPending();
            _panelContext.Pop();

            toggle.Checked += (s, e) => container.Visibility = inverted ? Visibility.Collapsed : Visibility.Visible;
            toggle.Unchecked += (s, e) => container.Visibility = inverted ? Visibility.Visible : Visibility.Collapsed;

            if (currentPanel != null)
                currentPanel.Children.Add(container);
        }

        /// <summary>
        /// Repeatable rows: buildRow receives a PanelBuilder for each row's content (fluent DSL).
        /// </summary>
        public void WithRepeatableRows(string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            if (buildRow == null) return;
            var panel = GetTargetPanel(tabName);
            if (panel == null) return;

            var container = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            var rowsData = LoadRowsData(saveKey);

            for (int i = 0; i < rowsData.Count; i++)
            {
                var (outerRow, contentPanel) = CreateRowPanel(i, saveKey, container);
                _panelContext.Push(contentPanel);
                _saveKeyContext.Push($"{saveKey}[{i}]");
                var rowPb = new PanelBuilder(this, contentPanel, tabName);
                buildRow(rowPb);
                rowPb.FlushPending();
                _saveKeyContext.Pop();
                _panelContext.Pop();
                container.Children.Add(outerRow);
            }

            var addBtn = new Wpf.Ui.Controls.Button
            {
                Content = "+ Add Row",
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(15, 5, 15, 5)
            };
            addBtn.Click += (s, e) => AddRowWithBuilder(container, saveKey, tabName, buildRow);
            container.Children.Add(addBtn);

            panel.Children.Add(container);
        }

        private void AddRowWithBuilder(Panel container, string saveKey, string tabName, Action<PanelBuilder> buildRow)
        {
            int newIndex = Math.Max(0, container.Children.Count - 1);
            var (outerRow, contentPanel) = CreateRowPanel(newIndex, saveKey, container);
            _panelContext.Push(contentPanel);
            _saveKeyContext.Push($"{saveKey}[{newIndex}]");
            var rowPb = new PanelBuilder(this, contentPanel, tabName);
            buildRow(rowPb);
            rowPb.FlushPending();
            _saveKeyContext.Pop();
            _panelContext.Pop();
            container.Children.Insert(container.Children.Count - 1, outerRow);
        }

        /// <summary>
        /// Add a header image at the top of the UI. Only one header is shown; multiple calls result in the last URL being used.
        /// </summary>
        public void AddHeader(string imageUrl)
        {
            if (_mainGrid == null || string.IsNullOrEmpty(imageUrl)) return;
            _mainGrid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
            foreach (System.Windows.UIElement child in _mainGrid.Children)
            {
                var row = Grid.GetRow(child as FrameworkElement);
                if (row >= 0) Grid.SetRow(child as FrameworkElement, row + 1);
            }
            try
            {
                var bi = new System.Windows.Media.Imaging.BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bi.EndInit();
                var img = new System.Windows.Controls.Image
                {
                    Source = bi,
                    MaxHeight = 120,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                headerPanel.Children.Add(img);
                Grid.SetRow(headerPanel, 0);
                _mainGrid.Children.Insert(0, headerPanel);
            }
            catch
            {
                var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                headerPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "[Header image]",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 4)
                });
                Grid.SetRow(headerPanel, 0);
                _mainGrid.Children.Insert(0, headerPanel);
            }
        }

        private (DockPanel outerRow, StackPanel contentPanel) CreateRowPanel(int rowIndex, string saveKey, Panel container)
        {
            var outerRow = new DockPanel
            {
                Margin = new Thickness(0, 5, 0, 5),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
            };
            outerRow.Tag = new RowTag { RowIndex = rowIndex, SaveKey = saveKey };

            var deleteBtn = new Wpf.Ui.Controls.Button
            {
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = "×",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                MinWidth = 30,
                MinHeight = 30
            };
            System.Windows.Controls.Panel.SetZIndex(deleteBtn, 10);

            var contentPanel = new StackPanel { Margin = new Thickness(10) };
            deleteBtn.Click += (s, e) =>
            {
                container.Children.Remove(outerRow);
                RenumberRows(container, saveKey);
                DeleteRowData(saveKey, rowIndex);
                MarkDirty();
            };
            DockPanel.SetDock(deleteBtn, Dock.Right);
            outerRow.Children.Add(deleteBtn);
            outerRow.Children.Add(contentPanel);

            return (outerRow, contentPanel);
        }

        private void RenumberRows(Panel container, string saveKey)
        {
            int index = 0;
            foreach (var child in container.Children)
            {
                if (child is DockPanel dock && dock.Tag is RowTag tag)
                {
                    tag.RowIndex = index++;
                }
            }
        }

        private void DeleteRowData(string saveKey, int rowIndex)
        {
            var arr = LoadRowsData(saveKey);
            if (rowIndex >= 0 && rowIndex < arr.Count)
            {
                arr.RemoveAt(rowIndex);
                if (_existingSettings == null) _existingSettings = new JObject();
                _existingSettings[saveKey] = arr;
                MarkDirty();
            }
        }

        private JArray LoadRowsData(string saveKey)
        {
            if (_existingSettings == null) return new JArray();
            var token = _existingSettings[saveKey];
            if (token is JArray arr) return arr;
            if (token != null)
            {
                try { return JArray.Parse(token.ToString()); }
                catch { }
            }
            return new JArray();
        }

        private class RowTag
        {
            public int RowIndex;
            public string SaveKey;
        }

        public T GetValue<T>(string key)
        {
            var token = _existingSettings?[key];
            if (token == null) return default;
            try { return token.ToObject<T>(); }
            catch { return default; }
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
            return new ProgressReporterImpl(progressWindow, progressBar, total);
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
