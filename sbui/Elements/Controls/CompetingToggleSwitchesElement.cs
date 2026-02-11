using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Sbui.Helpers;
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
        public int MaxSelected { get; set; } = 1;
        public int[] DefaultIndices { get; set; }

        public CompetingToggleSwitchesElement(string title, string description, string tabName, string saveKey, string[] options, int defaultIndex, int maxSelected = 1, int[] defaultIndices = null, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            Options = options ?? Array.Empty<string>();
            DefaultIndex = defaultIndex;
            MaxSelected = Math.Max(1, maxSelected);
            DefaultIndices = defaultIndices;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var options = Options ?? Array.Empty<string>();
            var maxSelected = Math.Max(1, MaxSelected);
            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            if (maxSelected == 1)
            {
                var token = context.GetSetting(SaveKey);
                var savedIndex = token != null && token.Type == Newtonsoft.Json.Linq.JTokenType.Integer
                    ? token.ToObject<int>() : DefaultIndex;
                savedIndex = Math.Max(0, Math.Min(savedIndex, options.Length > 0 ? options.Length - 1 : 0));
                var selector = new ComboBox { Tag = SaveKey, Visibility = Visibility.Collapsed, Width = 0, Height = 0 };
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
                        foreach (var child in stack.Children)
                        {
                            if (child is ToggleSwitch other && other != ts && other.Tag is string t && t.StartsWith(SaveKey + "_opt_"))
                                other.IsChecked = false;
                        }
                    };
                    stack.Children.Add(ts);
                }
            }
            else
            {
                var token = context.GetSetting(SaveKey);
                var selectedIndices = ParseIndices(token);
                if (selectedIndices.Count == 0 && DefaultIndices != null && DefaultIndices.Length > 0)
                    selectedIndices = new List<int>(DefaultIndices);
                var storage = new System.Windows.Controls.TextBox { Tag = SbuiTags.CompetingPrefix + SaveKey, Text = SerializeIndices(selectedIndices), Visibility = Visibility.Collapsed, Width = 0, Height = 0 };
                var storagePanel = new StackPanel { Tag = SbuiTags.CompetingPrefix + SaveKey };
                storagePanel.Children.Add(storage);
                context.Registry.Register(SaveKey, storagePanel);
                stack.Children.Add(storagePanel);
                var maxIdx = options.Length > 0 ? options.Length - 1 : 0;
                var selectionOrder = new List<int>(selectedIndices);
                for (int i = 0; i < options.Length; i++)
                {
                    var idx = i;
                    var isChecked = selectedIndices.Contains(idx);
                    var ts = new ToggleSwitch { Tag = SaveKey + "_opt_" + i, Content = options[i], IsChecked = isChecked };
                    ts.Checked += (s, e) =>
                    {
                        context.MarkDirty();
                        if (selectionOrder.Contains(idx))
                        {
                            selectionOrder.Remove(idx);
                        }
                        else
                        {
                            if (selectionOrder.Count >= maxSelected)
                                selectionOrder.RemoveAt(0);
                            selectionOrder.Add(idx);
                        }
                        storage.Text = SerializeIndices(selectionOrder);
                        SyncToggleStates(stack, SaveKey, selectionOrder);
                    };
                    ts.Unchecked += (s, e) =>
                    {
                        context.MarkDirty();
                        selectionOrder.Remove(idx);
                        storage.Text = SerializeIndices(selectionOrder);
                        SyncToggleStates(stack, SaveKey, selectionOrder);
                    };
                    stack.Children.Add(ts);
                }
            }
            panel.Children.Add(stack);
        }

        private static List<int> ParseIndices(Newtonsoft.Json.Linq.JToken token)
        {
            var list = new List<int>();
            if (token is Newtonsoft.Json.Linq.JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item != null && (item.Type == Newtonsoft.Json.Linq.JTokenType.Integer || item.Type == Newtonsoft.Json.Linq.JTokenType.Float))
                        list.Add(item.ToObject<int>());
                }
            }
            else if (token != null && token.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                list.Add(token.ToObject<int>());
            }
            return list;
        }

        private static string SerializeIndices(List<int> indices)
        {
            if (indices == null || indices.Count == 0) return "[]";
            return "[" + string.Join(",", indices) + "]";
        }

        private static void SyncToggleStates(StackPanel stack, string saveKey, List<int> selectionOrder)
        {
            var prefix = saveKey + "_opt_";
            foreach (var child in stack.Children)
            {
                if (child is ToggleSwitch ts && ts.Tag is string t && t.StartsWith(prefix))
                {
                    if (int.TryParse(t.Substring(prefix.Length), out var i))
                        ts.IsChecked = selectionOrder.Contains(i);
                }
            }
        }
    }
}
