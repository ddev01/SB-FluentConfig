using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FluentConfig.Core;
using FluentConfig.Native;
using FluentConfig.Protocol;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FluentConfig
{
    /// <summary>
    /// Thin WPF host window owning a single WebView2 control (native window chrome for v1).
    /// </summary>
    public class FluentConfigHostWindow : Window
    {
        private readonly WebView2 _webView;
        private UiDocument _pendingBootstrap;
        private readonly string _colorScheme;

        public event Action<string> WebMessageReceived;
        public event Action NavigationCompleted;

        static FluentConfigHostWindow()
        {
            WebView2AssemblyResolve.EnsureInitialized();
        }

        public FluentConfigHostWindow(
            string title,
            string version,
            WindowGeometryData geometry = null,
            string iconPath = null,
            string colorScheme = "dark")
        {
            Title = string.IsNullOrEmpty(version) ? title : $"{title} (v{version})";
            Width = 900;
            Height = 700;
            MinWidth = 480;
            MinHeight = 360;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _colorScheme = colorScheme ?? "dark";

            ApplyIcon(iconPath);
            WindowGeometryStore.ApplyToWindow(this, geometry);
            DwmTitleBar.Apply(this, _colorScheme);

            _webView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 30, 30, 30),
            };
            Content = _webView;
        }

        internal void SetBootstrapDocument(UiDocument document) => _pendingBootstrap = document;

        internal UiDocument TakeBootstrapDocument()
        {
            var doc = _pendingBootstrap;
            _pendingBootstrap = null;
            return doc;
        }

        private void ApplyIcon(string iconPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                {
                    Icon = BitmapFrame.Create(new Uri(Path.GetFullPath(iconPath), UriKind.Absolute));
                    return;
                }
            }
            catch
            {
                // Fall through to embedded default.
            }

            try
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/FluentConfig;component/Assets/FluentConfig.ico"));
            }
            catch
            {
                // No icon is acceptable.
            }
        }

        /// <summary>
        /// Kick off WebView2 init after the dispatcher has a SynchronizationContext.
        /// Streamer.bot's WebView2 throws if EnsureCoreWebView2Async runs with
        /// SynchronizationContext.Current == null (even on the correct STA/UI thread).
        /// </summary>
        internal void BeginNavigate()
        {
            // BeginInvoke installs DispatcherSynchronizationContext for the callback,
            // which Streamer.bot's WebView2 treats as "event loop has started".
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _ = EnsureAndNavigateSafeAsync();
            }));
        }

        private async Task EnsureAndNavigateSafeAsync()
        {
            try
            {
                EnsureDispatcherSyncContext();

                if (!IsLoaded)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    RoutedEventHandler handler = null;
                    handler = (s, e) =>
                    {
                        Loaded -= handler;
                        tcs.TrySetResult(true);
                    };
                    Loaded += handler;
                    await tcs.Task.ConfigureAwait(true);
                    EnsureDispatcherSyncContext();
                }

                await EnsureAndNavigateAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Content = new TextBlock
                {
                    Text = "WebView2 init failed:\n" + ex,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.OrangeRed,
                };
            }
        }

        private void EnsureDispatcherSyncContext()
        {
            if (SynchronizationContext.Current != null)
                return;

            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher));
        }

        private async Task EnsureAndNavigateAsync()
        {
            await _webView.EnsureCoreWebView2Async(null);
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            _webView.CoreWebView2.WebMessageReceived += (s, args) =>
            {
                try
                {
                    // Prefer string posts (web RpcClient JSON.stringifies). Fall back to
                    // WebMessageAsJson when script posted an object instead.
                    var text = args.TryGetWebMessageAsString();
                    if (string.IsNullOrEmpty(text))
                        text = args.WebMessageAsJson;
                    if (!string.IsNullOrEmpty(text))
                        WebMessageReceived?.Invoke(text);
                }
                catch (Exception ex)
                {
                    FluentConfigApp.LogInternal("[FluentConfig] WebMessageReceived error: " + ex.Message);
                }
            };

            _webView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                Dispatcher.BeginInvoke(new Action(() => NavigationCompleted?.Invoke()));
            };

#if DEBUG
            // Hot-reload: Vite/Bun dev server from FluentConfig/web
            _webView.CoreWebView2.Navigate("http://localhost:5173");
#else
            _webView.NavigateToString(EmbeddedHtml.Content);
#endif
        }

        internal void PostWebMessage(string json)
        {
            if (_webView?.CoreWebView2 == null) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => PostWebMessage(json)));
                return;
            }
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }
}
