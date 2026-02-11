using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for a section (tab). Intro, Toggle, Textbox, Slider, Button, Separator, etc.; WithVisibility.
    /// </summary>
    public class SectionBuilder : ControlHostBuilder<SectionFluentWrapper>
    {
        private readonly Sbui _ui;
        private readonly string _tabName;

        internal SectionBuilder(Sbui ui, string tabName)
            : base(CreateCore(ui, tabName), new AddToTabStrategy(ui, tabName ?? ""))
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
        }

        private static ControlBuilderCore CreateCore(Sbui ui, string tabName)
        {
            var strategy = new AddToTabStrategy(ui, tabName ?? "");
            return new ControlBuilderCore(strategy, tabName ?? "", ui);
        }

        protected override SectionFluentWrapper WrapControl(IFlushableControlBuilder b) => new SectionFluentWrapper(this, b);

        public SectionBuilder Intro(string text)
        {
            AddIntro(text, _tabName);
            return this;
        }

        public SectionBuilder Title(string text)
        {
            AddTitle(text, _tabName);
            return this;
        }

        public SectionBuilder Separator()
        {
            AddSeparator(_tabName);
            return this;
        }

        public SectionBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                _ui.WithVisibility(toggleKey, _tabName, inverted, build);
            return this;
        }

        public SectionBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build)
        {
            return WithVisibility(toggleKey, false, build);
        }
    }
}
