using System;
using System.Collections.Generic;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig
{
    /// <summary>
    /// Static entry helpers: AlreadyOpened, SetLogCallback, logging.
    /// Named FluentConfigApp to avoid clashing with the FluentConfig namespace/type pattern.
    /// Plugin authors still call <c>FluentConfig.FluentConfig.AlreadyOpened</c> via the alias type below.
    /// </summary>
    public static class FluentConfigApp
    {
        private static Action<string> _logCallback;

        static FluentConfigApp()
        {
            WebView2AssemblyResolve.EnsureInitialized();
        }

        public static void SetLogCallback(Action<string> callback) => _logCallback = callback;

        public static void SetWindowClosedCallback(Action<double, double> callback)
            => FluentConfigWindowManager.SetWindowClosedCallback(callback);

        public static void SetWindowClosedCallback(Action<double, double, double, double> callback)
            => FluentConfigWindowManager.SetWindowClosedCallback(callback);

        public static bool AlreadyOpened(string title = "FluentConfig", string version = "1.0")
            => FluentConfigWindowManager.AlreadyOpened(title, version, LogInternal);

        public static bool IsOpen => FluentConfigWindowManager.IsOpen;

        internal static void LogInternal(string message)
        {
            if (_logCallback != null)
                _logCallback(message);
            else
                System.Diagnostics.Debug.WriteLine($"[FluentConfig] {message}");
        }
    }

    /// <summary>
    /// Compatibility façade matching the old <c>FluentConfig.FluentConfig</c> static API
    /// used by examples (<c>FluentConfig.FluentConfig.AlreadyOpened</c>, <c>SetLogCallback</c>).
    /// Also exposes framework version / update constants for docs and extension min-gates.
    /// </summary>
    public static class FluentConfig
    {
        /// <summary>
        /// Placeholder GitHub repo for FluentConfig.dll releases (<c>owner/name</c>).
        /// Replace when publishing; DllCheck actions use their own copy of this constant.
        /// </summary>
        public const string UpdateRepo = "ddev01/SB-FluentConfig";

        /// <summary>
        /// Minimum Streamer.bot version required by this FluentConfig build (semver-ish).
        /// Mirrored as a constant in DllCheck examples (which cannot reference this assembly).
        /// </summary>
        public const string MinStreamerBotVersion = "1.0.0";

        static FluentConfig()
        {
            WebView2AssemblyResolve.EnsureInitialized();
        }

        /// <summary>FluentConfig framework version string (same as UiDocument footer).</summary>
        public static string GetVersion() => FluentConfigSession.FrameworkVersion;

        public static void SetLogCallback(Action<string> callback) => FluentConfigApp.SetLogCallback(callback);
        public static void SetWindowClosedCallback(Action<double, double> callback) => FluentConfigApp.SetWindowClosedCallback(callback);
        public static void SetWindowClosedCallback(Action<double, double, double, double> callback) => FluentConfigApp.SetWindowClosedCallback(callback);
        public static bool AlreadyOpened(string title = "FluentConfig", string version = "1.0") => FluentConfigApp.AlreadyOpened(title, version);
        public static bool IsOpen => FluentConfigApp.IsOpen;

        /// <summary>Slugify a menu title (e.g. "First Chatters" → "first_chatters").</summary>
        public static string SlugFor(string title) => SettingsKeyHelper.Slugify(title);

        /// <summary>Build <c>{slug}_{suffix}</c> for a menu title (e.g. counter/users state vars).</summary>
        public static string KeyFor(string title, string suffix) => SettingsKeyHelper.KeyFor(title, suffix);

        /// <summary>Settings-blob global key for a menu title (e.g. "First Chatters" → "first_chatters_settings").</summary>
        public static string SettingsKeyFor(string title) => SettingsKeyHelper.SettingsKeyFor(title);

        /// <summary>
        /// Load a typed settings object from the CPH global for <paramref name="title"/>.
        /// Missing/malformed fields keep the property defaults from <c>new T()</c>;
        /// optional <paramref name="validate"/> runs afterward for domain clamping.
        /// JSON keys are expected in snake_case (matching FluentConfig save keys).
        /// </summary>
        public static T LoadSettings<T>(IInlineInvokeProxy cph, string title, Action<T> validate = null)
            where T : new()
        {
            var result = new T();
            if (cph == null)
            {
                validate?.Invoke(result);
                return result;
            }

            string json = null;
            try
            {
                json = cph.GetGlobalVar<string>(SettingsKeyHelper.SettingsKeyFor(title), true);
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal($"LoadSettings GetGlobalVar failed: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    JsonConvert.PopulateObject(json, result, CreateSnakeCaseSettings());
                }
                catch (Exception ex)
                {
                    FluentConfigApp.LogInternal($"LoadSettings PopulateObject failed: {ex.Message}");
                }
            }

            validate?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Read a single JSON field (or nested path) from the settings blob for <paramref name="title"/>.
        /// Useful for flat numbered keys (e.g. <c>points_1</c>) that do not map cleanly onto a typed property.
        /// </summary>
        public static TValue GetSetting<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonKey,
            TValue defaultValue = default)
        {
            if (cph == null || string.IsNullOrEmpty(jsonKey))
                return defaultValue;

            var mgr = OpenSettingsManager(cph, title);
            mgr.Load();
            return mgr.GetValue(jsonKey, defaultValue);
        }

        /// <summary>
        /// Write a single JSON field (or nested path) on the settings blob and persist.
        /// Other keys are preserved (read-modify-write).
        /// </summary>
        public static void SetSetting<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonKey,
            TValue value)
        {
            if (cph == null || string.IsNullOrEmpty(jsonKey))
                return;

            var mgr = OpenSettingsManager(cph, title);
            mgr.Load();
            mgr.SetValue(jsonKey, value);
            mgr.Save(mgr.GetSettings());
        }

        /// <summary>
        /// Load the settings blob, apply <paramref name="mutate"/>, and persist in one write.
        /// Useful for resetting several keys without opening the UI.
        /// </summary>
        public static void SaveSettings(IInlineInvokeProxy cph, string title, Action<JObject> mutate)
        {
            if (cph == null || mutate == null)
                return;

            var mgr = OpenSettingsManager(cph, title);
            mgr.Load();
            var current = mgr.GetSettings() ?? new JObject();
            mutate(current);
            mgr.ReplaceSettings(current);
            mgr.Save(current);
        }

        /// <summary>
        /// True when the <c>{slug}_settings</c> global exists and is non-blank
        /// (user has opened/saved the menu at least once).
        /// </summary>
        public static bool HasSavedSettings(IInlineInvokeProxy cph, string title)
        {
            if (cph == null)
                return false;
            try
            {
                var raw = cph.GetGlobalVar<string>(SettingsKeyHelper.SettingsKeyFor(title), true);
                return !string.IsNullOrWhiteSpace(raw);
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal($"HasSavedSettings failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Data-blob global key for a menu title (e.g. "First Chatters" → "first_chatters_data").</summary>
        public static string DataKeyFor(string title) => SettingsKeyHelper.KeyFor(title, "data");

        /// <summary>
        /// Load a typed runtime state object from the <c>{slug}_data</c> global.
        /// Missing/malformed fields keep property defaults from <c>new T()</c>.
        /// </summary>
        public static T LoadData<T>(IInlineInvokeProxy cph, string title, Action<T> validate = null)
            where T : new()
        {
            var result = new T();
            if (cph == null)
            {
                validate?.Invoke(result);
                return result;
            }

            string json = null;
            try
            {
                json = cph.GetGlobalVar<string>(DataKeyFor(title), true);
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal($"LoadData GetGlobalVar failed: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    JsonConvert.PopulateObject(json, result, CreateSnakeCaseSettings());
                }
                catch (Exception ex)
                {
                    FluentConfigApp.LogInternal($"LoadData PopulateObject failed: {ex.Message}");
                }
            }

            validate?.Invoke(result);
            return result;
        }

        /// <summary>Serialize <paramref name="data"/> to the <c>{slug}_data</c> persisted global.</summary>
        public static void SaveData<T>(IInlineInvokeProxy cph, string title, T data)
        {
            if (cph == null)
                return;
            try
            {
                var json = data == null
                    ? "{}"
                    : JsonConvert.SerializeObject(data, CreateSnakeCaseSettings());
                cph.SetGlobalVar(DataKeyFor(title), json, true);
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal($"SaveData failed: {ex.Message}");
            }
        }

        /// <summary>Read a nested path from the <c>{slug}_data</c> blob.</summary>
        public static TValue GetData<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonPath,
            TValue defaultValue = default)
        {
            if (cph == null || string.IsNullOrEmpty(jsonPath))
                return defaultValue;

            var mgr = OpenDataManager(cph, title);
            mgr.Load();
            return mgr.GetValue(jsonPath, defaultValue);
        }

        /// <summary>Write a nested path on the <c>{slug}_data</c> blob and persist.</summary>
        public static void SetData<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonPath,
            TValue value)
        {
            if (cph == null || string.IsNullOrEmpty(jsonPath))
                return;

            var mgr = OpenDataManager(cph, title);
            mgr.Load();
            mgr.SetValue(jsonPath, value);
            mgr.Save(mgr.GetSettings());
        }

        /// <summary>Create a structured logger for an extension action script.</summary>
        public static ExtensionLogger Logger(
            IInlineInvokeProxy cph,
            string title,
            string version,
            IEnumerable<string> extraSensitiveKeys = null)
            => new ExtensionLogger(cph, title, version, GetVersion(), extraSensitiveKeys);

        /// <summary>Capture common Streamer.bot event args for the current action.</summary>
        public static EventContext CaptureEvent(IInlineInvokeProxy cph)
            => EventContext.Capture(cph);

        /// <summary>Fill <c>%user%</c> / <c>{user}</c> placeholders from an event snapshot.</summary>
        public static string ApplyTemplate(string template, EventContext ev)
            => MessageTemplates.Apply(template, ev);

        /// <summary>Fill <c>%key%</c> / <c>{key}</c> placeholders from a dictionary.</summary>
        public static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> vars)
            => MessageTemplates.Apply(template, vars);

        private static SettingsManager OpenSettingsManager(IInlineInvokeProxy cph, string title)
            => new SettingsManager(cph, SettingsKeyHelper.SettingsKeyFor(title), FluentConfigApp.LogInternal);

        private static SettingsManager OpenDataManager(IInlineInvokeProxy cph, string title)
            => new SettingsManager(cph, DataKeyFor(title), FluentConfigApp.LogInternal);

        private static JsonSerializerSettings CreateSnakeCaseSettings()
            => new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Error = (_, args) => { args.ErrorContext.Handled = true; }
            };
    }

    /// <summary>
    /// Short alias for <see cref="FluentConfig"/> runtime helpers.
    /// Prefer <c>Fc.KeyFor</c> / <c>Fc.LoadSettings</c> in action scripts
    /// (avoids the awkward <c>FluentConfig.FluentConfig</c> qualification).
    /// </summary>
    public static class Fc
    {
        public static string SlugFor(string title) => FluentConfig.SlugFor(title);
        public static string KeyFor(string title, string suffix) => FluentConfig.KeyFor(title, suffix);
        public static string SettingsKeyFor(string title) => FluentConfig.SettingsKeyFor(title);

        public static T LoadSettings<T>(IInlineInvokeProxy cph, string title, Action<T> validate = null)
            where T : new()
            => FluentConfig.LoadSettings(cph, title, validate);

        public static TValue GetSetting<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonKey,
            TValue defaultValue = default)
            => FluentConfig.GetSetting(cph, title, jsonKey, defaultValue);

        public static void SetSetting<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonKey,
            TValue value)
            => FluentConfig.SetSetting(cph, title, jsonKey, value);

        public static void SaveSettings(IInlineInvokeProxy cph, string title, Action<JObject> mutate)
            => FluentConfig.SaveSettings(cph, title, mutate);

        public static bool HasSavedSettings(IInlineInvokeProxy cph, string title)
            => FluentConfig.HasSavedSettings(cph, title);

        public static string DataKeyFor(string title) => FluentConfig.DataKeyFor(title);

        public static T LoadData<T>(IInlineInvokeProxy cph, string title, Action<T> validate = null)
            where T : new()
            => FluentConfig.LoadData(cph, title, validate);

        public static void SaveData<T>(IInlineInvokeProxy cph, string title, T data)
            => FluentConfig.SaveData(cph, title, data);

        public static TValue GetData<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonPath,
            TValue defaultValue = default)
            => FluentConfig.GetData(cph, title, jsonPath, defaultValue);

        public static void SetData<TValue>(
            IInlineInvokeProxy cph,
            string title,
            string jsonPath,
            TValue value)
            => FluentConfig.SetData(cph, title, jsonPath, value);

        public static ExtensionLogger Logger(
            IInlineInvokeProxy cph,
            string title,
            string version,
            IEnumerable<string> extraSensitiveKeys = null)
            => FluentConfig.Logger(cph, title, version, extraSensitiveKeys);

        public static EventContext CaptureEvent(IInlineInvokeProxy cph)
            => FluentConfig.CaptureEvent(cph);

        public static string ApplyTemplate(string template, EventContext ev)
            => FluentConfig.ApplyTemplate(template, ev);

        public static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> vars)
            => FluentConfig.ApplyTemplate(template, vars);

        public static bool AlreadyOpened(string title = "FluentConfig", string version = "1.0")
            => FluentConfig.AlreadyOpened(title, version);

        public static bool IsOpen => FluentConfig.IsOpen;
        public static string GetVersion() => FluentConfig.GetVersion();

        /// <summary>
        /// Open a settings window for this title (focus if already open, otherwise create/build/show).
        /// Preferred entry for menu actions; see <see cref="FluentConfigUi.Create"/> for the granular builder.
        /// </summary>
        public static void Open(
            IInlineInvokeProxy cph,
            string title,
            string version,
            Action<FluentConfigUi> build,
            string iconPath = null)
            => FluentConfigUi.ShowOrFocus(cph, title, version, build, iconPath);

        /// <summary>Same as <see cref="Open"/>; kept for callers that already use this name.</summary>
        public static void ShowOrFocus(
            IInlineInvokeProxy cph,
            string title,
            string version,
            Action<FluentConfigUi> build,
            string iconPath = null)
            => Open(cph, title, version, build, iconPath);
    }

    /// <summary>
    /// Root fluent entry for building FluentConfig UIs. Create(...).Section(...).Show().
    /// </summary>
    /// <remarks>
    /// Recommended authoring pattern: a plain static <c>ExtensionInfo</c> class per extension
    /// with <c>Title</c> / <c>Version</c> / <c>IconPath</c> constants, then call
    /// <see cref="Fc.Open"/> (or the granular Create/Section/Show API).
    /// </remarks>
    public class FluentConfigUi
    {
        private readonly FluentConfigSession _session;

        /// <summary>Test hook for InternalsVisibleTo consumers.</summary>
        internal FluentConfigSession Session => _session;

        private FluentConfigUi(FluentConfigSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <param name="cph">Streamer.bot IInlineInvokeProxy (CPH).</param>
        /// <param name="title">Window title; also slugified into the settings global key (<c>{slug}_settings</c>).</param>
        /// <param name="version">Display version.</param>
        /// <param name="plainWindow">Ignored in the WebView2 host (kept for API compatibility).</param>
        public static FluentConfigUi Create(IInlineInvokeProxy cph, string title, string version, bool plainWindow = false)
        {
            WebView2AssemblyResolve.EnsureInitialized();
            var session = new FluentConfigSession(cph, title ?? "Settings", version);
            return new FluentConfigUi(session);
        }

        /// <summary>
        /// Focus an existing window with this title, or create/build/show a new one.
        /// Collapses the usual AlreadyOpened → Create → build → Show boilerplate.
        /// </summary>
        /// <remarks>
        /// Prefer a static <c>ExtensionInfo</c> class holding Title/Version/IconPath constants
        /// and pass those here — that is a recommended convention, not a framework type.
        /// </remarks>
        public static void ShowOrFocus(
            IInlineInvokeProxy cph,
            string title,
            string version,
            Action<FluentConfigUi> build,
            string iconPath = null)
        {
            if (!FluentConfigWindowManager.TryBeginOpen(title, version, FluentConfigApp.LogInternal))
                return;

            try
            {
                var ui = Create(cph, title, version);
                if (!string.IsNullOrEmpty(iconPath))
                    ui.Icon(iconPath);
                build?.Invoke(ui);
                ui.Show();
            }
            catch
            {
                FluentConfigWindowManager.Unregister(title);
                throw;
            }
        }

        /// <summary>
        /// Optional window icon (.ico path). Falls back to the embedded FluentConfig icon.
        /// </summary>
        public FluentConfigUi Icon(string iconPath)
        {
            _session.SetIconPath(iconPath);
            return this;
        }

        /// <summary>
        /// Override the footer repo / docs URL for this UI (defaults to the build-time
        /// <c>FluentConfigRepoUrl</c> property, else <c>https://example.test/fluentconfig</c>).
        /// </summary>
        public FluentConfigUi RepoUrl(string repoUrl)
        {
            _session.SetRepoUrl(repoUrl);
            return this;
        }

        public FluentConfigUi Section(string title, string tabId, Action<SectionBuilder> build)
        {
            if (build == null) return this;
            _session.RegisterDeferredSection(tabId ?? "", title ?? "", build);
            return this;
        }

        /// <summary>
        /// Schedule a notify-only extension update check after the window is shown
        /// (never blocks <see cref="Show"/>). Defaults to GitHub <c>releases/latest</c>;
        /// pass <paramref name="tagPrefix"/> for monorepo tags like <c>{prefix}-v{semver}</c>.
        /// Optional min Streamer.bot / FluentConfig gates fail <see cref="Show"/> with a popup.
        /// </summary>
        /// <param name="repo">GitHub <c>owner/name</c>.</param>
        /// <param name="currentVersion">Installed extension version (semver-ish).</param>
        /// <param name="tagPrefix">When set, use tagged releases (<c>{prefix}-v*</c>); otherwise latest.</param>
        /// <param name="minStreamerBot">Optional minimum <c>CPH.GetVersion()</c>.</param>
        /// <param name="minFluentConfig">Optional minimum FluentConfig framework version.</param>
        /// <param name="updateGuideUrl">Optional “How to update” URL (else release page).</param>
        public FluentConfigUi WithExtensionUpdateNotice(
            string repo,
            string currentVersion,
            string tagPrefix = null,
            string minStreamerBot = null,
            string minFluentConfig = null,
            string updateGuideUrl = null)
        {
            _session.ConfigureExtensionUpdateNotice(
                repo, currentVersion, tagPrefix, minStreamerBot, minFluentConfig, updateGuideUrl);
            return this;
        }

        public FluentConfigUi LogExistingSettings()
        {
            _session.LogExistingSettings();
            return this;
        }

        public void Show() => _session.Show();
    }
}
