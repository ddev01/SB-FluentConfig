using System;

namespace FluentConfig
{
    public class SectionFluentWrapper : FluentWrapperBase<SectionFluentWrapper, SectionBuilder>
    {
        internal SectionFluentWrapper(SectionBuilder section, IControlOptions pending) : base(section, pending)
        {
        }

        private SectionFluentWrapper Next() => new SectionFluentWrapper(Host, Host.GetPending());
        public SectionFluentWrapper Intro(string text)
        {
            Host.FlushPending();
            Host.Intro(text);
            return Next();
        }

        public SectionFluentWrapper Title(string text)
        {
            Host.FlushPending();
            Host.Title(text);
            return Next();
        }

        public SectionFluentWrapper Separator()
        {
            Host.FlushPending();
            Host.Separator();
            return Next();
        }

        public SectionFluentWrapper ConnectionStatus(string label, string initialStatus, string buttonText = null, Action<UiContext> onClick = null)
        {
            Host.FlushPending();
            Host.ConnectionStatus(label, initialStatus, buttonText, onClick);
            return Next();
        }

        /// <summary>
        /// Visibility group. Prefer <c>inverted: true</c> (named) or <see cref = "WithVisibilityWhenOff"/>.
        /// </summary>
        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build, inverted);
            return Next();
        }

        public SectionFluentWrapper WithVisibility(string key, Comparator op, int value, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(key, op, value, build);
            return Next();
        }

        public SectionFluentWrapper WithVisibility(string key, Comparator op, string compareKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(key, op, compareKey, build);
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

        public SectionFluentWrapper Grid(string spec, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Grid(spec, build);
            return Next();
        }

        public SectionFluentWrapper Row(Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Row(build);
            return Next();
        }

        public SectionFluentWrapper Row(string spec, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Row(spec, build);
            return Next();
        }

        public SectionFluentWrapper RepeatFor(string driverKey, Action<PanelBuilder, int> build, int? max = null, int startIndex = 1)
        {
            Host.FlushPending();
            Host.RepeatFor(driverKey, build, max, startIndex);
            return Next();
        }
    }
}