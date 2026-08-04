using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace FluentConfig.Native
{
    /// <summary>
    /// Applies DWM immersive dark mode and optional caption colors to native WPF chrome.
    /// </summary>
    internal static class DwmTitleBar
    {
        private const int DwmwaUseImmersiveDarkModeOld = 19;
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaBorderColor = 34;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        internal static void Apply(Window window, string colorScheme)
        {
            if (window == null)
                return;

            void ApplyWhenReady(object sender, EventArgs e)
            {
                window.SourceInitialized -= ApplyWhenReady;
                try
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    if (handle == IntPtr.Zero)
                        return;

                    bool dark = ResolveDark(colorScheme);
                    SetDarkMode(handle, dark);
                    if (dark)
                    {
                        // Match WebView2 DefaultBackgroundColor (#1E1E1E).
                        SetColor(handle, DwmwaCaptionColor, 0x1E1E1E);
                        SetColor(handle, DwmwaBorderColor, 0x1E1E1E);
                    }
                }
                catch
                {
                    // Fail soft — default OS chrome is fine.
                }
            }

            if (window.IsLoaded)
                ApplyWhenReady(window, EventArgs.Empty);
            else
                window.SourceInitialized += ApplyWhenReady;
        }

        internal static bool ResolveDark(string colorScheme)
        {
            if (string.IsNullOrEmpty(colorScheme) ||
                string.Equals(colorScheme, "dark", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(colorScheme, "light", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(colorScheme, "system", StringComparison.OrdinalIgnoreCase))
                return SystemPrefersDark();

            return true;
        }

        private static bool SystemPrefersDark()
        {
            try
            {
                var value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    1);
                return value is int i && i == 0;
            }
            catch
            {
                return true;
            }
        }

        private static void SetDarkMode(IntPtr handle, bool dark)
        {
            int enabled = dark ? 1 : 0;
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeOld, ref enabled, sizeof(int));
        }

        private static void SetColor(IntPtr handle, int attribute, int rgb)
        {
            int color = rgb;
            DwmSetWindowAttribute(handle, attribute, ref color, sizeof(int));
        }
    }
}
