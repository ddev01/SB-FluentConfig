using System;
using System.Windows.Controls;
using FluentConfig.Elements;

namespace FluentConfig
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
        public SectionFluentWrapper WithVisibility(string[] dependencyKeys, Func<IRenderContext, bool> predicate, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(dependencyKeys, predicate, build); return Next(); }
        public SectionFluentWrapper WithVisibility(string[] dependencyKeys, Func<IRenderContext, bool> predicate, bool inverted, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(dependencyKeys, predicate, inverted, build); return Next(); }
        public SectionFluentWrapper Grid(int cols, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0) { Host.FlushPending(); Host.Grid(cols, build, gap, justify, align, padding); return Next(); }
        public SectionFluentWrapper Grid(string[] colWidths, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0) { Host.FlushPending(); Host.Grid(colWidths, build, gap, justify, align, padding); return Next(); }
        public SectionFluentWrapper Flex(Action<PanelBuilder> build, int gap = 16, bool wrap = false, string justify = null, string align = null, double padding = 0) { Host.FlushPending(); Host.Flex(build, gap, wrap, justify, align, padding); return Next(); }
        public SectionFluentWrapper Div(Action<PanelBuilder> build) { Host.FlushPending(); Host.Div(build); return Next(); }
    }
}
