using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sbui.Elements
{
    public class TitleElement : UIElement
    {
        public string Text { get; set; }

        public TitleElement(string text, string tabName, string visibilityKey = null)
        {
            Text = text;
            TabName = tabName ?? "";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = Text ?? "",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new System.Windows.Thickness(0, 12, 0, 8),
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }
    }
}
