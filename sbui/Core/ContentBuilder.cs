using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sbui.Elements;
using Wpf.Ui.Controls;

namespace Sbui.Core
{
    public class ContentBuilder
    {
        private readonly IRenderContext _context;

        public ContentBuilder(IRenderContext context)
        {
            _context = context;
        }

        public void BuildFromPendingItems(IReadOnlyList<PendingItem> items, Grid mainGrid, out string headerUrl)
        {
            headerUrl = ExtractHeaderUrl(items);

            if (!string.IsNullOrEmpty(headerUrl))
                AddHeaderImage(mainGrid, headerUrl);

            var groups = GroupItemsByVisibility(items);
            foreach (var (visibilityKey, groupItems) in groups)
            {
                if (!string.IsNullOrEmpty(visibilityKey))
                    RenderConditionalGroup(visibilityKey, groupItems);
                else
                    RenderDirectGroup(groupItems);
            }
        }

        private static string ExtractHeaderUrl(IReadOnlyList<PendingItem> items)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item.Kind == PendingKind.Header && item.Args != null && item.Args.Length > 0 &&
                    item.Args[0] is string url && !string.IsNullOrEmpty(url))
                    return url;
            }
            return null;
        }

        private void AddHeaderImage(Grid mainGrid, string url)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 12)
            };
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new System.Uri(url, System.UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                var img = new System.Windows.Controls.Image
                {
                    Source = bi,
                    MaxHeight = 120,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                headerPanel.Children.Add(img);
            }
            catch
            {
                headerPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "[Header image]",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 4)
                });
            }
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Insert(0, headerPanel);
        }

        private static List<(string visibilityKey, List<PendingItem> groupItems)> GroupItemsByVisibility(IReadOnlyList<PendingItem> items)
        {
            var groups = new List<(string visibilityKey, List<PendingItem> groupItems)>();
            string currentKey = null;
            List<PendingItem> currentGroup = null;

            foreach (var item in items)
            {
                if (item.Kind == PendingKind.Header) continue;
                var key = item.VisibilityKey ?? "";
                var useKey = string.IsNullOrEmpty(key) ? null : key;
                if (useKey != currentKey || currentGroup == null)
                {
                    if (currentGroup != null && currentGroup.Count > 0)
                        groups.Add((currentKey, currentGroup));
                    currentKey = useKey;
                    currentGroup = new List<PendingItem>();
                }
                currentGroup.Add(item);
            }
            if (currentGroup != null && currentGroup.Count > 0)
                groups.Add((currentKey, currentGroup));

            return groups;
        }

        private void RenderConditionalGroup(string visibilityKey, List<PendingItem> groupItems)
        {
            var container = new StackPanel { Orientation = Orientation.Vertical };
            _context.SetVisibilityOverridePanel(container);
            foreach (var item in groupItems)
            {
                var el = PendingItemConverter.ToElement(item);
                if (el != null) el.Render(_context);
            }
            _context.ClearVisibilityOverridePanel();

            if (_context.Registry.TryGetValue(visibilityKey, out ToggleSwitch toggle))
            {
                container.Visibility = toggle.IsChecked == true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                toggle.Checked += (s, e) => container.Visibility = System.Windows.Visibility.Visible;
                toggle.Unchecked += (s, e) => container.Visibility = System.Windows.Visibility.Collapsed;
            }

            var tabName = PendingItemConverter.GetTabName(groupItems[0]);
            var tabPanel = _context.GetPanel(tabName);
            if (tabPanel != null)
                tabPanel.Children.Add(container);
        }

        private void RenderDirectGroup(List<PendingItem> groupItems)
        {
            foreach (var item in groupItems)
            {
                var el = PendingItemConverter.ToElement(item);
                if (el != null) el.Render(_context);
            }
        }
    }
}
