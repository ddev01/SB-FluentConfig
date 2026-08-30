using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Updater;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig
{
    /// <summary>
    /// Owns settings, schema registration, and the WebView2 host bridge for one UI session.
    /// </summary>
    public sealed partial class FluentConfigSession
    {
        /// <summary>FluentConfig framework version injected into every UiDocument (footer branding). Sourced from Host.csproj InformationalVersion at build time.</summary>
        public static string FrameworkVersion => FluentConfigBuildInfo.FrameworkVersion;
        /// <summary>
        /// Default footer repo URL. Generic placeholder — override at build time with
        /// <c>-p:FluentConfigRepoUrl=...</c> or at runtime via <see cref = "SetRepoUrl"/>.
        /// </summary>
        public static string DefaultRepoUrl => FluentConfigBuildInfo.RepoUrl;

        private static readonly object HostExitHookLock = new object ();
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
        private readonly Dictionary<string, (double Min, double Max)> _declaredRanges = new Dictionary<string, (double Min, double Max)>(StringComparer.Ordinal);
        private readonly List<FilepathRule> _filepathRules = new List<FilepathRule>();
        internal string ItemTemplatePillKey;
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
        private int _itemTemplateDepth;
        internal bool InItemTemplate => _itemTemplateDepth > 0;

        internal void PushItemTemplateScope() => _itemTemplateDepth++;
        internal void PopItemTemplateScope()
        {
            if (_itemTemplateDepth > 0)
                _itemTemplateDepth--;
        }

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
            global::FluentConfig.FluentConfig.MigrateLegacySettingsKey(cph, _title);
            _settingsKey = SettingsKeyHelper.SettingsKeyFor(_title);
            _settingsManager = new SettingsManager(cph, _settingsKey, Log);
            _perfTracer = new PerformanceTracer(Log);
        }

        internal PerformanceTracer PerfTracer => _perfTracer;

        /// <summary>Per-menu persistence identity. W3 replaces the body with the explicit menu id.</summary>
        internal string ScopeKey => _title;

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

        internal void RegisterPillCallbacks(string saveKey, IList<SchemaNode> itemTemplate, Action<string, CallbackContext> onAdded, Action<string, CallbackContext> onRemoved)
        {
            if (string.IsNullOrEmpty(saveKey))
                return;
            _pills[saveKey] = new PillRegistration
            {
                ItemTemplate = itemTemplate,
                OnAdded = onAdded,
                OnRemoved = onRemoved,
            };
        }

        /// <summary>
        /// Records Min/Max from a flushed IntegerInput / NumberInput / Slider so
        /// <c>RepeatFor</c> can auto-detect the driver's range across section boundaries.
        /// </summary>
        internal void RegisterDeclaredRange(string saveKey, double min, double max)
        {
            if (string.IsNullOrEmpty(saveKey))
                return;
            _declaredRanges[saveKey] = (min, max);
        }

        internal void ClearFilepathRules() => _filepathRules.Clear();
        internal void RegisterFilepathRule(FilepathRule rule)
        {
            if (rule != null && !string.IsNullOrEmpty(rule.SaveKey))
                _filepathRules.Add(rule);
        }

        internal bool TryGetDeclaredRange(string saveKey, out double min, out double max)
        {
            if (!string.IsNullOrEmpty(saveKey) && _declaredRanges.TryGetValue(saveKey, out var range))
            {
                min = range.Min;
                max = range.Max;
                return true;
            }

            min = 0;
            max = 0;
            return false;
        }

        /// <summary>
        /// Store notify-only extension update args; HTTP runs after bootstrap (never blocks Show).
        /// Empty <paramref name = "tagPrefix"/> → <c>releases/latest</c>; otherwise tagged prefix.
        /// </summary>
        public FluentConfigSession ConfigureExtensionUpdateNotice(string repo, string currentVersion, string tagPrefix = null, string minStreamerBot = null, string minFluentConfig = null, string updateGuideUrl = null)
        {
            _updateRepo = repo;
            _updateCurrentVersion = currentVersion;
            _updateTagPrefix = string.IsNullOrWhiteSpace(tagPrefix) ? null : tagPrefix.Trim();
            _updateMinStreamerBot = string.IsNullOrWhiteSpace(minStreamerBot) ? null : minStreamerBot.Trim();
            _updateMinFluentConfig = string.IsNullOrWhiteSpace(minFluentConfig) ? null : minFluentConfig.Trim();
            _updateGuideUrl = string.IsNullOrWhiteSpace(updateGuideUrl) ? null : updateGuideUrl.Trim();
            return this;
        }

        internal void Show()
        {
            _perfTracer.Start("Show.begin");
            _closeAlreadyConfirmed = false;
            _closePromptInFlight = false;
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("FluentConfig requires STA (enable Run on UI thread on Execute C# Method). " + "Current apartment state: " + Thread.CurrentThread.GetApartmentState());
            }

            if (!TryValidateExtensionVersionGates(out var gateMessage))
            {
                MessageBox.Show(gateMessage, _title, MessageBoxButton.OK, MessageBoxImage.Warning);
                FluentConfigWindowManager.Unregister(ScopeKey);
                return;
            }

            // Idempotent: if this session already owns a live window, focus it instead of orphaning.
            if (_window != null)
            {
                try
                {
                    if (_window.IsLoaded || _window.IsVisible)
                    {
                        FluentConfigWindowManager.AlreadyOpened(ScopeKey, _version, Log);
                        return;
                    }
                }
                catch
                {
                // Fall through and create a replacement window.
                }
            }

            if (Application.Current == null)
                new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            _perfTracer.BeginPhase("Settings.Load");
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            var prefs = WindowPrefsStore.Load(_cph, ScopeKey);
            _dontRemindDiscard = prefs?.DontRemindDiscard ?? false;
            _perfTracer.BeginPhase("Schema.Build");
            var document = BuildDocument();
            // Overlap CoreWebView2Environment.CreateAsync with Window.Create / Show (UI thread only).
            FluentConfigHostWindow.KickoffSharedEnvironment();
            _perfTracer.BeginPhase("Window.Create");
            // ColorScheme is always "dark" today — only dark is supported (no author builder yet).
            var geometry = WindowGeometryStore.Load(_cph, ScopeKey);
            _window = new FluentConfigHostWindow(_title, _version, geometry, _iconPath, "dark");
            _window.PerfTracer = _perfTracer;
            _bridge = new HostBridge(_window, this);
            _window.Closing += OnWindowClosing;
            _window.Closed += OnWindowClosed;
            _window.NativeCloseRequested += OnNativeCloseRequested;
            FluentConfigWindowManager.Register(ScopeKey, _window);
            EnsureHostExitHook();
            _perfTracer.BeginPhase("Window.Show");
            _window.Show();
            _bridge.Start(document);
        // LogSummary moves to perf.mark("web-ready") — navigation has not started yet here.
        }

        /// <summary>Override the footer GitHub / docs link for this session.</summary>
        internal void SetRepoUrl(string repoUrl)
        {
            if (!string.IsNullOrWhiteSpace(repoUrl))
                _repoUrl = repoUrl.Trim();
        }
    }
}
