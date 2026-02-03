using System;
using System.Linq;
using System.Windows.Controls;
using Sbui.Components;
using Wpf.Ui.Controls;

namespace Sbui.Elements
{
    public class CompetingToggleSwitchesElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string[] Options { get; set; }
        public int DefaultIndex { get; set; }

        public CompetingToggleSwitchesElement(string title, string description, string tabName, string saveKey, string[] options, int defaultIndex, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Options = options ?? Array.Empty<string>();
            DefaultIndex = defaultIndex;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var options = Options ?? Array.Empty<string>();
            var savedIndex = context.Settings?[SaveKey] != null && context.Settings[SaveKey].Type == Newtonsoft.Json.Linq.JTokenType.Integer
                ? context.Settings[SaveKey].ToObject<int>() : DefaultIndex;
            savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
            var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));
            var selector = new System.Windows.Controls.ComboBox { Tag = SaveKey, Visibility = System.Windows.Visibility.Collapsed, Width = 0, Height = 0 };
            selector.ItemsSource = Enumerable.Range(0, options.Length).Select(i => i.ToString()).ToArray();
            selector.SelectedIndex = savedIndex;
            context.Registry.Register(SaveKey, selector);
            stack.Children.Add(selector);
            for (int i = 0; i < options.Length; i++)
            {
                var idx = i;
                var ts = new ToggleSwitch { Tag = SaveKey + "_opt_" + i, Content = options[i], IsChecked = (i == savedIndex) };
                ts.Checked += (s, e) =>
                {
                    context.MarkDirty();
                    selector.SelectedIndex = idx;
                    for (int j = 0; j < stack.Children.Count; j++)
                    {
                        if (stack.Children[j] is ToggleSwitch other && other != ts && other.Tag is string t && t.StartsWith(SaveKey + "_opt_"))
                            other.IsChecked = false;
                    }
                };
                stack.Children.Add(ts);
            }
            panel.Children.Add(stack);
        }
    }
}
