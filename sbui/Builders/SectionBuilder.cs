using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Fluent builder for a section (tab). Intro, Toggle, Textbox, Slider, Button, Separator, etc.; WithVisibility.
    /// </summary>
    public class SectionBuilder
    {
        private readonly ControlBuilderCore _core;
        private readonly IAddStrategy _strategy;
        private readonly Sbui _ui;
        private readonly string _tabName;
        private IFlushableControlBuilder _pending;

        internal SectionBuilder(Sbui ui, string tabName)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
            _strategy = new AddToTabStrategy(ui, _tabName);
            _core = new ControlBuilderCore(_strategy, _tabName, ui);
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

        public SectionFluentWrapper Toggle(string label, string key) { FlushPending(); var b = _core.CreateToggle(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Textbox(string label, string key) { FlushPending(); var b = _core.CreateTextbox(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Slider(string label, string key) { FlushPending(); var b = _core.CreateSlider(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Button(string label) { FlushPending(); var b = _core.CreateButton(label); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper IntegerInput(string label, string key) { FlushPending(); var b = _core.CreateIntegerInput(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DurationInput(string label, string key) { FlushPending(); var b = _core.CreateDurationInput(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper Filepath(string label, string key) { FlushPending(); var b = _core.CreateFilepath(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper ResponseBox(string label, string key) { FlushPending(); var b = _core.CreateResponseBox(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DecimalStepper(string label, string key) { FlushPending(); var b = _core.CreateDecimalStepper(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper ColorPicker(string label, string key) { FlushPending(); var b = _core.CreateColorPicker(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper SliderWithToggle(string label, string key) { FlushPending(); var b = _core.CreateSliderWithToggle(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper RefreshableDropdown(string label, string key) { FlushPending(); var b = _core.CreateRefreshableDropdown(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper CompetingToggles(string label, string key) { FlushPending(); var b = _core.CreateCompetingToggles(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper DynamicTextboxes(string label, string key) { FlushPending(); var b = _core.CreateDynamicTextboxes(label, key); _pending = b; return new SectionFluentWrapper(this, b); }
        public SectionFluentWrapper PillInput(string label, string key) { FlushPending(); var b = _core.CreatePillInput(label, key); _pending = b; return new SectionFluentWrapper(this, b); }

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
