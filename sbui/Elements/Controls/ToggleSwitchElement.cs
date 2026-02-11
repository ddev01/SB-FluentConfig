using System;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Sbui.Components;
using Wpf.Ui.Controls;

namespace Sbui.Elements
{
    public class ToggleSwitchElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public bool DefaultValue { get; set; }

        public ToggleSwitchElement(string title, string description, string tabName, string saveKey, bool defaultValue, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            DefaultValue = defaultValue;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            bool isChecked = DefaultValue;
            var settingToken = context.GetSetting(SaveKey);
            if (settingToken != null)
            {
                try { isChecked = settingToken.ToObject<bool>(); }
                catch (Exception ex) { context.Log($"ToggleSwitchElement: failed to parse bool for '{SaveKey}': {ex.Message}"); }
            }
            var ts = SbuiComponentFactory.CreateToggleSwitch(SaveKey, isChecked);
            ts.Checked += (s, e) => context.MarkDirty();
            ts.Unchecked += (s, e) => context.MarkDirty();
            context.Registry.Register(SaveKey, ts);
            stack.Children.Add(ts);
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            panel.Children.Add(stack);
        }
    }
}
