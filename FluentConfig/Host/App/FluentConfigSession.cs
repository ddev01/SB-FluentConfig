using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Updater;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig
{
    /// <summary>
    /// Owns settings, schema registration, and the WebView2 host bridge for one UI session.
    /// </summary>
    public sealed class FluentConfigSession
    {
        /// <summary>FluentConfig framework version injected into every UiDocument (footer branding).</summary>
        public const string FrameworkVersion = "0.1.0-dev";

        /// <summary>FluentConfig repo URL injected into every UiDocument (footer GitHub link).</summary>
        public const string RepoUrl = "https://github.com/ddev01/SB-FluentConfig";

        private static readonly object HostExitHookLock = new object();
        private static bool _hostExitHookRegistered;
        private static volatile bool _hostExitInProgress;

        private readonly IInlineInvokeProxy _cph;
        private readonly string _title;
        private readonly string _version;
        private readonly string _settingsKey;
        private readonly SettingsManager _settingsManager;
        private readonly PerformanceTracer _perfTracer;
        private readonly List<Func<SectionSchema>> _deferredSections = new List<Func<SectionSchema>>();
        private readonly Dictionary<string, Action<UiContext>> _buttonClicks = new Dictionary<string, Action<UiContext>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Func<IList<DropdownOption>>> _dropdownRefresh = new Dictionary<string, Func<IList<DropdownOption>>>(StringComparer.Ordinal);
        private readonly Dictionary<string, PillRegistration> _pills = new Dictionary<string, PillRegistration>(StringComparer.Ordinal);

        private HostBridge _bridge;
        private FluentConfigHostWindow _window;
        private JObject _latestValues;
        private long _nextRequestId = 1;
        private string _iconPath;
        private UpdateCheckResult _pendingUpdate;
        private string _updateRepo;
        private string _updateCurrentVersion;
        private string _updateTagPrefix;
        private string _updateMode = "self";
        private int _updateCheckStarted;
        private bool _dontRemindDiscard;
        private bool _closeAlreadyConfirmed;
        /// <summary>True while a deferred native-close discard prompt is in flight.</summary>
        private bool _closePromptInFlight;

        private sealed class PillRegistration
        {
            public IList<SchemaNode> ItemTemplate;
            public Action<string, CallbackContext> OnAdded;
            public Action<string, CallbackContext> OnRemoved;
        }

        internal FluentConfigSession(IInlineInvokeProxy cph, string title, string version)
        {
            _cph = cph;
            _title = title ?? "Settings";
            _version = version ?? "1.0";
            _settingsKey = "FluentConfig_Settings_" + _title;
            _settingsManager = new SettingsManager(cph, _settingsKey, Log);
            _perfTracer = new PerformanceTracer(Log);
        }

        internal PerformanceTracer PerfTracer => _perfTracer;

        internal void SetIconPath(string iconPath) => _iconPath = iconPath;

        internal void RegisterDeferredSection(string tabId, string title, Action<SectionBuilder> build)
        {
            _deferredSections.Add(() =>
            {
                var sb = new SectionBuilder(this, tabId, title);
                build?.Invoke(sb);
                return sb.BuildSection();
            });
        }

        internal void RegisterButtonClick(string buttonId, Action<UiContext> callback)
        {
            if (!string.IsNullOrEmpty(buttonId) && callback != null)
                _buttonClicks[buttonId] = callback;
        }

        internal void RegisterDropdownRefresh(string saveKey, Func<IList<DropdownOption>> refresh)
        {
            if (!string.IsNullOrEmpty(saveKey) && refresh != null)
                _dropdownRefresh[saveKey] = refresh;
        }

        internal void RegisterPillCallbacks(
            string saveKey,
            IList<SchemaNode> itemTemplate,
            Action<string, CallbackContext> onAdded,
            Action<string, CallbackContext> onRemoved)
        {
            if (string.IsNullOrEmpty(saveKey)) return;
            _pills[saveKey] = new PillRegistration
            {
                ItemTemplate = itemTemplate,
                OnAdded = onAdded,
                OnRemoved = onRemoved,
            };
        }

        /// <summary>
        /// Store FluentConfig self-update args; HTTP runs after bootstrap (never blocks Show).
        /// </summary>
        public FluentConfigSession ConfigureSelfUpdateCheck(string repo, string currentVersion)
        {
            _updateRepo = repo;
            _updateCurrentVersion = currentVersion;
            _updateTagPrefix = null;
            _updateMode = "self";
            return this;
        }

        /// <summary>
        /// Store notify-only extension update args; HTTP runs after bootstrap (never blocks Show).
        /// </summary>
        public FluentConfigSession ConfigureExtensionUpdateNotice(string repo, string tagPrefix, string currentVersion)
        {
            _updateRepo = repo;
            _updateTagPrefix = tagPrefix;
            _updateCurrentVersion = currentVersion;
            _updateMode = "notify";
            return this;
        }

        /// <summary>Obsolete name — prefer <see cref="ConfigureSelfUpdateCheck"/>.</summary>
        public FluentConfigSession CheckSelfUpdate(string repo, string currentVersion)
            => ConfigureSelfUpdateCheck(repo, currentVersion);

        /// <summary>Obsolete name — prefer <see cref="ConfigureExtensionUpdateNotice"/>.</summary>
        public FluentConfigSession CheckExtensionUpdateNotice(string repo, string tagPrefix, string currentVersion)
            => ConfigureExtensionUpdateNotice(repo, tagPrefix, currentVersion);

        internal void LogExistingSettings()
        {
            // Load from CPH — in-memory may still be empty when called from the build delegate
            // before Show() (common pattern: .LogExistingSettings() inside ShowOrFocus).
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            Log($"[FluentConfig] Existing settings for '{_title}': {settings}");
        }

        /// <summary>
        /// Loads settings and builds the UiDocument without opening a window (tests / harnesses).
        /// </summary>
        internal UiDocument BuildDocumentForTests()
        {
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            var prefs = WindowPrefsStore.Load(_cph, _title);
            _dontRemindDiscard = prefs?.DontRemindDiscard ?? false;
            return BuildDocument();
        }

        /// <summary>Exposes in-memory settings for tests after save RPCs.</summary>
        internal JObject GetSettingsForTests() => _settingsManager.GetSettings();
        internal void Show()
        {
            _perfTracer.Start("Show.begin");
            _closeAlreadyConfirmed = false;
            _closePromptInFlight = false;

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException(
                    "FluentConfig requires STA (enable Run on UI thread on Execute C# Method). " +
                    "Current apartment state: " + Thread.CurrentThread.GetApartmentState());
            }

            if (Application.Current == null)
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            _perfTracer.BeginPhase("Settings.Load");
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();

            var prefs = WindowPrefsStore.Load(_cph, _title);
            _dontRemindDiscard = prefs?.DontRemindDiscard ?? false;

            _perfTracer.BeginPhase("Schema.Build");
            var document = BuildDocument();

            _perfTracer.BeginPhase("Window.Create");
            // ColorScheme is always "dark" today — only dark is supported (no author builder yet).
            var geometry = WindowGeometryStore.Load(_cph, _title);
            _window = new FluentConfigHostWindow(_title, _version, geometry, _iconPath, "dark");
            _window.PerfTracer = _perfTracer;
            _bridge = new HostBridge(_window, this);
            _window.Closing += OnWindowClosing;
            _window.Closed += OnWindowClosed;
            _window.NativeCloseRequested += OnNativeCloseRequested;
            FluentConfigWindowManager.Register(_title, _window);
            EnsureHostExitHook();

            _perfTracer.BeginPhase("Window.Show");
            _window.Show();
            _bridge.Start(document);
            // LogSummary moves to perf.mark("web-ready") — navigation has not started yet here.
        }

        private UiDocument BuildDocument()
        {
            var sections = _deferredSections.Select(f => f()).ToList();

            // Update notices are pushed via update.available after a deferred HTTP check
            // (never injected here — that would block Show on network I/O).

            ExpandPillItems(sections, _latestValues);

            // First menu open writes schema defaults for missing keys only (never overwrites
            // user values). Runtime actions can then use CPH.GetGlobalVar on the settings blob.
            var defaults = SettingsDefaultsCollector.Collect(sections, _latestValues);
            if (SettingsSync.SeedMissingDefaults(_settingsManager, defaults, persist: true) > 0)
                _latestValues = _settingsManager.GetSettings() ?? new JObject();

            return new UiDocument
            {
                Title = _title,
                Version = _version,
                // Only dark is supported today (no .ColorScheme builder).
                ColorScheme = "dark",
                Sections = sections,
                Values = _latestValues,
                FrameworkVersion = FrameworkVersion,
                RepoUrl = RepoUrl,
                DontRemindDiscard = _dontRemindDiscard,
            };
        }

        private void ExpandPillItems(IList<SectionSchema> sections, JObject values)
        {
            if (sections == null) return;
            foreach (var section in sections)
                ExpandPillNodes(section.Children, values);
        }

        private void ExpandPillNodes(IList<SchemaNode> nodes, JObject values)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node is PillInputNode pill && !string.IsNullOrEmpty(pill.SaveKey))
                {
                    var names = values?[pill.SaveKey] as JArray;
                    if (names != null && pill.ItemTemplate != null)
                    {
                        pill.Items = names
                            .Select(t => t?.ToString())
                            .Where(n => !string.IsNullOrEmpty(n))
                            .Select(n => new PillItemSchema
                            {
                                Name = n,
                                Children = ExpandTemplate(pill.ItemTemplate, n),
                            })
                            .ToList();
                    }
                }
                else if (node is GroupNode group)
                {
                    ExpandPillNodes(group.Children, values);
                }
            }
        }

        /// <summary>
        /// Substitute <c>{name}</c> only inside leaf string values of the schema tree
        /// (avoids corrupting JSON when the pill name contains quotes/backslashes).
        /// </summary>
        private static IList<SchemaNode> ExpandTemplate(IList<SchemaNode> template, string name)
        {
            if (template == null) return new List<SchemaNode>();
            var json = ProtocolJson.Serialize(template);
            var root = JToken.Parse(json);
            ReplaceNamePlaceholders(root, name ?? "");
            // Use ProtocolJson.Serialize — not JToken.ToString(Formatting). Streamer.bot may load an
            // older Newtonsoft.Json without that overload (MissingMethodException at runtime).
            return ProtocolJson.Deserialize<List<SchemaNode>>(ProtocolJson.Serialize(root))
                   ?? new List<SchemaNode>();
        }

        private static void ReplaceNamePlaceholders(JToken token, string name)
        {
            if (token == null) return;

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties())
                        ReplaceNamePlaceholders(prop.Value, name);
                    break;
                case JTokenType.Array:
                    foreach (var item in (JArray)token)
                        ReplaceNamePlaceholders(item, name);
                    break;
                case JTokenType.String:
                    var s = token.Value<string>();
                    if (s != null && s.IndexOf("{name}", StringComparison.Ordinal) >= 0)
                        ((JValue)token).Value = s.Replace("{name}", name);
                    break;
            }
        }

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

        // ── Values / settings ──

        internal T GetValue<T>(string key) => _settingsManager.GetValue(key, default(T));

        internal void RemoveSettingsKeys(params string[] keys)
        {
            _settingsManager.RemoveKeys(keys);
            _settingsManager.Save(_settingsManager.GetSettings());
            _latestValues = _settingsManager.GetSettings();
        }

        internal T GetPendingValue<T>(string key, JObject snapshot)
        {
            var source = snapshot ?? _latestValues ?? _settingsManager.GetSettings();
            if (source == null || string.IsNullOrEmpty(key))
                return default;
            var token = SettingsPathHelper.GetNestedValue(source, key) ?? source[key];
            if (token == null || token.Type == JTokenType.Null)
                return default;
            try { return token.ToObject<T>(); }
            catch { return default; }
        }

        internal void PushSchemaPatch(string sectionId, string nodeId, SchemaNode node)
        {
            _bridge?.Send(WireMessage.Push(PushEventNames.SchemaPatch, new SchemaPatchPayload
            {
                SectionId = sectionId,
                NodeId = nodeId,
                Node = node,
            }));
        }

        // ── Bridge RPC handlers ──

        internal void HandleWebMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Log("[FluentConfig] Empty web message (expected JSON string from postMessage)");
                return;
            }

            WireMessage msg;
            try { msg = ProtocolJson.Deserialize<WireMessage>(json); }
            catch (Exception ex)
            {
                Log($"[FluentConfig] Bad wire message: {ex.Message}");
                return;
            }
            if (msg == null) return;

            if (msg.Kind == WireKinds.Request)
                HandleRequest(msg);
            else if (msg.Kind == WireKinds.Response)
                _bridge?.CompletePendingResponse(msg);
        }

        private void HandleRequest(WireMessage msg)
        {
            if (msg.Id == null)
            {
                Log("[FluentConfig] Request missing id");
                return;
            }

            try
            {
                switch (msg.Method)
                {
                    case RpcMethods.Save:
                        HandleSave(msg);
                        break;
                    case RpcMethods.DropdownRefresh:
                        HandleDropdownRefresh(msg);
                        break;
                    case RpcMethods.ButtonClick:
                        HandleButtonClick(msg);
                        break;
                    case RpcMethods.FilepathBrowse:
                        HandleFilepathBrowse(msg);
                        break;
                    case RpcMethods.PillChanged:
                        HandlePillChanged(msg);
                        break;
                    case RpcMethods.UpdateStage:
                        HandleUpdateStage(msg);
                        break;
                    case RpcMethods.UpdateDismiss:
                        _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
                        break;
                    case RpcMethods.WindowClose:
                        HandleWindowClose(msg);
                        break;
                    case RpcMethods.ShellOpenUrl:
                        HandleShellOpenUrl(msg);
                        break;
                    case RpcMethods.PerfMark:
                        HandlePerfMark(msg);
                        break;
                    case RpcMethods.Log:
                        var logParams = msg.Params?.ToObject<LogParams>(ProtocolJson.CreateSerializer());
                        if (!string.IsNullOrEmpty(logParams?.Message))
                            Log(logParams.Message);
                        _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
                        break;
                    default:
                        _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "method_not_found", "Unknown method: " + msg.Method));
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] RPC '{msg.Method}' failed: {ex.Message}");
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "internal", ex.Message));
            }
        }

        private void HandleSave(WireMessage msg)
        {
            var p = msg.Params?.ToObject<SaveParams>(ProtocolJson.CreateSerializer());
            _latestValues = SettingsSync.ApplyAndSave(_settingsManager, p?.Values);
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { ok = true }));
        }

        private void HandleDropdownRefresh(WireMessage msg)
        {
            var p = msg.Params?.ToObject<DropdownRefreshParams>(ProtocolJson.CreateSerializer());
            IList<DropdownOption> options = Array.Empty<DropdownOption>();
            if (p != null && !string.IsNullOrEmpty(p.SaveKey) && _dropdownRefresh.TryGetValue(p.SaveKey, out var fn))
                options = fn() ?? Array.Empty<DropdownOption>();
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new DropdownRefreshResult { Options = options }));
        }

        private void HandleButtonClick(WireMessage msg)
        {
            var p = msg.Params?.ToObject<ButtonClickParams>(ProtocolJson.CreateSerializer());
            if (p?.Values != null)
                _latestValues = p.Values;

            Action<UiContext> cb = null;
            if (p != null && !string.IsNullOrEmpty(p.ButtonId))
                _buttonClicks.TryGetValue(p.ButtonId, out cb);

            // Reply before running OnClick. Nested SendRequestAndWait (confirm/popup) inside
            // WebMessageReceived deadlocks — WebView2 won't deliver the dialog response until
            // this handler returns. Defer the callback so confirm/progress RPCs can complete.
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            if (cb == null) return;

            var ctx = new UiContext(this, _latestValues);
            var dispatcher = _window?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        cb(ctx);
                    }
                    catch (Exception ex)
                    {
                        Log($"[FluentConfig] button.click handler failed: {ex.Message}");
                    }
                }));
            }
            else
            {
                try
                {
                    cb(ctx);
                }
                catch (Exception ex)
                {
                    Log($"[FluentConfig] button.click handler failed: {ex.Message}");
                }
            }
        }

        private void HandleFilepathBrowse(WireMessage msg)
        {
            string path = null;
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog(_window) == true)
                path = dlg.FileName;
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new FilepathBrowseResult { Path = path }));
        }

        private void HandlePillChanged(WireMessage msg)
        {
            var p = msg.Params?.ToObject<PillChangedParams>(ProtocolJson.CreateSerializer());
            var result = new PillChangedResult();
            var ctx = new CallbackContext(this);

            if (p != null && !string.IsNullOrEmpty(p.SaveKey) && _pills.TryGetValue(p.SaveKey, out var reg))
            {
                if (p.Action == "add" && !string.IsNullOrEmpty(p.Name))
                    reg.OnAdded?.Invoke(p.Name, ctx);
                else if (p.Action == "remove" && !string.IsNullOrEmpty(p.Name))
                    reg.OnRemoved?.Invoke(p.Name, ctx);

                if (reg.ItemTemplate != null && p.Items != null)
                {
                    result.Items = p.Items
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Select(n => new PillItemSchema
                        {
                            Name = n,
                            Children = ExpandTemplate(reg.ItemTemplate, n),
                        })
                        .ToList();
                }
            }

            if (p?.Items != null && !string.IsNullOrEmpty(p.SaveKey))
            {
                _settingsManager.SetValue(p.SaveKey, p.Items.ToArray());
                _latestValues = _settingsManager.GetSettings();
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, result));
        }

        private void HandleUpdateStage(WireMessage msg)
        {
            var p = msg.Params?.ToObject<UpdateStageParams>(ProtocolJson.CreateSerializer());
            if (p == null || string.IsNullOrEmpty(p.DownloadUrl))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "downloadUrl required"));
                return;
            }

            // Self-update always targets this assembly — no author-supplied dllPath override.
            var target = System.Reflection.Assembly.GetExecutingAssembly().Location;
            GitHubUpdater.StageUpdate(p.DownloadUrl, target);
            UpdateHelperLauncher.LaunchSwapAndRelaunch(target, "Streamer.bot.exe");
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { staged = true }));
        }

        private void HandleWindowClose(WireMessage msg)
        {
            var p = msg.Params?.ToObject<WindowCloseParams>(ProtocolJson.CreateSerializer());
            if (p != null && p.AlreadyConfirmed)
                _closeAlreadyConfirmed = true;
            if (p != null && p.DontRemindAgain)
            {
                _dontRemindDiscard = true;
                WindowPrefsStore.SetDontRemindDiscard(_cph, _title, true);
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            // WebMessageReceived already runs on the UI thread.
            _window?.CloseAllowed();
        }

        private void HandleShellOpenUrl(WireMessage msg)
        {
            var p = msg.Params?.ToObject<ShellOpenUrlParams>(ProtocolJson.CreateSerializer());
            var url = p?.Url?.Trim();
            if (string.IsNullOrEmpty(url)
                || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "url must be http(s)"));
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] shell.openUrl failed: {ex.Message}");
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
        }

        private void HandlePerfMark(WireMessage msg)
        {
            var p = msg.Params?.ToObject<PerfMarkParams>(ProtocolJson.CreateSerializer());
            var name = p?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _perfTracer.Mark(name);
                if (string.Equals(name, "web-ready", StringComparison.OrdinalIgnoreCase))
                {
                    _perfTracer.LogSummary();
                    ExportPerfLastToCph();
                }
            }
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
        }

        /// <summary>
        /// Writes the last PerfTrace summary to CPH global <c>FluentConfig_PerfLast</c>
        /// (no-op unless compiled with FC_PERF_TRACE and summary has closed).
        /// </summary>
        private void ExportPerfLastToCph()
        {
#if FC_PERF_TRACE
            try
            {
                var json = _perfTracer.ToSummaryJson();
                if (json == null) return;
                _cph.SetGlobalVar("FluentConfig_PerfLast", json, true);
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] FluentConfig_PerfLast export failed: {ex.Message}");
            }
