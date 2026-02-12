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
    }
}
