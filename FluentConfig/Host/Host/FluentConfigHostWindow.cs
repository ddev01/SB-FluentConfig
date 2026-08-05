using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
        private readonly Grid _root;
        private WebView2 _webView;
        private readonly Border _loadingOverlay;
        private UiDocument _pendingBootstrap;
        private readonly string _colorScheme;
        private readonly string _iconPath;
        private bool _webViewDisposed;
        private HwndSource _hwndSource;
        /// <summary>
        /// When false, title-bar / Alt+F4 close is swallowed so <see cref="Window.Closing"/>
        /// never runs (WebView2 goes blank if Closing is raised then cancelled).
        /// </summary>
        private bool _allowClose;
        private bool _nativeCloseNotifyInFlight;

        /// <summary>Optional perf tracer (set by session after construction).</summary>
        internal PerformanceTracer PerfTracer { get; set; }

        /// <summary>
        /// Process-lifetime shared WebView2 environment. Reused across window opens on the
        /// UI thread only (no background PreWarm). Speeds warm <c>EnsureCoreWebView2Async</c>.
        /// </summary>
        private static readonly object SharedEnvLock = new object();
        private static Task<CoreWebView2Environment> _sharedEnvironmentTask;

        /// <summary>
        /// Start <see cref="CoreWebView2Environment.CreateAsync"/> without awaiting so creation
        /// overlaps window construction. UI thread only; safe to call multiple times.
        /// </summary>
        internal static void KickoffSharedEnvironment()
        {
            _ = GetSharedEnvironmentAsync();
        }

        public event Action<string> WebMessageReceived;
        public event Action NavigationCompleted;

        /// <summary>
        /// Raised when the user clicks the native close button (or Alt+F4 / system menu)
        /// before WPF <see cref="Window.Closing"/> runs. Session should prompt, then call
        /// <see cref="CloseAllowed"/>.
        /// </summary>
        internal event Action NativeCloseRequested;

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
            _iconPath = iconPath;

            WindowGeometryStore.ApplyToWindow(this, geometry);
            DwmTitleBar.Apply(this, _colorScheme);
            SourceInitialized += OnSourceInitialized;

            _loadingOverlay = CreateLoadingOverlay();
            _root = new Grid();
            _root.Children.Add(_loadingOverlay);
            Content = _root;

            // Icon decode is not on the critical path — defer until after first layout.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ApplyIcon(_iconPath)));
        }

        /// <summary>
        /// Lazily construct the WebView2 control after the window HWND exists (Show), not during
        /// <c>Window.Create</c>. Must run on the UI thread before <see cref="EnsureAndNavigateAsync"/>.
        /// </summary>
        private void EnsureWebViewCreated()
        {
            if (_webView != null || _webViewDisposed) return;

            _webView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 30, 30, 30),
            };
            _root.Children.Insert(0, _webView);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            SourceInitialized -= OnSourceInitialized;
            _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            _hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int wmSysCommand = 0x0112;
            const int scClose = 0xF060;

            if (_allowClose)
                return IntPtr.Zero;

            // Title-bar X, Alt+F4, system-menu Close. Do not swallow WM_CLOSE — WPF
            // programmatic Close() relies on it after Closing; intercepting blanks/orphans the HWND.
            if (msg == wmSysCommand && ((int)(long)wParam & 0xFFF0) == scClose)
            {
                handled = true;
                NotifyNativeCloseRequested();
                return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private void NotifyNativeCloseRequested()
        {
            if (_nativeCloseNotifyInFlight)
                return;
            _nativeCloseNotifyInFlight = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                try
                {
                    NativeCloseRequested?.Invoke();
                }
                finally
                {
                    _nativeCloseNotifyInFlight = false;
                }
            }));
        }

        /// <summary>
        /// Proceed with a real close after discard confirmation (or host exit / in-app Exit).
        /// Sets the allow flag so the WndProc hook does not swallow destruction.
        /// </summary>
        internal void CloseAllowed()
        {
            _allowClose = true;
            Close();
        }

        private static Border CreateLoadingOverlay()
        {
            // Solid cover only — spinner/text add no value before web paints and cost WPF elements.
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            };
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay != null)
                _loadingOverlay.Visibility = Visibility.Collapsed;
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
            if (_webView == null)
            {
                _webViewDisposed = true;
                return;
            }

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
                if (_root != null)
                    _root.Children.Remove(_webView);
                else if (ReferenceEquals(Content, _webView))
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

        protected override void OnClosed(EventArgs e)
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
            base.OnClosed(e);
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
                EnsureWebViewCreated();
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
                HideLoadingOverlay();
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

        /// <summary>
        /// Lazily create one <see cref="CoreWebView2Environment"/> for the process and reuse it.
        /// Must be awaited on the UI thread (same as <see cref="WebView2.EnsureCoreWebView2Async"/>).
        /// </summary>
        private static Task<CoreWebView2Environment> GetSharedEnvironmentAsync()
        {
            lock (SharedEnvLock)
            {
                if (_sharedEnvironmentTask != null)
                    return _sharedEnvironmentTask;
                _sharedEnvironmentTask = CoreWebView2Environment.CreateAsync();
                return _sharedEnvironmentTask;
            }
        }

        private async Task EnsureAndNavigateAsync()
        {
            if (_webViewDisposed) return;

            PerfTracer?.BeginPhase("WebView.EnsureCore");
            var env = await GetSharedEnvironmentAsync().ConfigureAwait(true);
            if (_webViewDisposed) return;
            await _webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);
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
                    if (_webViewDisposed) return;
                    // Hide overlay on NavigationCompleted (not web-ready) — correctness, not instrumentation.
                    HideLoadingOverlay();
                    NavigationCompleted?.Invoke();
                }));
            };

            PerfTracer?.BeginPhase("WebView.NavigateCall");
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
