using System;
using Streamer.bot.Plugin.Interface;

namespace Sbui
{
    /// <summary>
    /// Root fluent entry for building Sbui UIs. Create(...).Header(...).Section(...).Show().
    /// </summary>
    public class SbuiUi
    {
        private readonly Sbui _sbui;
        private bool _firstSectionBuilt;

        private SbuiUi(Sbui sbui)
        {
            _sbui = sbui ?? throw new ArgumentNullException(nameof(sbui));
        }

        /// <summary>
        /// Creates the root builder and underlying Sbui instance. Call Header/Section then Show().
        /// Set plainWindow to true for faster startup at the cost of Mica backdrop, rounded corners,
        /// and accent title bar (FluentWindow chrome). All controls inside are identical.
        /// </summary>
        public static SbuiUi Create(IInlineInvokeProxy cph, string title, string version, bool plainWindow = false)
        {
            var sbui = new Sbui(cph, title ?? "Settings", version, true, plainWindow);
            return new SbuiUi(sbui);
        }

        /// <summary>Adds a header image at the top.</summary>
        public SbuiUi Header(string imageUrl)
        {
            _sbui.AddHeader(imageUrl ?? "");
            return this;
        }

        /// <summary>Adds a section (tab) with the given title and builds its content with the fluent SectionBuilder.</summary>
        public SbuiUi Section(string title, string tabId, Action<SectionBuilder> build)
        {
            if (build == null) return this;

            // Store all sections as deferred. The first section will be built after the window is shown;
            // the rest build lazily on first tab switch.
            _sbui.RegisterDeferredSection(tabId ?? "", title ?? "", build);
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
            _sbui.ShowUIWithLoadingOverlay(_firstTabId);
        }

        /// <summary>Logs current settings to the log callback (debug).</summary>
        public SbuiUi LogExistingSettings()
        {
            _sbui.LogExistingSettings();
            return this;
        }

        /// <summary>
        /// Forces all deferred sections to build synchronously (useful for testing).
        /// Not needed in normal usage — Show() handles this automatically.
        /// </summary>
        internal SbuiUi BuildAllDeferred()
        {
            _sbui.BuildAllDeferredSections();
            return this;
        }
    }
}
