using System;
using System.Windows.Controls;
using System.Windows.Media;
using FluentConfig.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace FluentConfig.Elements
{
    public class ClickableButtonElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ConfirmText { get; set; }
        public string Color { get; set; }
        public Action Callback { get; set; }

        public ClickableButtonElement(string title, string description, string confirmText, string color, string tabName, Action callback, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            ConfirmText = confirmText ?? "OK";
            Color = color ?? "";
            TabName = tabName ?? "";
            Callback = callback;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var stack = FluentConfigComponentFactory.CreateTitledStack(Title, Description);
            var btn = new Button { Content = ConfirmText, Margin = new System.Windows.Thickness(0, 4, 0, 0), Padding = new System.Windows.Thickness(16, 8, 16, 8) };
            try
            {
                if (!string.IsNullOrEmpty(Color) && Color.StartsWith("#"))
                {
                    var brush = (Brush)new BrushConverter().ConvertFrom(Color);
                    if (brush != null) btn.Background = brush;
                }
            }
            catch (Exception ex) { context.Log($"Button color parse error: {ex.Message}"); }
            var window = context.Window;
            var callback = Callback;
            btn.Click += (s, e) =>
            {
                try
                {
                    if (window != null && !window.Dispatcher.CheckAccess())
                        window.Dispatcher.Invoke(callback);
                    else
                        callback?.Invoke();
                }
                catch (Exception ex)
                {
                    context.Log($"Button callback error: {ex.Message}");
                }
            };
            stack.Children.Add(btn);
            panel.Children.Add(stack);
        }
    }
}
