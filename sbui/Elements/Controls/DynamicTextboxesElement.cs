using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Sbui.Components;
using Sbui.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    /// <summary>
    /// Dynamic list of textboxes with per-row delete. Preset is a fluent option via DynamicTextboxesBuilder.Preset().
    /// </summary>
    public class DynamicTextboxesElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string[] PresetValues { get; set; }

        public DynamicTextboxesElement(string title, string description, string tabName, string saveKey, string[] presetValues, string visibilityKey = null)
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
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 8, 0, 0),
                Tag = SbuiTags.DynamicPrefix + SaveKey
            };
            if (context.DynamicTextboxPanels != null)
                context.DynamicTextboxPanels[SaveKey] = stack;
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var listPanel = new StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < count; i++)
            {
                var text = existing != null && i < existing.Count ? existing[i]?.ToString() ?? "" : (PresetValues != null && i < PresetValues.Length ? PresetValues[i] : "");
                listPanel.Children.Add(CreateRow(SaveKey, i, text, listPanel, context));
            }
            stack.Children.Add(listPanel);
            var addBtn = new Button { Content = "Add", Margin = new Thickness(0, 8, 0, 0), Padding = new Thickness(12, 6, 12, 6) };
            addBtn.Click += (s, e) =>
            {
                listPanel.Children.Add(CreateRow(SaveKey, listPanel.Children.Count, "", listPanel, context));
                context.MarkDirty();
            };
            stack.Children.Add(addBtn);
            context.Registry.Register(SaveKey, stack);
            panel.Children.Add(stack);
        }

        private static FrameworkElement CreateRow(string saveKey, int index, string text, StackPanel listPanel, IRenderContext context)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tb = new System.Windows.Controls.TextBox
            {
                Tag = saveKey + "|" + index,
                Text = text,
                MinWidth = 200,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 4, 0, 0)
            };
            tb.TextChanged += (s, e) => context.MarkDirty();
            var trashIcon = new System.Windows.Controls.TextBlock
            {
                Text = "\uE74D",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var deleteBtn = new Button
            {
                Content = trashIcon,
                ToolTip = "Delete",
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 4, 0, 0)
            };
            deleteBtn.Click += (s, e) =>
            {
                if (listPanel.Children.Count > 1)
                {
                    listPanel.Children.Remove(row);
                    ReindexRows(listPanel, saveKey);
                    context.MarkDirty();
                }
            };
            Grid.SetColumn(tb, 0);
            Grid.SetColumn(deleteBtn, 1);
            row.Children.Add(tb);
            row.Children.Add(deleteBtn);
            return row;
        }

        private static void ReindexRows(StackPanel listPanel, string saveKey)
        {
            for (int i = 0; i < listPanel.Children.Count; i++)
            {
                if (listPanel.Children[i] is Grid row)
                {
                    var tb = row.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                    if (tb != null)
                        tb.Tag = saveKey + "|" + i;
                }
            }
        }
    }
}
