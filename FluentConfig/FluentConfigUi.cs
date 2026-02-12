using System;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig
{
    /// <summary>
    /// Root fluent entry for building FluentConfig UIs. Create(...).Header(...).Section(...).Show().
    /// </summary>
    public class FluentConfigUi
    {
        private readonly FluentConfig _config;
        private bool _firstSectionBuilt;

        private FluentConfigUi(FluentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Creates the root builder and underlying FluentConfig instance. Call Header/Section then Show().
        /// </summary>
        /// <param name="cph">Streamer.bot IInlineInvokeProxy (CPH). Required for loading/saving settings.</param>
        /// <param name="title">Window title and settings key prefix (e.g. "My Extension").</param>
        /// <param name="version">Display version for window title and storage (e.g. "1.0").</param>
        /// <param name="plainWindow">True for faster startup without Mica/rounded corners; controls are identical.</param>
        public static FluentConfigUi Create(IInlineInvokeProxy cph, string title, string version, bool plainWindow = false)
        {
            var config = new FluentConfig(cph, title ?? "Settings", version, true, plainWindow);
            return new FluentConfigUi(config);
        }

        /// <summary>Adds a header image at the top.</summary>
        public FluentConfigUi Header(string imageUrl)
        {
            _config.AddHeader(imageUrl ?? "");
            return this;
        }

        /// <summary>Adds a section (tab) with the given title and builds its content with the fluent SectionBuilder.</summary>
        public FluentConfigUi Section(string title, string tabId, Action<SectionBuilder> build)
        {
            if (build == null) return this;

            // Store all sections as deferred. The first section will be built after the window is shown;
            // the rest build lazily on first tab switch.
            _config.RegisterDeferredSection(tabId ?? "", title ?? "", build);
            if (!_firstSectionBuilt)
            {
                _firstTabId = tabId ?? "";
                _firstSectionBuilt = true;
            }
            return this;
        }

        private string _firstTabId;

        /// <summary>Shows the UI window. Displays immediately with a loading overlay, builds the first section via Dispatcher, then removes the overlay.</summary>
        public void Show()
        {
            _config.ShowUIWithLoadingOverlay(_firstTabId);
        }

        /// <summary>Logs current settings to the log callback (debug).</summary>
        public FluentConfigUi LogExistingSettings()
        {
            _config.LogExistingSettings();
            return this;
        }

        /// <summary>
        /// Forces all deferred sections to build synchronously (useful for testing).
        /// Not needed in normal usage — Show() handles this automatically.
        /// </summary>
        internal FluentConfigUi BuildAllDeferred()
        {
            _config.BuildAllDeferredSections();
            return this;
        }
    }
}
