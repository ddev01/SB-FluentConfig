using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Sbui.Components;
using Sbui.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class PillInputElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public Action<System.Windows.Controls.StackPanel, System.Windows.Controls.Panel> WithSectionsPanel { get; set; }
        public Action<string, System.Windows.Controls.StackPanel> OnPillAdded { get; set; }
        public Action<string, System.Windows.Controls.StackPanel> OnPillRemoved { get; set; }

        public PillInputElement(string title, string description, string tabName, string saveKey, Action<System.Windows.Controls.StackPanel, System.Windows.Controls.Panel> withSectionsPanel = null, Action<string, System.Windows.Controls.StackPanel> onPillAdded = null, Action<string, System.Windows.Controls.StackPanel> onPillRemoved = null, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            WithSectionsPanel = withSectionsPanel;
            OnPillAdded = onPillAdded;
            OnPillRemoved = onPillRemoved;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            var token = context.GetSetting(SaveKey);
            var existing = (token as JArray)?.Select(t => t?.ToString() ?? "").ToList() ?? new List<string>();

            var outer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 8, 0, 0),
                Tag = SbuiTags.PillPrefix + SaveKey
            };
            outer.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                outer.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            var inputBox = new System.Windows.Controls.TextBox
            {
                Text = "",
                MinWidth = 180,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var addBtn = new Button
            {
                Content = "Add",
                Padding = new Thickness(12, 6, 12, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            addRow.Children.Add(inputBox);
            addRow.Children.Add(addBtn);

            var pillsPanel = new WrapPanel
            {
                Margin = new Thickness(0, 8, 0, 0),
                Orientation = Orientation.Horizontal
            };

            var sectionsPanel = new System.Windows.Controls.StackPanel { Orientation = Orientation.Vertical };

            Action<string, bool> addPill = (text, isUserAdd) =>
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                text = text.Trim();
                var pillBorder = new Border
                {
                    Tag = text,
                    Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8, 4, 4, 4),
                    Margin = new Thickness(0, 0, 6, 6),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var pillRow = new StackPanel { Orientation = Orientation.Horizontal };
                pillRow.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = text,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });
                var pillText = text;
                var removeBtn = new Button
                {
                    Content = "×",
                    Width = 22,
                    Height = 22,
                    FontSize = 14,
                    Padding = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                removeBtn.Click += (s, e) =>
                {
                    pillsPanel.Children.Remove(pillBorder);
                    context.MarkDirty();
                    OnPillRemoved?.Invoke(pillText, sectionsPanel);
                };
                pillRow.Children.Add(removeBtn);
                pillBorder.Child = pillRow;
                pillsPanel.Children.Add(pillBorder);
                if (isUserAdd)
                {
                    context.MarkDirty();
                    OnPillAdded?.Invoke(text, sectionsPanel);
                }
            };

            addBtn.Click += (s, e) =>
            {
                addPill(inputBox.Text, true);
                inputBox.Text = "";
                inputBox.Focus();
            };
            inputBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    addPill(inputBox.Text, true);
                    inputBox.Text = "";
                    e.Handled = true;
                }
            };

            foreach (var item in existing)
                addPill(item, false);

            outer.Children.Add(addRow);
            outer.Children.Add(pillsPanel);

            context.Registry.Register(SaveKey, outer);
            panel.Children.Add(outer);
            if (WithSectionsPanel != null)
                WithSectionsPanel(sectionsPanel, panel);
        }
    }
}
