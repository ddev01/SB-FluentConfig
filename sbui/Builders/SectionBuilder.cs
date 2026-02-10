using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for a section (tab). Intro, Toggle, Textbox, Slider, Button, Separator, etc.; WithVisibility.
    /// </summary>
    public class SectionBuilder
    {
        private readonly IAddStrategy _strategy;
        private readonly Sbui _ui;
        private readonly string _tabName;
        private IFlushableControlBuilder _pending;

        internal SectionBuilder(Sbui ui, string tabName)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
            _strategy = new AddToTabStrategy(ui, _tabName);
        }

        /// <summary>Flush any pending control (auto-add). Called automatically when starting the next control or when the section ends.</summary>
        internal void FlushPending()
        {
            _pending?.Add();
            _pending = null;
        }

        internal IFlushableControlBuilder GetPending() => _pending;

        /// <summary>Section-level description text (intro).</summary>
        public SectionBuilder Intro(string text)
        {
            FlushPending();
            var el = new DescriptionElement(text ?? "", _tabName);
            _strategy.Add(el, null);
            return this;
        }

        /// <summary>Section title (optional; Section() already adds one). Use for sub-titles.</summary>
        public SectionBuilder Title(string text)
        {
            FlushPending();
            var el = new TitleElement(text ?? "", _tabName);
            _strategy.Add(el, null);
            return this;
        }

        /// <summary>Horizontal separator.</summary>
        public SectionBuilder Separator()
        {
            FlushPending();
            var el = new InlineSeparatorElement(_tabName);
            _strategy.Add(el, null);
            return this;
        }

        public SectionFluentWrapper Toggle(string label, string key) { FlushPending(); var b = new ToggleBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Textbox(string label, string key) { FlushPending(); var b = new TextboxBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Slider(string label, string key) { FlushPending(); var b = new SliderBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Button(string label) { FlushPending(); var b = new ButtonBuilder(_strategy, _ui, _tabName, label); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper IntegerInput(string label, string key) { FlushPending(); var b = new IntegerInputBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DurationInput(string label, string key) { FlushPending(); var b = new DurationInputBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Filepath(string label, string key) { FlushPending(); var b = new FilepathBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper ResponseBox(string label, string key) { FlushPending(); var b = new ResponseBoxBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DecimalStepper(string label, string key) { FlushPending(); var b = new DecimalStepperBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper ColorPicker(string label, string key) { FlushPending(); var b = new ColorPickerBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper SliderWithToggle(string label, string key) { FlushPending(); var b = new SliderWithToggleBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper RefreshableDropdown(string label, string key) { FlushPending(); var b = new RefreshableDropdownBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper CompetingToggles(string label, string key) { FlushPending(); var b = new CompetingTogglesBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DynamicTextboxes(string label, string key) { FlushPending(); var b = new DynamicTextboxesBuilder(_strategy, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper PillInput(string label, string key) { FlushPending(); var b = new PillInputBuilder(_strategy, _ui, _tabName, label, key); _pending = b; return new SectionFluentWrapper(this, b); }

        /// <summary>Content is visible when the toggle is on (or off when inverted). Build receives a PanelBuilder for the inner content.</summary>
        public SectionBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                _ui.WithVisibility(toggleKey, _tabName, inverted, build);
            return this;
        }

        /// <summary>Content is visible when the toggle is on. Build receives a PanelBuilder for the inner content.</summary>
        public SectionBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build)
        {
            return WithVisibility(toggleKey, false, build);
        }
    }
}
