using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
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
        private string _headerImageUrl;
        private string _iconPath;
        private UpdateCheckResult _pendingUpdate;
        private string _updateTargetPath;
        private string _updateRepo;

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

        internal void SetHeader(string imageUrl) => _headerImageUrl = imageUrl;

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
        /// Optional self-update check. When an update is available, an update-notice node is added
        /// and an <c>update.available</c> event is pushed after bootstrap.
        /// </summary>
        public FluentConfigSession CheckSelfUpdate(string repo, string currentVersion, string dllPath = null)
        {
            try
            {
                var result = GitHubUpdater.CheckForUpdate(repo, currentVersion);
                if (result != null && result.UpdateAvailable)
                {
                    _pendingUpdate = result;
                    _updateRepo = repo;
                    _updateTargetPath = dllPath;
                }
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] CheckSelfUpdate failed: {ex.Message}");
            }
            return this;
        }

        internal void LogExistingSettings()
        {
            var settings = _settingsManager.GetSettings();
            Log($"[FluentConfig] Existing settings for '{_title}': {settings}");
        }

        /// <summary>
        /// Loads settings and builds the UiDocument without opening a window (tests / harnesses).
        /// </summary>
        internal UiDocument BuildDocumentForTests()
        {
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            return BuildDocument();
        }

        /// <summary>Exposes in-memory settings for tests after save RPCs.</summary>
        internal JObject GetSettingsForTests() => _settingsManager.GetSettings();

        internal void Show()
        {
            _perfTracer.Start("Show.begin");

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

            _perfTracer.BeginPhase("Schema.Build");
            var document = BuildDocument();

            _perfTracer.BeginPhase("Window.Create");
            FluentConfigWindowManager.SetOpened(true);
            var geometry = WindowGeometryStore.Load(_cph, _title);
            var colorScheme = document?.ColorScheme ?? "dark";
            _window = new FluentConfigHostWindow(_title, _version, geometry, _iconPath, colorScheme);
            _bridge = new HostBridge(_window, this);
            _window.Closed += OnWindowClosed;

            _perfTracer.BeginPhase("Window.Show");
            _window.Show();
            _bridge.Start(document);

            _perfTracer.EndPhase();
            _perfTracer.LogSummary();
        }

        private UiDocument BuildDocument()
        {
            var sections = _deferredSections.Select(f => f()).ToList();

            if (_pendingUpdate != null && _pendingUpdate.UpdateAvailable && sections.Count > 0)
            {
                var notice = new UpdateNoticeNode
                {
                    Id = "self-update",
                    CurrentVersion = _pendingUpdate.CurrentVersion,
                    LatestVersion = _pendingUpdate.LatestVersion,
                    ReleaseNotes = _pendingUpdate.ReleaseNotes,
                    DownloadUrl = _pendingUpdate.DownloadUrl,
                    Repo = _updateRepo,
                    Dismissible = true,
                };
                var first = sections[0];
                var children = first.Children?.ToList() ?? new List<SchemaNode>();
                children.Insert(0, notice);
                first.Children = children;
            }

            ExpandPillItems(sections, _latestValues);

            return new UiDocument
            {
                Title = _title,
                Version = _version,
                ColorScheme = "dark",
                Sections = sections,
                Values = _latestValues,
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

        private static IList<SchemaNode> ExpandTemplate(IList<SchemaNode> template, string name)
        {
            if (template == null) return new List<SchemaNode>();
            // Re-serialize with placeholder substitution for "{name}"
            var json = ProtocolJson.Serialize(template);
            json = json.Replace("{name}", name);
            return ProtocolJson.Deserialize<List<SchemaNode>>(json) ?? new List<SchemaNode>();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            FluentConfigWindowManager.SetOpened(false);
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
            }
            _bridge = null;
            _window = null;
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

            if (p != null && !string.IsNullOrEmpty(p.ButtonId) && _buttonClicks.TryGetValue(p.ButtonId, out var cb))
            {
                var ctx = new UiContext(this, _latestValues);
                cb(ctx);
            }
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
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

            var target = _updateTargetPath;
            if (string.IsNullOrEmpty(target))
                target = System.Reflection.Assembly.GetExecutingAssembly().Location;

            GitHubUpdater.StageUpdate(p.DownloadUrl, target);
            UpdateHelperLauncher.LaunchSwapAndRelaunch(target, "Streamer.bot.exe");
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { staged = true }));
        }

        internal void OnWebReady()
        {
            if (_pendingUpdate != null && _pendingUpdate.UpdateAvailable)
            {
                _bridge?.Send(WireMessage.Push(PushEventNames.UpdateAvailable, new UpdateAvailablePayload
                {
                    NoticeId = "self-update",
                    CurrentVersion = _pendingUpdate.CurrentVersion,
                    LatestVersion = _pendingUpdate.LatestVersion,
                    ReleaseNotes = _pendingUpdate.ReleaseNotes,
                    DownloadUrl = _pendingUpdate.DownloadUrl,
                    Repo = _updateRepo,
                }));
            }
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

        internal MessageBoxResult ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_bridge == null)
                return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes
                    ? MessageBoxResult.Yes : MessageBoxResult.No;

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
                return parsed != null && parsed.Confirmed ? MessageBoxResult.Yes : MessageBoxResult.No;
            }
            catch
            {
                return MessageBoxResult.No;
            }
        }

        internal IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            var id = "progress-" + Interlocked.Increment(ref _nextRequestId);
            return new BridgeProgressReporter(_bridge, id, title, message, total);
        }

        internal void Log(string message) => FluentConfigApp.LogInternal(message);

        internal long NextRequestId() => Interlocked.Increment(ref _nextRequestId);
    }

    internal sealed class BridgeProgressReporter : IProgressReporter
    {
        private readonly HostBridge _bridge;
        private readonly string _id;
        private readonly string _title;
        private readonly int _total;
        private string _message;

        public BridgeProgressReporter(HostBridge bridge, string id, string title, string message, int total)
        {
            _bridge = bridge;
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
            double? percent = _total > 0 ? (100.0 * current / _total) : (double?)null;
            _bridge?.Send(WireMessage.Push(PushEventNames.Progress, new ProgressPayload
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