#endif
        }

        internal void OnWebReady()
        {
            BeginDeferredUpdateCheck();
        }

        /// <summary>
        /// Run configured update HTTP off the Show critical path after bootstrap is sent.
        /// Pushes <c>update.available</c> on the UI thread when a newer release exists.
        /// </summary>
        private void BeginDeferredUpdateCheck()
        {
            if (string.IsNullOrWhiteSpace(_updateRepo))
                return;
            if (Interlocked.Exchange(ref _updateCheckStarted, 1) != 0)
                return;

            var repo = _updateRepo;
            var currentVersion = _updateCurrentVersion;
            var tagPrefix = _updateTagPrefix;
            var mode = _updateMode ?? "self";
            var isNotify = string.Equals(mode, "notify", StringComparison.Ordinal);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    UpdateCheckResult result = isNotify
                        ? GitHubUpdater.CheckForTaggedRelease(repo, tagPrefix, currentVersion)
                        : GitHubUpdater.CheckForUpdate(repo, currentVersion);

                    if (result == null || !result.UpdateAvailable)
                        return;

                    var dispatcher = _window?.Dispatcher;
                    if (dispatcher == null)
                        return;

                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_bridge == null)
                            return;
                        _pendingUpdate = result;
                        PushUpdateAvailableNotice();
                    }));
                }
                catch (Exception ex)
                {
                    Log("[FluentConfig] deferred update check failed: " + ex.Message);
                }
            });
        }

        private void PushUpdateAvailableNotice()
        {
            if (_pendingUpdate == null || !_pendingUpdate.UpdateAvailable)
                return;

            var isNotify = string.Equals(_updateMode, "notify", StringComparison.Ordinal);
            _bridge?.Send(WireMessage.Push(PushEventNames.UpdateAvailable, new UpdateAvailablePayload
            {
                NoticeId = isNotify ? "extension-update" : "self-update",
                CurrentVersion = _pendingUpdate.CurrentVersion,
                LatestVersion = _pendingUpdate.LatestVersion,
                ReleaseNotes = _pendingUpdate.ReleaseNotes,
                DownloadUrl = _pendingUpdate.DownloadUrl,
                Repo = _updateRepo,
                Mode = _updateMode ?? "self",
                ReleasePageUrl = _pendingUpdate.ReleasePageUrl,
            }));
        }

        // ── UiContext affordances ──

        internal void Toast(string message)
        {
            _bridge?.SendRequestFireAndForget(RpcMethods.Toast, new ToastParams { Message = message });
        }

        internal void Popup(string title, string message)
        {
            _bridge?.SendRequestFireAndForget(RpcMethods.DialogPopup, new PopupParams { Title = title, Message = message });
        }

        internal bool ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_bridge == null)
                return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;

            var resultJson = _bridge.SendRequestAndWait(RpcMethods.DialogConfirm, new ConfirmParams
            {
                Title = title,
                Message = message,
                ConfirmText = yesButton,
                CancelText = noButton,
            });

            try
            {
                var parsed = resultJson?.ToObject<ConfirmResult>(ProtocolJson.CreateSerializer());
                return parsed != null && parsed.Confirmed;
            }
            catch
            {
                return false;
            }
        }

        internal IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            var id = "progress-" + Interlocked.Increment(ref _nextRequestId);
            return new BridgeProgressReporter(_bridge, _window?.Dispatcher, id, title, message, total);
        }

        internal void Log(string message) => FluentConfigApp.LogInternal(message);

        internal long NextRequestId() => Interlocked.Increment(ref _nextRequestId);
    }

    internal sealed class BridgeProgressReporter : IProgressReporter
    {
        private readonly HostBridge _bridge;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;
        private readonly string _id;
        private readonly string _title;
        private readonly int _total;
        private string _message;

        public BridgeProgressReporter(
            HostBridge bridge,
            System.Windows.Threading.Dispatcher dispatcher,
            string id,
            string title,
            string message,
            int total)
        {
            _bridge = bridge;
            _dispatcher = dispatcher;
            _id = id;
            _title = title;
            _message = message;
            _total = total;
            Push(0, false);
        }

        public void Report(int current)
        {
            Push(current, false);
        }

        public void Close()
        {
            Push(_total, true);
        }

        private void Push(int current, bool done)
        {
            if (_bridge == null) return;

            // Example OnClick handlers often Report from Task.Run — marshal to the UI
            // dispatcher so WebView2 posts aren't racing a busy/non-UI thread.
            if (_dispatcher != null && !_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(() => Push(current, done)));
                return;
            }

            double? percent = _total > 0 ? (100.0 * current / _total) : (double?)null;
            _bridge.Send(WireMessage.Push(PushEventNames.Progress, new ProgressPayload
            {
                Id = _id,
                Title = _title,
                Message = _message,
                Current = current,
                Total = _total,
                Percent = percent,
                Done = done,
            }));
        }
    }
}
