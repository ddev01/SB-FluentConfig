using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for a section (tab). Intro, Toggle, Textbox, Slider, Button, Separator, etc.; WithVisibility.
    /// </summary>
    public class SectionBuilder : ControlHostBuilder
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

        public SectionFluentWrapper Toggle(string label, string key) => CreateControl(_core.CreateToggle(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper Textbox(string label, string key) => CreateControl(_core.CreateTextbox(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper Slider(string label, string key) => CreateControl(_core.CreateSlider(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper Button(string label) => CreateControl(_core.CreateButton(label), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper IntegerInput(string label, string key) => CreateControl(_core.CreateIntegerInput(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper DurationInput(string label, string key) => CreateControl(_core.CreateDurationInput(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper Filepath(string label, string key) => CreateControl(_core.CreateFilepath(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper ResponseBox(string label, string key) => CreateControl(_core.CreateResponseBox(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper DecimalStepper(string label, string key) => CreateControl(_core.CreateDecimalStepper(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper ColorPicker(string label, string key) => CreateControl(_core.CreateColorPicker(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper SliderWithToggle(string label, string key) => CreateControl(_core.CreateSliderWithToggle(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper Dropdown(string label, string key) => CreateControl(_core.CreateDropdown(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper CompetingToggles(string label, string key) => CreateControl(_core.CreateCompetingToggles(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper DynamicTextboxes(string label, string key) => CreateControl(_core.CreateDynamicTextboxes(label, key), b => new SectionFluentWrapper(this, b));
        public SectionFluentWrapper PillInput(string label, string key) => CreateControl(_core.CreatePillInput(label, key), b => new SectionFluentWrapper(this, b));

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
