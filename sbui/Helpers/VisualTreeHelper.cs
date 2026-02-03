using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Sbui.Helpers
{
    /// <summary>
    /// Static helper for traversing the WPF visual tree (distinct from System.Windows.Media.VisualTreeHelper).
    /// </summary>
    public static class VisualTreeHelper
    {
        public static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            if (root == null) yield break;
            yield return root;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                foreach (var d in Descendants(child))
                    yield return d;
            }
        }
    }
}
