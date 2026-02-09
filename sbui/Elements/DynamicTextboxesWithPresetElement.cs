using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Sbui.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class DynamicTextboxesWithPresetElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string[] PresetValues { get; set; }

        public DynamicTextboxesWithPresetElement(string title, string description, string tabName, string saveKey, string[] presetValues, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            PresetValues = presetValues ?? System.Array.Empty<string>();
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            JArray existing = null;
            if (context.GetSetting(SaveKey) is JArray arr)
                existing = arr;
            var count = existing?.Count ?? (PresetValues?.Length ?? 1);
            if (count < 1) count = 1;
            var stack = new System.Windows.Controls.StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new System.Windows.Thickness(0, 8, 0, 0),
                Tag = "dynamic:" + SaveKey
            };
            if (context.DynamicTextboxPanels != null)
                context.DynamicTextboxPanels[SaveKey] = stack;
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var listPanel = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < count; i++)
            {
                var text = existing != null && i < existing.Count ? existing[i]?.ToString() ?? "" : (PresetValues != null && i < PresetValues.Length ? PresetValues[i] : "");
                var tb = new System.Windows.Controls.TextBox
                {
                    Tag = SaveKey + "|" + i,
                    Text = text,
                    MinWidth = 200,
                    Height = 28,
                    Padding = new System.Windows.Thickness(6, 4, 6, 4),
                    Margin = new System.Windows.Thickness(0, 4, 0, 0)
                };
                tb.TextChanged += (s, e) => context.MarkDirty();
                listPanel.Children.Add(tb);
            }
            stack.Children.Add(listPanel);
            var btnRow = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            var addBtn = new Button { Content = "Add", Margin = new System.Windows.Thickness(0, 0, 8, 0), Padding = new System.Windows.Thickness(12, 6, 12, 6) };
            addBtn.Click += (s, e) =>
            {
                var tb = new System.Windows.Controls.TextBox
                {
                    Tag = SaveKey + "|" + listPanel.Children.Count,
                    Text = "",
                    MinWidth = 200,
                    Height = 28,
                    Padding = new System.Windows.Thickness(6, 4, 6, 4),
                    Margin = new System.Windows.Thickness(0, 4, 0, 0)
                };
                tb.TextChanged += (se, ev) => context.MarkDirty();
                listPanel.Children.Add(tb);
                context.MarkDirty();
            };
            var removeBtn = new Button { Content = "Remove", Padding = new System.Windows.Thickness(12, 6, 12, 6) };
            removeBtn.Click += (s, e) =>
            {
                if (listPanel.Children.Count > 1)
                {
                    listPanel.Children.RemoveAt(listPanel.Children.Count - 1);
                    context.MarkDirty();
                }
            };
            btnRow.Children.Add(addBtn);
            btnRow.Children.Add(removeBtn);
            stack.Children.Add(btnRow);
            context.Registry.Register(SaveKey, stack);
            panel.Children.Add(stack);
        }
    }
}
