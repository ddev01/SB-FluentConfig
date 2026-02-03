using System.Windows.Controls;

namespace Sbui.Elements
{
    /// <summary>
    /// Base for strongly-typed UI element descriptors; Render adds content to the context panel.
    /// </summary>
    public abstract class UIElement
    {
        public string TabName { get; set; }
        public string VisibilityKey { get; set; }

        /// <summary>
        /// Adds this element's content to the appropriate panel using the context.
        /// </summary>
        public abstract void Render(IRenderContext context);
    }
}
