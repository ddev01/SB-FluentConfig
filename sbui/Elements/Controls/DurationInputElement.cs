using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Sbui.Helpers;

namespace Sbui.Elements
{
    public class DurationInputElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public bool PermanentOption { get; set; }
        public string DefaultValue { get; set; }

        private static readonly string[] UnitOptions = { "s", "m", "h", "d", "w", "permanent" };

        public DurationInputElement(string title, string description, string tabName, string saveKey, bool permanentOption = true, string defaultValue = "permanent", string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            PermanentOption = permanentOption;
            DefaultValue = defaultValue ?? "permanent";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            var token = context.GetSetting(SaveKey);
            var stored = token?.ToString() ?? DefaultValue;
            ParseDuration(stored, out int numVal, out int unitIndex);

            var options = PermanentOption ? UnitOptions : UnitOptions.Take(5).ToArray();
            unitIndex = Math.Max(0, Math.Min(unitIndex, options.Length - 1));
            var isPermanent = options[unitIndex] == "permanent";

            var outer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 8, 0, 0),
                Tag = SbuiTags.DurationPrefix + SaveKey
            };
            outer.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                outer.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            var row = new StackPanel { Orientation = Orientation.Horizontal };

            var numBox = new System.Windows.Controls.TextBox
            {
                Text = isPermanent ? "0" : numVal.ToString(),
                Width = 60,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                IsEnabled = !isPermanent
            };
            numBox.TextChanged += (s, e) =>
            {
                context.MarkDirty();
                if (int.TryParse(numBox.Text, out var v) && v < 0)
                    numBox.Text = "0";
            };

            var unitCombo = SbuiComponentFactory.CreateComboBoxForEmbedded(options, unitIndex);
            unitCombo.SelectionChanged += (s, e) =>
            {
                context.MarkDirty();
                var idx = unitCombo.SelectedIndex;
                var isPerm = idx >= 0 && idx < options.Length && options[idx] == "permanent";
                numBox.IsEnabled = !isPerm;
                if (isPerm) numBox.Text = "0";
            };

            row.Children.Add(numBox);
            row.Children.Add(unitCombo);
            outer.Children.Add(row);

            context.Registry.Register(SaveKey, outer);
            panel.Children.Add(outer);
        }

        private static void ParseDuration(string value, out int num, out int unitIndex)
        {
            num = 0;
            unitIndex = 5; // permanent
            if (string.IsNullOrEmpty(value)) return;
            value = value.Trim().ToLowerInvariant();
            if (value == "permanent") return;

            var unitChars = "smhdw";
            for (int i = value.Length - 1; i >= 0; i--)
            {
                var c = value[i];
                if (char.IsDigit(c) || c == ' ')
                    continue;
                if (unitChars.IndexOf(c) >= 0)
                {
                    var numStr = value.Substring(0, i).Trim();
                    int.TryParse(numStr, out num);
                    switch (c) { case 's': unitIndex = 0; break; case 'm': unitIndex = 1; break; case 'h': unitIndex = 2; break; case 'd': unitIndex = 3; break; case 'w': unitIndex = 4; break; default: unitIndex = 5; break; }
                    return;
                }
                break;
            }
            int.TryParse(value, out num);
            unitIndex = 1; // default to minutes
        }
    }
}
