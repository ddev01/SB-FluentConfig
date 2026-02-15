using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for a section (tab). Intro, Toggle, Textbox, Slider, Button, Separator, etc.; WithVisibility.
    /// </summary>
    public class SectionBuilder : ControlHostBuilder<SectionFluentWrapper>
    {
        private readonly FluentConfig _ui;
        private readonly string _tabName;

        internal SectionBuilder(FluentConfig ui, string tabName)
            : base(CreateCore(ui, tabName), new AddToTabStrategy(ui, tabName ?? ""))
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
        }

        private static ControlBuilderCore CreateCore(FluentConfig ui, string tabName)
        {
            var strategy = new AddToTabStrategy(ui, tabName ?? "");
            return new ControlBuilderCore(strategy, tabName ?? "", ui);
        }

        protected override SectionFluentWrapper WrapControl(IFlushableControlBuilder b) => new SectionFluentWrapper(this, b);

        /// <summary>Adds intro/description text at the top of the section.</summary>
        public SectionBuilder Intro(string text)
        {
            AddIntro(text, _tabName);
            return this;
        }

        /// <summary>Adds a section title block.</summary>
        public SectionBuilder Title(string text)
        {
            AddTitle(text, _tabName);
            return this;
        }

        /// <summary>Adds a horizontal separator line.</summary>
        public SectionBuilder Separator()
        {
            AddSeparator(_tabName);
            return this;
        }

        /// <summary>Adds a block visible when the toggle is on (or off when inverted).</summary>
        public SectionBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                _ui.WithVisibility(toggleKey, _tabName, inverted, build);
            return this;
        }

        /// <summary>Adds a block visible when the toggle with the given key is on.</summary>
        public SectionBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build)
        {
            return WithVisibility(toggleKey, false, build);
        }

        /// <summary>Adds a block visible when the predicate returns true. Re-evaluates when any dependency control changes.</summary>
        public SectionBuilder WithVisibility(string[] dependencyKeys, Func<Elements.IRenderContext, bool> predicate, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null && dependencyKeys != null && dependencyKeys.Length > 0 && predicate != null)
                _ui.WithVisibility(dependencyKeys, predicate, _tabName, false, build);
            return this;
        }

        /// <summary>Adds a block visible when the predicate returns true (or false when inverted).</summary>
        public SectionBuilder WithVisibility(string[] dependencyKeys, Func<Elements.IRenderContext, bool> predicate, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null && dependencyKeys != null && dependencyKeys.Length > 0 && predicate != null)
                _ui.WithVisibility(dependencyKeys, predicate, _tabName, inverted, build);
            return this;
        }

        /// <summary>Adds a grid layout with the given number of columns.</summary>
        public SectionBuilder Grid(int cols, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0)
        {
            FlushPending();
            if (build != null)
            {
                var panel = _ui.GetTargetPanel(_tabName);
                if (panel != null)
                {
                    var pb = new PanelBuilder(_ui, panel, _tabName);
                    pb.Grid(cols, build, gap, justify, align, padding);
                }
            }
            return this;
        }

        /// <summary>Adds a grid layout with custom column widths (e.g. "auto", "1*", "2*", "100").</summary>
        public SectionBuilder Grid(string[] colWidths, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0)
        {
            FlushPending();
            if (build != null)
            {
                var panel = _ui.GetTargetPanel(_tabName);
                if (panel != null)
                {
                    var pb = new PanelBuilder(_ui, panel, _tabName);
                    pb.Grid(colWidths, build, gap, justify, align, padding);
                }
            }
            return this;
        }

        /// <summary>Adds a horizontal flex layout with optional gap between items.</summary>
        public SectionBuilder Flex(Action<PanelBuilder> build, int gap = 16, bool wrap = false, string justify = null, string align = null, double padding = 0)
        {
            FlushPending();
            if (build != null)
            {
                var panel = _ui.GetTargetPanel(_tabName);
                if (panel != null)
                {
                    var pb = new PanelBuilder(_ui, panel, _tabName);
                    pb.Flex(build, gap, wrap, justify, align, padding);
                }
            }
            return this;
        }

        /// <summary>Groups controls into a single container (one grid cell or flex item when inside Grid/Flex).</summary>
        public SectionBuilder Div(Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
            {
                var panel = _ui.GetTargetPanel(_tabName);
                if (panel != null)
                {
                    var pb = new PanelBuilder(_ui, panel, _tabName);
                    pb.Div(build);
                }
            }
            return this;
        }
    }
}
