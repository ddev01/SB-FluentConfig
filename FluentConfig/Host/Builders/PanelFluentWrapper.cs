using System;

namespace FluentConfig
{
    public class PanelFluentWrapper : FluentWrapperBase<PanelFluentWrapper, PanelBuilder>
    {
        internal PanelFluentWrapper(PanelBuilder panel, IControlOptions pending)
            : base(panel, pending) { }

        private PanelFluentWrapper Next() => new PanelFluentWrapper(Host, Host.GetPending());

        public PanelFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public PanelFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public PanelFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }

        public PanelFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build);
            return Next();
        }

        public PanelFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build, inverted);
            return Next();
        }

        public PanelFluentWrapper WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibilityWhenOff(toggleKey, build);
            return Next();
        }

        public PanelFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            Host.FlushPending();
            Host.WithRepeatableRows(saveKey, buildRow);
            return Next();
        }
    }
}
