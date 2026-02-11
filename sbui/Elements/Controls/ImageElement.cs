using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Sbui.Elements
{
    /// <summary>
    /// Renders an image from a URL. No save key; display only.
    /// </summary>
    public class ImageElement : UIElement
    {
        public string ImageUrl { get; set; }
        public double? MaxHeight { get; set; }

        public ImageElement(string tabName, string imageUrl, double? maxHeight = null, string visibilityKey = null)
        {
            TabName = tabName ?? "";
            ImageUrl = imageUrl ?? "";
            MaxHeight = maxHeight;
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null || string.IsNullOrEmpty(ImageUrl)) return;

            var img = new System.Windows.Controls.Image
            {
                Margin = new Thickness(0, 8, 0, 0),
                Stretch = System.Windows.Media.Stretch.Uniform
            };
            if (MaxHeight.HasValue && MaxHeight.Value > 0)
                img.MaxHeight = MaxHeight.Value;

            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(ImageUrl, UriKind.RelativeOrAbsolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                if (bi.CanFreeze) bi.Freeze();
                img.Source = bi;
            }
            catch (Exception ex)
            {
                context.Log($"Image load error: {ex.Message}");
                return;
            }

            panel.Children.Add(img);
        }
    }
}
