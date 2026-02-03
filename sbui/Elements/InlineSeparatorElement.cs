using System.Windows.Controls;
using Sbui.Components;

namespace Sbui.Elements
{
    public class InlineSeparatorElement : UIElement
    {
        public InlineSeparatorElement(string tabName, string visibilityKey = null)
        {
            TabName = tabName ?? "";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            panel.Children.Add(SbuiComponentFactory.CreateInlineSeparator());
        }
    }
}
