using System;
using FluentConfig.Core;
using FluentConfig.Protocol;
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

        /// <summary>
        /// CPH global for FluentConfig menu/DLL prefs (window geometry, DllCheck throttle, etc.).
        /// Mirrored in DllCheck examples (which cannot reference this assembly).
        /// </summary>
        public const string GeneralSettingsGlobal = "FluentConfig_General_Settings";

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
    }

    /// <summary>
    /// Root fluent entry for building FluentConfig UIs. Create(...).Section(...).Show().
    /// </summary>
    /// <remarks>
    /// Recommended authoring pattern: a plain static <c>ExtensionInfo</c> class per extension
    /// with <c>Title</c> / <c>Version</c> / <c>IconPath</c> constants, then call
    /// <see cref="ShowOrFocus"/> (or the granular Create/Section/Show API).
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
        /// <param name="title">Window title and settings key prefix.</param>
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
