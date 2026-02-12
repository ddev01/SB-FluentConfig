using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using FluentConfig.Components;
using Wpf.Ui.Controls;

namespace FluentConfig.Core
{
    /// <summary>
    /// Builds the main Window (FluentWindow or plain Window) with dock layout, main grid,
    /// sidebar, tab container, ScrollViewer, and footer buttons.
    /// Theme is not applied here; the caller applies it after Build.
    /// </summary>
    public class WindowBuilder
    {
        private readonly string _title;
        private readonly JObject _settings;
        private readonly PerformanceTracer _tracer;
        private readonly bool _plainWindow;

        public WindowBuilder(string extensionName, string displayVersion, JObject settings)
            : this(extensionName, displayVersion, settings, null, false)
        {
        }

        internal WindowBuilder(string extensionName, string displayVersion, JObject settings, PerformanceTracer tracer, bool plainWindow = false)
        {
            _title = !string.IsNullOrEmpty(displayVersion)
                ? $"{extensionName} (v{displayVersion})"
                : extensionName;
            _settings = settings;
            _tracer = tracer;
            _plainWindow = plainWindow;
        }

        public Window Build(
            out Grid mainGrid,
            out ContentControl headerPlaceholder,
            out TabManager tabManager,
            out System.Windows.Controls.ListBox sidebar,
            out ScrollViewer contentScrollViewer,
            out DockPanel contentPanel,
            Action onSave,
            Action onSaveAndExit,
            Action onReset,
            Action onExit)
        {
            _tracer?.BeginPhase("WindowBuilder.CreateWindow");
            var window = CreateWindow();
            var mainDock = CreateMainLayout(out mainGrid, out headerPlaceholder, out tabManager, out sidebar, out contentScrollViewer);
            _tracer?.BeginPhase("WindowBuilder.CreateFooter");
            var footer = CreateFooter(onSave, onSaveAndExit, onReset, onExit);

            DockPanel.SetDock(footer, Dock.Bottom);
            mainDock.Children.Insert(0, footer);
            mainDock.Children.Add(mainGrid);

            // Don't set window.Content here — caller controls when to attach content
            // (allows showing window with loading overlay first, then swapping in real content)
            contentPanel = mainDock;
            return window;
        }

        private Window CreateWindow()
        {
            Window window;
            if (_plainWindow)
            {
                // Plain Window: skips Mica backdrop, custom chrome, DWM API calls, and rounded corners.
                // Much faster Show() (~800-1200ms savings) but loses the polished FluentWindow chrome.
                // All controls inside still use WPF UI styles via application-level theme resources.
                window = new Window
                {
                    Title = _title,
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2e))
                };
            }
            else
            {
                window = new FluentWindow
                {
                    Title = _title,
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
            }

            if (_settings != null)
            {
                var w = _settings["WindowWidth"];
                var h = _settings["WindowHeight"];
                if (w != null && h != null)
                {
                    if (double.TryParse(w.ToString(), out var wd) && double.TryParse(h.ToString(), out var hd) && wd > 0 && hd > 0)
                    {
                        window.Width = wd;
                        window.Height = hd;
                    }
                }
            }
            return window;
        }

        private DockPanel CreateMainLayout(
            out Grid mainGrid,
            out ContentControl headerPlaceholder,
            out TabManager tabManager,
            out System.Windows.Controls.ListBox sidebar,
            out ScrollViewer contentScrollViewer)
        {
            _tracer?.BeginPhase("WindowBuilder.DockPanel+Grid");
            var mainDock = new DockPanel
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                LastChildFill = true
            };

