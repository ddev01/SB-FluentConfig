using System;
using System.Windows.Controls;

namespace FluentConfig
{
    /// <summary>
    /// Fluent wrapper for panel so control chains return a type that has both option methods (Hint, Default, ...)
    /// and panel methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class PanelFluentWrapper : FluentWrapperBase<PanelFluentWrapper, PanelBuilder>
    {
        internal PanelFluentWrapper(PanelBuilder panel, IFlushableControlBuilder pending)
            : base(panel, pending as IControlOptions) { }

        private PanelFluentWrapper Next() => new PanelFluentWrapper(Host, Host.GetPending());

        public PanelFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public PanelFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public PanelFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }
        public PanelFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, inverted, build); return Next(); }
        public PanelFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow) { Host.FlushPending(); Host.WithRepeatableRows(saveKey, buildRow); return Next(); }
    }
}
