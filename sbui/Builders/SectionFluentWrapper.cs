using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper so control chains return a type that has both option methods (Hint, Default, ...)
    /// and section methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class SectionFluentWrapper : FluentWrapperBase<SectionFluentWrapper, SectionBuilder>
    {
        internal SectionFluentWrapper(SectionBuilder section, IFlushableControlBuilder pending)
            : base(section, pending as IControlOptions) { }

        private SectionFluentWrapper Next() => new SectionFluentWrapper(Host, Host.GetPending());

        public SectionFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public SectionFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public SectionFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }
        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, build); return Next(); }
        public SectionFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, inverted, build); return Next(); }
    }
}