            mainGrid = new Grid
            {
                Margin = new Thickness(20, 20, 20, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _tracer?.BeginPhase("WindowBuilder.HeaderPlaceholder");
            headerPlaceholder = new ContentControl { Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(headerPlaceholder, 0);
            mainGrid.Children.Add(headerPlaceholder);

            _tracer?.BeginPhase("WindowBuilder.SidebarContentGrid");
            var sidebarContentGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 160 });
            sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _tracer?.BeginPhase("WindowBuilder.CreateStyledSidebar");
            sidebar = CreateStyledSidebar();
            _tracer?.BeginPhase("WindowBuilder.ScrollViewer+TabManager");
            var tabContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            contentScrollViewer = new ScrollViewer
            {
                Content = tabContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };

            Grid.SetColumn(sidebar, 0);
            Grid.SetColumn(contentScrollViewer, 1);
            sidebarContentGrid.Children.Add(sidebar);
            sidebarContentGrid.Children.Add(contentScrollViewer);

            tabManager = new TabManager(tabContainer, sidebar);
            tabManager.SetScrollViewer(contentScrollViewer);

            Grid.SetRow(sidebarContentGrid, 1);
            mainGrid.Children.Add(sidebarContentGrid);

            return mainDock;
        }

        // Cached frozen brushes for sidebar styling — avoids per-instance allocations.
        private static readonly SolidColorBrush _sidebarWhiteBrush = Freeze(new SolidColorBrush(Colors.White));
        private static readonly SolidColorBrush _sidebarHoverBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x26, 0x2d, 0x3d)));
        private static readonly SolidColorBrush _sidebarSelectedBorderBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x63, 0x6b, 0x9a)));
        private static readonly LinearGradientBrush _sidebarSelectedGradient = FreezeGradient(
            Color.FromRgb(0x2d, 0x35, 0x4a),
            Color.FromRgb(0x22, 0x28, 0x38));

        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }
        private static LinearGradientBrush FreezeGradient(Color c1, Color c2)
        {
            var b = new LinearGradientBrush(c1, c2, new Point(0, 0), new Point(1, 1));
            b.Freeze();
            return b;
        }

        /// <summary>
        /// Creates the sidebar ListBox with a simplified style that avoids expensive FrameworkElementFactory/ControlTemplate.
        /// Uses only Setters and Triggers on the default ListBoxItem template for faster initialization.
        /// </summary>
        private static System.Windows.Controls.ListBox CreateStyledSidebar()
        {
            var sidebar = new System.Windows.Controls.ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = _sidebarWhiteBrush,
                FontSize = 14,
                Padding = new Thickness(4, 2, 8, 2),
                MinWidth = 140
            };

            var listItemStyle = new Style(typeof(ListBoxItem));
            listItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 10, 12, 10)));
            listItemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(4, 2, 4, 2)));
            listItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, _sidebarWhiteBrush));
            listItemStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            listItemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            listItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            listItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, _sidebarHoverBrush));
            listItemStyle.Triggers.Add(hoverTrigger);

            var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, _sidebarSelectedGradient));
            selectedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, _sidebarSelectedBorderBrush));
            selectedTrigger.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(3, 0, 0, 0)));
            listItemStyle.Triggers.Add(selectedTrigger);

            sidebar.ItemContainerStyle = listItemStyle;
            return sidebar;
        }

        private static StackPanel CreateFooter(Action onSave, Action onSaveAndExit, Action onReset, Action onExit)
        {
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 12, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var saveBtn = new Wpf.Ui.Controls.Button { Content = "Save", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
            saveBtn.Click += (s, e) => onSave?.Invoke();
            var saveExitBtn = new Wpf.Ui.Controls.Button { Content = "Save & Exit", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
            saveExitBtn.Click += (s, e) => onSaveAndExit?.Invoke();
            var resetBtn = new Wpf.Ui.Controls.Button { Content = "Reset", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
            resetBtn.Click += (s, e) => onReset?.Invoke();
            var exitBtn = new Wpf.Ui.Controls.Button { Content = "Exit", Padding = new Thickness(16, 8, 16, 8) };
            exitBtn.Click += (s, e) => onExit?.Invoke();
            footer.Children.Add(saveBtn);
            footer.Children.Add(saveExitBtn);
            footer.Children.Add(resetBtn);
            footer.Children.Add(exitBtn);
            return footer;
        }
    }
}
