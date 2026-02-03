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
        private StackPanel _tabContainer;
        private System.Windows.Controls.ListBox _sidebar;
        private ScrollViewer _contentScrollViewer;
        private readonly List<PendingItem> _pendingItems;
        private bool _dirty;
        private readonly ControlRegistry _controlRegistry = new ControlRegistry();
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
        /// Updates _existingSettings with current window width/height and persists via SettingsManager when available.
        /// </summary>
        private void PersistWindowSize()
        {
            if (_window == null || _window.WindowState != WindowState.Normal) return;
            if (_existingSettings == null) _existingSettings = new JObject();
            _existingSettings["WindowWidth"] = _window.Width;
            _existingSettings["WindowHeight"] = _window.Height;
            var settings = BuildSettings();
            if (settings != null)
            {
                foreach (var kv in settings)
                    _existingSettings[kv.Key] = kv.Value;
            }
            _settingsManager.Save(_existingSettings);
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
            _existingSettings = _settingsManager.Load();
        }

        private FluentWindow CreateAndConfigureWindow()
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
                var hoverTrigger = new Trigger { Property = System.Windows.UIElement.IsMouseOverProperty, Value = true };
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

                _tabManager = new TabManager(_tabContainer, _sidebar);
                _tabManager.SetScrollViewer(_contentScrollViewer);

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

                Log($"InitializeWindow: Window.IsLoaded = {_window.IsLoaded}");
                Log($"InitializeWindow: Window.Visibility = {_window.Visibility}");
                Log($"InitializeWindow: Window.IsVisible = {_window.IsVisible}");
                return _window;
            }
            catch (Exception ex)
            {
                Log($"ERROR in CreateAndConfigureWindow: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
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
                    var img = new System.Windows.Controls.Image { Source = bi, MaxHeight = 120, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Stretch };
                    headerPanel.Children.Add(img);
                }
                catch
                {
                    headerPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "[Header image]", Foreground = new SolidColorBrush(Colors.Gray), FontSize = 12, Margin = new Thickness(0, 4, 0, 4) });
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
                    if (currentGroup != null && currentGroup.Count > 0) groups.Add((currentKey, currentGroup));
                    currentKey = useKey;
                    currentGroup = new List<PendingItem>();
                }
                currentGroup.Add(item);
            }
            if (currentGroup != null && currentGroup.Count > 0) groups.Add((currentKey, currentGroup));

            var ctx = (IRenderContext)this;
            foreach (var (visibilityKey, groupItems) in groups)
            {
                if (!string.IsNullOrEmpty(visibilityKey))
                {
                    var container = new StackPanel { Orientation = Orientation.Vertical };
                    _visibilityOverridePanel = container;
                    foreach (var item in groupItems)
                    {
                        var el = PendingItemToElement(item);
                        if (el != null) el.Render(ctx);
                    }
                    _visibilityOverridePanel = null;
                    if (_controlRegistry.TryGetValue(visibilityKey, out var toggleControl) && toggleControl is ToggleSwitch toggle)
                    {
                        container.Visibility = toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                        toggle.Checked += (s, e) => container.Visibility = Visibility.Visible;
                        toggle.Unchecked += (s, e) => container.Visibility = Visibility.Collapsed;
                    }
                    var tabName = GetTabNameFromItem(groupItems[0]);
                    EnsureTabExists(tabName);
                    if (_tabManager != null && _tabManager.GetPanel(tabName) is StackPanel tabPanel)
                        tabPanel.Children.Add(container);
                }
                else
                {
                    foreach (var item in groupItems)
                    {
                        var el = PendingItemToElement(item);
                        if (el != null) el.Render(ctx);
                    }
                }
            }
            _mainGrid?.UpdateLayout();
        }

        private static Elements.UIElement PendingItemToElement(PendingItem item)
        {
            if (item?.Args == null) return null;
            var args = item.Args;
            var vk = item.VisibilityKey;
            switch (item.Kind)
            {
                case PendingKind.Header: return null;
                case PendingKind.Title: return new TitleElement((string)args[0], (string)args[1], vk);
                case PendingKind.Description: return new DescriptionElement((string)args[0], (string)args[1], vk);
                case PendingKind.ToggleSwitch: return new ToggleSwitchElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (bool)args[4], vk);
                case PendingKind.Textbox: return new TextboxElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (bool)args[5], vk);
                case PendingKind.Slider: return new SliderElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], vk);
                case PendingKind.InlineSeparator: return new InlineSeparatorElement((string)args[0], vk);
                case PendingKind.SliderWithToggleSwitch: return new SliderWithToggleSwitchElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], (bool)args[7], vk);
                case PendingKind.Filepath: return new FilepathElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                case PendingKind.ClickableButton: return new ClickableButtonElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (Action)args[5], vk);
                case PendingKind.RefreshableDropdown: return new RefreshableDropdownElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (Func<string[]>)args[5], (int)args[6], vk);
                case PendingKind.ResponseBox: return new ResponseBoxElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                case PendingKind.DecimalStepper: return new DecimalStepperElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (double)args[4], (double)args[5], (double)args[6], (double)args[7], vk);
                case PendingKind.CompetingToggleSwitches: return new CompetingToggleSwitchesElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (int)args[5], vk);
                case PendingKind.DynamicTextboxesWithPreset: return new DynamicTextboxesWithPresetElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], vk);
                case PendingKind.ColorPicker: return new ColorPickerElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                default: return null;
            }
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

        private static StackPanel GetDynamicListPanel(StackPanel outer)
        {
            foreach (var c in outer.Children)
                if (c is StackPanel inner && inner.Children.OfType<System.Windows.Controls.TextBox>().Any())
                    return inner;
            return null;
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
