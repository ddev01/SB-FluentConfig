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

        private SbuiUi(Sbui sbui)
        {
            _sbui = sbui ?? throw new ArgumentNullException(nameof(sbui));
        }

        /// <summary>Creates the root builder and underlying Sbui instance. Call Header/Section then Show().</summary>
        public static SbuiUi Create(IInlineInvokeProxy cph, string title, string version)
        {
            var sbui = new Sbui(cph, title ?? "Settings", version, true);
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
            var titleEl = new Elements.TitleElement(title ?? "", tabId ?? "");
            _sbui.AddElement(tabId ?? "", titleEl, null);
            var section = new SectionBuilder(_sbui, tabId ?? "");
            build(section);
            section.FlushPending();
            return this;
        }

        /// <summary>Shows the UI window.</summary>
        public void Show()
        {
            _sbui.ShowUI();
        }

        /// <summary>Logs current settings to the log callback (debug).</summary>
        public SbuiUi LogExistingSettings()
        {
            _sbui.LogExistingSettings();
            return this;
        }
    }
}
