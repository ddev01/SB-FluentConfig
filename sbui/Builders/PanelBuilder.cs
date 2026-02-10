using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for adding controls to a specific panel (e.g. inside PillInput or WithVisibility callbacks).
    /// </summary>
    public class PanelBuilder
    {
        private readonly IAddStrategy _strategy;
        private IFlushableControlBuilder _pending;
        internal readonly Sbui Ui;
        internal readonly Panel Panel;
        internal readonly string TabName;

        internal PanelBuilder(Sbui ui, Panel panel, string tabName)
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Panel = panel ?? throw new ArgumentNullException(nameof(panel));
            TabName = tabName ?? "";
            _strategy = new AddToPanelStrategy(ui, panel, TabName);
        }

        /// <summary>Flush any pending control (auto-add). Called automatically when starting the next control or when the panel block ends.</summary>
        internal void FlushPending()
        {
            _pending?.Add();
            _pending = null;
        }

        internal IFlushableControlBuilder GetPending() => _pending;

        public PanelBuilder Title(string text)
        {
            FlushPending();
            var el = new TitleElement(text ?? "", TabName);
            _strategy.Add(el, null);
            return this;
        }

        public PanelBuilder Intro(string text)
        {
            FlushPending();
            var el = new DescriptionElement(text ?? "", TabName);
            _strategy.Add(el, null);
            return this;
        }

        public PanelBuilder Separator()
        {
            FlushPending();
            var el = new InlineSeparatorElement(TabName);
            _strategy.Add(el, null);
            return this;
        }

        public PanelFluentWrapper Toggle(string label, string key) { FlushPending(); var b = new ToggleBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper Textbox(string label, string key) { FlushPending(); var b = new TextboxBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper Slider(string label, string key) { FlushPending(); var b = new SliderBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper Button(string label) { FlushPending(); var b = new ButtonBuilder(_strategy, Ui, TabName, label); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper IntegerInput(string label, string key) { FlushPending(); var b = new IntegerInputBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper DurationInput(string label, string key) { FlushPending(); var b = new DurationInputBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper Filepath(string label, string key) { FlushPending(); var b = new FilepathBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper ResponseBox(string label, string key) { FlushPending(); var b = new ResponseBoxBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper DecimalStepper(string label, string key) { FlushPending(); var b = new DecimalStepperBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper ColorPicker(string label, string key) { FlushPending(); var b = new ColorPickerBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper SliderWithToggle(string label, string key) { FlushPending(); var b = new SliderWithToggleBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper RefreshableDropdown(string label, string key) { FlushPending(); var b = new RefreshableDropdownBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper CompetingToggles(string label, string key) { FlushPending(); var b = new CompetingTogglesBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }
        public PanelFluentWrapper DynamicTextboxes(string label, string key) { FlushPending(); var b = new DynamicTextboxesBuilder(_strategy, TabName, label, key); _pending = b; return new PanelFluentWrapper(this, b); }

        /// <summary>Content is visible when the toggle is on (or off when inverted).</summary>
        public PanelBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                Ui.WithVisibility(toggleKey, TabName, inverted, build);
            return this;
        }

        /// <summary>Repeatable rows; buildRow receives a PanelBuilder for each row.</summary>
        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            FlushPending();
            if (buildRow != null)
                Ui.WithRepeatableRows(saveKey, TabName, buildRow);
            return this;
        }
    }
}
