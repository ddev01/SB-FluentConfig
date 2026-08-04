using System;

namespace FluentConfig
{
    public class SectionFluentWrapper : FluentWrapperBase<SectionFluentWrapper, SectionBuilder>
    {
        internal SectionFluentWrapper(SectionBuilder section, IControlOptions pending)
            : base(section, pending) { }

        private SectionFluentWrapper Next() => new SectionFluentWrapper(Host, Host.GetPending());

        public SectionFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public SectionFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public SectionFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }

        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build);
            return Next();
        }

        /// <summary>
        /// Prefer named arg: <c>WithVisibility("key", build, inverted: true)</c>,
        /// or <see cref="WithVisibilityWhenOff"/>.
        /// </summary>
        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build, inverted);
            return Next();
        }

        public SectionFluentWrapper WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibilityWhenOff(toggleKey, build);
            return Next();
        }

        public SectionFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            Host.FlushPending();
            Host.WithRepeatableRows(saveKey, buildRow);
            return Next();
        }
    }
}
