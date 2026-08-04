using System;
using System.Windows;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>
    /// Persisted window bounds for a FluentConfig UI instance (keyed by title).
    /// </summary>
    public sealed class WindowGeometryData
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string State { get; set; } = "Normal";
    }

    /// <summary>
    /// Loads/saves window geometry via CPH global FluentConfig_Window_{title}.
    /// </summary>
    internal static class WindowGeometryStore
    {
        internal static string KeyForTitle(string title) => "FluentConfig_Window_" + (title ?? "Settings");

        internal static WindowGeometryData Load(IInlineInvokeProxy cph, string title)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return null;

            try
            {
                string json = cph.GetGlobalVar<string>(KeyForTitle(title), true);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var obj = JObject.Parse(json);
                var data = new WindowGeometryData
                {
                    Left = obj.Value<double?>("left") ?? double.NaN,
                    Top = obj.Value<double?>("top") ?? double.NaN,
                    Width = obj.Value<double?>("width") ?? double.NaN,
                    Height = obj.Value<double?>("height") ?? double.NaN,
                    State = obj.Value<string>("state") ?? "Normal",
                };

                if (!IsValid(data))
                    return null;

                return ClampToVirtualScreen(data);
            }
            catch
            {
                return null;
            }
        }

        internal static void Save(IInlineInvokeProxy cph, string title, WindowGeometryData data)
        {
            if (cph == null || string.IsNullOrEmpty(title) || data == null || !IsValid(data))
                return;

            try
            {
                var obj = new JObject
                {
                    ["left"] = data.Left,
                    ["top"] = data.Top,
                    ["width"] = data.Width,
                    ["height"] = data.Height,
                    ["state"] = data.State ?? "Normal",
                };
                cph.SetGlobalVar(KeyForTitle(title), obj.ToString(), true);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }

        internal static WindowGeometryData FromWindow(Window window)
        {
            if (window == null)
                return null;

            if (window.WindowState == WindowState.Minimized)
                return null;

            var rect = window.WindowState == WindowState.Maximized
                ? window.RestoreBounds
                : new Rect(window.Left, window.Top, window.Width, window.Height);

            if (rect.Width <= 0 || rect.Height <= 0)
                return null;

            return new WindowGeometryData
            {
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Width,
                Height = rect.Height,
                State = window.WindowState == WindowState.Maximized ? "Maximized" : "Normal",
            };
        }

        internal static void ApplyToWindow(Window window, WindowGeometryData data)
        {
            if (window == null || data == null || !IsValid(data))
                return;

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = data.Left;
            window.Top = data.Top;
            window.Width = data.Width;
            window.Height = data.Height;

            if (string.Equals(data.State, "Maximized", StringComparison.OrdinalIgnoreCase))
            {
                void MaximizeWhenReady(object sender, RoutedEventArgs e)
                {
                    window.Loaded -= MaximizeWhenReady;
                    window.WindowState = WindowState.Maximized;
                }

                if (window.IsLoaded)
                    window.WindowState = WindowState.Maximized;
                else
                    window.Loaded += MaximizeWhenReady;
            }
        }

        internal static WindowGeometryData ClampToVirtualScreen(WindowGeometryData data)
        {
            if (data == null || !IsValid(data))
                return data;

            double vsLeft = SystemParameters.VirtualScreenLeft;
            double vsTop = SystemParameters.VirtualScreenTop;
            double vsWidth = SystemParameters.VirtualScreenWidth;
            double vsHeight = SystemParameters.VirtualScreenHeight;

            double minW = Math.Max(windowMinWidth(), 480);
            double minH = Math.Max(windowMinHeight(), 360);

            data.Width = Math.Max(minW, Math.Min(data.Width, vsWidth));
            data.Height = Math.Max(minH, Math.Min(data.Height, vsHeight));

            double maxLeft = vsLeft + vsWidth - data.Width;
            double maxTop = vsTop + vsHeight - data.Height;
            data.Left = Math.Max(vsLeft, Math.Min(data.Left, maxLeft));
            data.Top = Math.Max(vsTop, Math.Min(data.Top, maxTop));

            return data;
        }

        private static bool IsValid(WindowGeometryData data)
        {
            if (data == null)
                return false;

            return !double.IsNaN(data.Left) && !double.IsNaN(data.Top)
                && !double.IsNaN(data.Width) && !double.IsNaN(data.Height)
                && data.Width >= 480 && data.Height >= 360;
        }

        private static double windowMinWidth() => 480;
        private static double windowMinHeight() => 360;
    }
}
