using System.Windows.Controls;
using Sbui.Components;

namespace Sbui.Elements
{
    public class TextboxElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string DefaultText { get; set; }
        public bool IsPassword { get; set; }
        public bool IsMultiline { get; set; }

        public TextboxElement(string title, string description, string tabName, string saveKey, string defaultText, bool isPassword, bool isMultiline = false, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            DefaultText = defaultText ?? "";
            IsPassword = isPassword;
            IsMultiline = isMultiline;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var stack = SbuiComponentFactory.CreateTitledStack(Title, Description);
            var initial = context.GetSetting(SaveKey)?.ToString() ?? DefaultText ?? "";
            if (IsPassword)
            {
                var pb = SbuiComponentFactory.CreatePasswordBox(SaveKey, initial);
                pb.PasswordChanged += (s, e) => context.MarkDirty();
                context.Registry.Register(SaveKey, pb);
                stack.Children.Add(pb);
            }
            else if (IsMultiline)
            {
                var tb = SbuiComponentFactory.CreateResponseBox(SaveKey, initial);
                tb.TextChanged += (s, e) => context.MarkDirty();
                context.Registry.Register(SaveKey, tb);
                stack.Children.Add(tb);
            }
            else
            {
                var tb = SbuiComponentFactory.CreateTextBox(SaveKey, initial);
                tb.TextChanged += (s, e) => context.MarkDirty();
                context.Registry.Register(SaveKey, tb);
                stack.Children.Add(tb);
            }
            panel.Children.Add(stack);
        }
    }
}
