using System.Windows.Controls;
using Sbui.Components;

namespace Sbui.Elements
{
    public class ResponseBoxElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string DefaultText { get; set; }

        public ResponseBoxElement(string title, string description, string tabName, string saveKey, string defaultText, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            DefaultText = defaultText ?? "";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var initial = context.GetSetting(SaveKey)?.ToString() ?? DefaultText ?? "";
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var tb = SbuiComponentFactory.CreateResponseBox(SaveKey, initial);
            tb.TextChanged += (s, e) => context.MarkDirty();
            context.Registry.Register(SaveKey, tb);
            stack.Children.Add(tb);
            panel.Children.Add(stack);
        }
    }
}
