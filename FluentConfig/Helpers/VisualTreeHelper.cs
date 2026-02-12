using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FluentConfig.Helpers
{
    /// <summary>
    /// Static helper for traversing the WPF visual tree (distinct from System.Windows.Media.VisualTreeHelper).
    /// </summary>
    public static class VisualTreeHelper
    {
        /// <summary>Returns root and all descendants (root included).</summary>
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

        /// <summary>Returns all descendants excluding root (children and below only).</summary>
        public static IEnumerable<DependencyObject> DescendantsOnly(DependencyObject root)
        {
            if (root == null) yield break;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var d in Descendants(child))
                    yield return d;
            }
        }
    }
}
