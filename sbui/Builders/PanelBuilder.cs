using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for adding controls to a specific panel (e.g. inside PillInput or WithVisibility callbacks).
    /// </summary>
    public class PanelBuilder : ControlHostBuilder
    {
        internal readonly Sbui Ui;
        internal readonly Panel Panel;
        internal readonly string TabName;

        internal PanelBuilder(Sbui ui, Panel panel, string tabName)
            : base(CreateCore(ui, panel, tabName), new AddToPanelStrategy(ui, panel, tabName ?? ""))
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Panel = panel ?? throw new ArgumentNullException(nameof(panel));
            TabName = tabName ?? "";
        }

        private static ControlBuilderCore CreateCore(Sbui ui, Panel panel, string tabName)
        {
            var strategy = new AddToPanelStrategy(ui, panel, tabName ?? "");
            return new ControlBuilderCore(strategy, tabName ?? "", ui);
        }

        public PanelBuilder Title(string text)
        {
            AddTitle(text, TabName);
            return this;
        }

        public PanelBuilder Intro(string text)
        {
            AddIntro(text, TabName);
            return this;
        }

        public PanelBuilder Separator()
        {
            AddSeparator(TabName);
            return this;
        }

        public PanelFluentWrapper Toggle(string label, string key) => CreateControl(_core.CreateToggle(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper Textbox(string label, string key) => CreateControl(_core.CreateTextbox(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper Slider(string label, string key) => CreateControl(_core.CreateSlider(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper Button(string label) => CreateControl(_core.CreateButton(label), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper IntegerInput(string label, string key) => CreateControl(_core.CreateIntegerInput(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper DurationInput(string label, string key) => CreateControl(_core.CreateDurationInput(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper Filepath(string label, string key) => CreateControl(_core.CreateFilepath(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper ResponseBox(string label, string key) => CreateControl(_core.CreateResponseBox(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper DecimalStepper(string label, string key) => CreateControl(_core.CreateDecimalStepper(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper ColorPicker(string label, string key) => CreateControl(_core.CreateColorPicker(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper SliderWithToggle(string label, string key) => CreateControl(_core.CreateSliderWithToggle(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper Dropdown(string label, string key) => CreateControl(_core.CreateDropdown(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper CompetingToggles(string label, string key) => CreateControl(_core.CreateCompetingToggles(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper DynamicTextboxes(string label, string key) => CreateControl(_core.CreateDynamicTextboxes(label, key), b => new PanelFluentWrapper(this, b));
        public PanelFluentWrapper PillInput(string label, string key) => CreateControl(_core.CreatePillInput(label, key), b => new PanelFluentWrapper(this, b));

        public PanelBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                Ui.WithVisibility(toggleKey, TabName, inverted, build);
            return this;
        }

        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            FlushPending();
            if (buildRow != null)
                Ui.WithRepeatableRows(saveKey, TabName, buildRow);
            return this;
        }
    }
}
