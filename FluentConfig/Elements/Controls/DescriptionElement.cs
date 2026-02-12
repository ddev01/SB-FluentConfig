using System.Windows;
using System.Windows.Controls;
using FluentConfig.Components;

namespace FluentConfig.Elements
{
    public class DescriptionElement : UIElement
    {
        public string Text { get; set; }

        public DescriptionElement(string text, string tabName, string visibilityKey = null)
        {
            Text = text;
            TabName = tabName ?? "";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var tb = new System.Windows.Controls.TextBlock
            {
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 4, 0, 12),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };
            foreach (var inline in RichTextParser.Parse(Text ?? ""))
                tb.Inlines.Add(inline);
            panel.Children.Add(tb);
        }
    }
}
