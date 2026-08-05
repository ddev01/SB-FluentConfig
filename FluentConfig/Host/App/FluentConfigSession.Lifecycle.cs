using System;
using System.Windows;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Window close handling and host-exit WebView2 teardown.</summary>
    public sealed partial class FluentConfigSession
    {
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Native X/Alt+F4 never reach here until CloseAllowed() — see FluentConfigHostWindow
            // WndProc hook. Closing only runs for confirmed / in-app / host-exit closes.
            if (!e.Cancel)
                _bridge?.Detach();
        }

        /// <summary>
        /// Native chrome close was intercepted before <see cref="Window.Closing"/>. Ask the web
        /// (async — do not DispatcherFrame-wait) so WebView2 can deliver the reply, then
        /// <see cref="FluentConfigHostWindow.CloseAllowed"/> when allowed.
        /// </summary>
        private void OnNativeCloseRequested()
        {
            if (_hostExitInProgress || _closeAlreadyConfirmed || _bridge == null || _window == null)
                return;
            if (_closePromptInFlight)
                return;

            _closePromptInFlight = true;
            try
            {
                _bridge.SendRequest(RpcMethods.WindowCloseRequested, new { }, OnNativeCloseReply);
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] window.closeRequested send failed: {ex.Message}");
                _closePromptInFlight = false;
                // Bridge broken — never leave the X button as a hard no-op.
                FinishNativeClose(allowClose: true, dontRemindAgain: false);
            }
        }

        private void OnNativeCloseReply(JToken resultJson)
        {
            try
            {
                if (_hostExitInProgress || _window == null)
                    return;

                WindowCloseRequestedResult parsed = null;
                try
                {
                    parsed = resultJson?.ToObject<WindowCloseRequestedResult>(ProtocolJson.CreateSerializer());
                }
                catch (Exception ex)
                {
                    Log($"[FluentConfig] window.closeRequested bad result: {ex.Message}");
                }

                // Null = timeout / error / detach. Prefer closing over a permanently stuck X.
                if (parsed == null)
                {
                    FinishNativeClose(allowClose: true, dontRemindAgain: false);
                    return;
                }

                FinishNativeClose(parsed.AllowClose, parsed.DontRemindAgain);
            }
            finally
            {
                _closePromptInFlight = false;
            }
        }

        private void FinishNativeClose(bool allowClose, bool dontRemindAgain)
        {
            if (dontRemindAgain)
            {
                _dontRemindDiscard = true;
                WindowPrefsStore.SetDontRemindDiscard(_cph, _title, true);
            }

            if (!allowClose || _window == null || _hostExitInProgress)
                return;

            _closeAlreadyConfirmed = true;
            _window.CloseAllowed();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            FluentConfigWindowManager.Unregister(_title);

            if (_window != null)
            {
                var geometry = WindowGeometryStore.FromWindow(_window);
                if (geometry != null)
                    WindowGeometryStore.Save(_cph, _title, geometry);

                var bounds = _window.WindowState == WindowState.Maximized
                    ? _window.RestoreBounds
                    : new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
                FluentConfigWindowManager.InvokeWindowClosedCallback(
                    bounds.Left, bounds.Top, bounds.Width, bounds.Height);

                // Idempotent safety net if Closing was skipped somehow.
                _window.DisposeWebViewCore();
            }

            _bridge = null;
            _window = null;
        }

        private static void EnsureHostExitHook()
        {
            lock (HostExitHookLock)
            {
                if (_hostExitHookRegistered) return;
                var app = Application.Current;
                if (app == null) return;

                _hostExitHookRegistered = true;

                // MainWindow.Closing fires before Application.Exit / WebView2 env teardown —
                // dispose our control while COM is still alive.
                var main = app.MainWindow;
                if (main != null)
                    main.Closing += OnHostMainWindowClosing;

                app.SessionEnding += (_, __) => CloseAllWindowsForHostExit();
                app.Exit += (_, __) => CloseAllWindowsForHostExit();
                app.Dispatcher.ShutdownStarted += (_, __) => CloseAllWindowsForHostExit();
            }
        }

        private static void OnHostMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseAllWindowsForHostExit();
        }

        private static void CloseAllWindowsForHostExit()
        {
            Window[] windows;
            lock (HostExitHookLock)
            {
                // Take-all clears the registry so re-entrant Exit/ShutdownStarted/Closing hooks no-op.
                windows = FluentConfigWindowManager.TakeAllWindows();
                if (windows != null && windows.Length > 0)
                    _hostExitInProgress = true;
            }
            if (windows == null || windows.Length == 0) return;

            foreach (var window in windows)
                TeardownWindowForHostExit(window as FluentConfigHostWindow);
        }

        private static void TeardownWindowForHostExit(FluentConfigHostWindow window)
        {
            if (window == null) return;

            try
            {
                void Teardown()
                {
                    try
                    {
                        FluentConfigApp.LogInternal(
                            "[FluentConfig] Host exit — disposing WebView2 before Streamer.bot teardown");
                        // Dispose first (SuppressFinalize even if COM is already dead), then Close.
                        window.DisposeWebViewCore();
                        window.CloseAllowed();
                    }
                    catch (Exception ex)
                    {
                        FluentConfigApp.LogInternal(
                            "[FluentConfig] Host-exit WebView2 teardown: " + ex.Message);
                        try { window.DisposeWebViewCore(); }
                        catch { /* already shutting down */ }
                    }
                }

                if (window.Dispatcher.CheckAccess())
                    Teardown();
                else
                {
                    try { window.Dispatcher.Invoke(Teardown); }
                    catch
                    {
                        // Last resort off the UI thread: still suppress the fatal finalizer.
                        try { window.DisposeWebViewCore(); }
                        catch { /* ignore */ }
                    }
                }
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal(
                    "[FluentConfig] Host-exit WebView2 teardown failed: " + ex.Message);
            }
        }
    }
}
