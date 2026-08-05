using System;
using System.ComponentModel;
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
        private bool _webViewDisposed;

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

        /// <summary>
        /// Dispose WebView2 on the UI thread while the COM controller is still alive.
        /// Leaving it to <see cref="System.Windows.Interop.HwndHost"/>'s finalizer crashes
        /// Streamer.bot on shutdown (InvalidCastException / E_NOINTERFACE).
        /// </summary>
        /// <remarks>
        /// WebView2.Dispose can throw when the COM controller is already torn down during
        /// host exit. We always <see cref="GC.SuppressFinalize"/> afterward so the broken
        /// HwndHost finalizer cannot run and take down Streamer.bot (WebView2Feedback #2420).
        /// </remarks>
        internal void DisposeWebViewCore()
        {
            if (_webViewDisposed) return;

            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(DisposeWebViewCore);
                }
                catch (Exception ex)
                {
                    // Dispatcher may already be shutting down — still kill the finalizer.
                    _webViewDisposed = true;
                    try { GC.SuppressFinalize(_webView); }
                    catch { /* ignore */ }
                    FluentConfigApp.LogInternal(
                        "[FluentConfig] WebView2 dispose marshal failed: " + ex.Message);
                }
                return;
            }

            if (_webViewDisposed) return;
            _webViewDisposed = true;

            try
            {
                if (ReferenceEquals(Content, _webView))
                    Content = null;

                TryCloseCoreWebView2Controller();
                _webView.Dispose();
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal("[FluentConfig] WebView2 dispose: " + ex.Message);
            }
            finally
            {
                // Critical: Dispose(true) can throw before WebView2 reaches SuppressFinalize.
                // Without this, HwndHost.Finalize → Dispose(false) fatals Streamer.bot.
                try { GC.SuppressFinalize(_webView); }
                catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Best-effort controller.Close() before Dispose, via the WPF control's public surface
        /// when available. Failures are ignored — Dispose / SuppressFinalize handle the rest.
        /// </summary>
        private void TryCloseCoreWebView2Controller()
        {
            try
            {
                var core = _webView.CoreWebView2;
                if (core == null) return;

                // WPF WebView2 does not expose Controller publicly on all SB-bundled builds.
                // Stop the page so in-flight work does not touch a dying COM object.
                core.Stop();
            }
            catch
            {
                // Environment may already be gone during Streamer.bot shutdown.
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // base raises Closing first (session Detach), then we dispose WebView2
            // while the COM controller is still alive — before HWND teardown.
            base.OnClosing(e);
            if (!e.Cancel)
                DisposeWebViewCore();
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
            if (_webViewDisposed) return;

            // BeginInvoke installs DispatcherSynchronizationContext for the callback,
            // which Streamer.bot's WebView2 treats as "event loop has started".
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (_webViewDisposed) return;
                _ = EnsureAndNavigateSafeAsync();
            }));
        }

        private async Task EnsureAndNavigateSafeAsync()
        {
            try
            {
                if (_webViewDisposed) return;

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
                    if (_webViewDisposed) return;
                    EnsureDispatcherSyncContext();
                }

                await EnsureAndNavigateAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (_webViewDisposed) return;
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
            if (_webViewDisposed) return;

            await _webView.EnsureCoreWebView2Async(null);
            if (_webViewDisposed || _webView.CoreWebView2 == null) return;

            // WebView2Feedback #2420: HwndHost.Finalize → Dispose(false) fatals if the COM
            // controller is already gone (typical Streamer.bot shutdown). We always dispose
            // ourselves on close/host-exit; suppressing the finalizer makes a missed teardown
            // a leak instead of a process-killing Fatal UI Exception.
            GC.SuppressFinalize(_webView);

            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            _webView.CoreWebView2.WebMessageReceived += (s, args) =>
            {
                if (_webViewDisposed) return;
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
                if (_webViewDisposed) return;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_webViewDisposed)
                        NavigationCompleted?.Invoke();
                }));
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
            if (_webViewDisposed || _webView?.CoreWebView2 == null) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => PostWebMessage(json)));
                return;
            }
            if (_webViewDisposed || _webView.CoreWebView2 == null) return;
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }
}
