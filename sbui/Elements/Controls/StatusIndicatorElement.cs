using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sbui.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    /// <summary>
    /// Renders a status indicator (colored dot) with optional label and button. Status can be updated via Sbui.SetStatusIndicator(key, "Green"|"Red") or UiContext.SetStatus(key, "Green").
    /// </summary>
    public class StatusIndicatorElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string InitialStatus { get; set; }
        public string ButtonText { get; set; }
        public Action<UiContext> OnClickCallback { get; set; }
        public Action<UiContext> OnLoadCheck { get; set; }

        public StatusIndicatorElement(string title, string description, string tabName, string saveKey, string initialStatus, string buttonText, Action<UiContext> onClick, Action<UiContext> onLoadCheck, string visibilityKey = null)
        {
            Title = title ?? "";
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            InitialStatus = initialStatus ?? "Red";
            ButtonText = buttonText;
            OnClickCallback = onClick;
            OnLoadCheck = onLoadCheck;
            VisibilityKey = visibilityKey;
        }

        private static Brush GetStatusBrush(string status)
        {
            if (string.IsNullOrEmpty(status)) return Brushes.Red;
            if (status.Equals("Green", StringComparison.OrdinalIgnoreCase)) return Brushes.LimeGreen;
            if (status.Equals("Red", StringComparison.OrdinalIgnoreCase)) return Brushes.Red;
            return Brushes.Red;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            var stack = SbuiComponentFactory.CreateTitledStack(Title ?? "", Description);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var statusBrush = GetStatusBrush(InitialStatus);
            var indicator = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = statusBrush,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = SaveKey
            };
            context.Registry.Register(SaveKey, indicator);
            row.Children.Add(indicator);

            if (!string.IsNullOrEmpty(ButtonText) && OnClickCallback != null)
            {
                var btn = new Button { Content = ButtonText, Margin = new Thickness(0, 2, 0, 0), Padding = new Thickness(12, 6, 12, 6) };
                var ctx = new UiContext((global::Sbui.Sbui)context);
                var callback = OnClickCallback;
                btn.Click += (s, e) =>
                {
                    try { callback(ctx); }
                    catch (Exception ex) { context.Log($"StatusIndicator button error: {ex.Message}"); }
                };
                row.Children.Add(btn);
            }

            stack.Children.Add(row);
            panel.Children.Add(stack);

            if (OnLoadCheck != null)
            {
                var ctx = new UiContext((global::Sbui.Sbui)context);
                try
                {
                    OnLoadCheck(ctx);
                }
                catch (Exception ex)
                {
                    context.Log($"StatusIndicator on-load check error: {ex.Message}");
                }
            }
        }

    }
}
