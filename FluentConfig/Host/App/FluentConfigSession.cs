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
    public sealed partial class FluentConfigSession
    {
        /// <summary>FluentConfig framework version injected into every UiDocument (footer branding).</summary>
        public const string FrameworkVersion = "0.1.0-beta.1";

        /// <summary>
        /// Default footer repo URL. Generic placeholder â€” override at build time with
        /// <c>-p:FluentConfigRepoUrl=...</c> or at runtime via <see cref="SetRepoUrl"/>.
        /// </summary>
        public static string DefaultRepoUrl => FluentConfigBuildInfo.RepoUrl;

        /// <summary>FluentConfig repo URL injected into every UiDocument (footer GitHub link).</summary>
        [Obsolete("Use DefaultRepoUrl or instance SetRepoUrl / FluentConfigUi.RepoUrl.")]
        public static string RepoUrl => DefaultRepoUrl;

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
        private string _repoUrl = DefaultRepoUrl;
        private UpdateCheckResult _pendingUpdate;
        private string _updateRepo;
        private string _updateCurrentVersion;
        private string _updateTagPrefix;
        private string _updateMinStreamerBot;
        private string _updateMinFluentConfig;
        private string _updateGuideUrl;
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
        /// Store notify-only extension update args; HTTP runs after bootstrap (never blocks Show).
        /// Empty <paramref name="tagPrefix"/> → <c>releases/latest</c>; otherwise tagged prefix.
        /// </summary>
        public FluentConfigSession ConfigureExtensionUpdateNotice(
            string repo,
            string currentVersion,
            string tagPrefix = null,
            string minStreamerBot = null,
            string minFluentConfig = null,
            string updateGuideUrl = null)
        {
            _updateRepo = repo;
            _updateCurrentVersion = currentVersion;
            _updateTagPrefix = string.IsNullOrWhiteSpace(tagPrefix) ? null : tagPrefix.Trim();
            _updateMinStreamerBot = string.IsNullOrWhiteSpace(minStreamerBot) ? null : minStreamerBot.Trim();
            _updateMinFluentConfig = string.IsNullOrWhiteSpace(minFluentConfig) ? null : minFluentConfig.Trim();
            _updateGuideUrl = string.IsNullOrWhiteSpace(updateGuideUrl) ? null : updateGuideUrl.Trim();
            return this;
        }

        internal void LogExistingSettings()
        {
            // Load from CPH â€” in-memory may still be empty when called from the build delegate
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

            if (!TryValidateExtensionVersionGates(out var gateMessage))
            {
                MessageBox.Show(
                    gateMessage,
                    _title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                FluentConfigWindowManager.Unregister(_title);
                return;
            }

            // Idempotent: if this session already owns a live window, focus it instead of orphaning.
            if (_window != null)
            {
                try
                {
                    if (_window.IsLoaded || _window.IsVisible)
                    {
                        FluentConfigWindowManager.AlreadyOpened(_title, _version, Log);
                        return;
                    }
                }
                catch
                {
                    // Fall through and create a replacement window.
                }
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

            // Overlap CoreWebView2Environment.CreateAsync with Window.Create / Show (UI thread only).
            FluentConfigHostWindow.KickoffSharedEnvironment();

            _perfTracer.BeginPhase("Window.Create");
            // ColorScheme is always "dark" today â€” only dark is supported (no author builder yet).
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
            // LogSummary moves to perf.mark("web-ready") â€” navigation has not started yet here.
        }

        private UiDocument BuildDocument()
        {
            var sections = _deferredSections.Select(f => f()).ToList();

            // Update notices are pushed via update.available after a deferred HTTP check
            // (never injected here â€” that would block Show on network I/O).

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
                RepoUrl = _repoUrl,
                DontRemindDiscard = _dontRemindDiscard,
            };
        }

        /// <summary>Override the footer GitHub / docs link for this session.</summary>
        internal void SetRepoUrl(string repoUrl)
        {
            if (!string.IsNullOrWhiteSpace(repoUrl))
                _repoUrl = repoUrl.Trim();
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
            // Use ProtocolJson.Serialize â€” not JToken.ToString(Formatting). Streamer.bot may load an
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

        // â”€â”€ Values / settings â”€â”€

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
    }
}

