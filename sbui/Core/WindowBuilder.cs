using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Sbui.Components;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    /// <summary>
    /// Builds the FluentWindow with dock layout, main grid, sidebar, tab container, ScrollViewer, and footer buttons.
    /// Theme is not applied here; the caller applies it after Build.
    /// </summary>
    public class WindowBuilder
    {
        private readonly string _title;
        private readonly JObject _settings;

        public WindowBuilder(string extensionName, string displayVersion, JObject settings)
        {
            _title = !string.IsNullOrEmpty(displayVersion)
                ? $"{extensionName} (v{displayVersion})"
                : extensionName;
            _settings = settings;
        }

        public FluentWindow Build(
            out Grid mainGrid,
            out ContentControl headerPlaceholder,
            out TabManager tabManager,
            out System.Windows.Controls.ListBox sidebar,
            out ScrollViewer contentScrollViewer,
            Action onSave,
            Action onSaveAndExit,
            Action onReset,
            Action onExit)
        {
            var window = CreateWindow();
            var mainDock = CreateMainLayout(out mainGrid, out headerPlaceholder, out tabManager, out sidebar, out contentScrollViewer);
            var footer = CreateFooter(onSave, onSaveAndExit, onReset, onExit);

            DockPanel.SetDock(footer, Dock.Bottom);
            mainDock.Children.Insert(0, footer);
            mainDock.Children.Add(mainGrid);

            window.Content = mainDock;
            return window;
        }

        private FluentWindow CreateWindow()
        {
            var window = new FluentWindow
            {
                Title = _title,
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
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

            headerPlaceholder = new ContentControl { Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(headerPlaceholder, 0);
            mainGrid.Children.Add(headerPlaceholder);

            var sidebarContentGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 160 });
            sidebarContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            sidebar = CreateStyledSidebar();
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

        private static System.Windows.Controls.ListBox CreateStyledSidebar()
        {
            var sidebar = new System.Windows.Controls.ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                Padding = new Thickness(4, 2, 8, 2),
                MinWidth = 140
            };
            var listItemStyle = new Style(typeof(ListBoxItem));
            listItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 10, 12, 10)));
            listItemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(4, 2, 4, 2)));
            listItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Colors.White)));
            listItemStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            listItemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            listItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            listItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
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
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x26, 0x2d, 0x3d))));
            listItemStyle.Triggers.Add(hoverTrigger);
            var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            var selectedGradient = new LinearGradientBrush(
                Color.FromRgb(0x2d, 0x35, 0x4a),
                Color.FromRgb(0x22, 0x28, 0x38),
                new Point(0, 0),
                new Point(1, 1));
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedGradient));
            selectedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x63, 0x6b, 0x9a))));
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
