using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    /// <summary>
    /// Non-generic base for host builders. Owns _core, _pending, FlushPending, and structural helpers.
    /// </summary>
    public abstract class ControlHostBuilder
    {
        protected readonly ControlBuilderCore _core;
        protected readonly IAddStrategy _strategy;
        protected IFlushableControlBuilder _pending;

        protected ControlHostBuilder(ControlBuilderCore core, IAddStrategy strategy)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>Flush any pending control (auto-add). Called automatically when starting the next control.</summary>
        internal void FlushPending()
        {
            _pending?.Add();
            _pending = null;
        }

        internal IFlushableControlBuilder GetPending() => _pending;

        /// <summary>Section/panel description text (intro).</summary>
        protected void AddIntro(string text, string tabName)
        {
            FlushPending();
            var el = new DescriptionElement(text ?? "", tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Section/panel title.</summary>
        protected void AddTitle(string text, string tabName)
        {
            FlushPending();
            var el = new TitleElement(text ?? "", tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Horizontal separator.</summary>
        protected void AddSeparator(string tabName)
        {
            FlushPending();
            var el = new InlineSeparatorElement(tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Common pattern: flush pending, create builder, set pending, return wrapper.</summary>
        protected T CreateControl<T>(IFlushableControlBuilder builder, Func<IFlushableControlBuilder, T> createWrapper)
        {
            FlushPending();
            _pending = builder;
            return createWrapper(builder);
        }
    }

    /// <summary>
    /// Generic host builder that provides the 13 shared control-creation methods (Toggle, Textbox, etc.).
    /// Concrete hosts override WrapControl to produce their specific wrapper type.
    /// </summary>
    public abstract class ControlHostBuilder<TWrapper> : ControlHostBuilder
    {
        protected ControlHostBuilder(ControlBuilderCore core, IAddStrategy strategy)
            : base(core, strategy) { }

        /// <summary>Create the fluent wrapper for a newly created control builder.</summary>
        protected abstract TWrapper WrapControl(IFlushableControlBuilder builder);

        public TWrapper Toggle(string label, string key) => CreateControl(_core.CreateToggle(label, key), WrapControl);
        public TWrapper Textbox(string label, string key) => CreateControl(_core.CreateTextbox(label, key), WrapControl);
        public TWrapper Slider(string label, string key) => CreateControl(_core.CreateSlider(label, key), WrapControl);
        public TWrapper Button(string label) => CreateControl(_core.CreateButton(label), WrapControl);
        public TWrapper Input(string label, string key) => CreateControl(_core.CreateInput(label, key), WrapControl);
        public TWrapper IntegerInput(string label, string key) => CreateControl(_core.CreateIntegerInput(label, key), WrapControl);
        public TWrapper DurationInput(string label, string key) => CreateControl(_core.CreateDurationInput(label, key), WrapControl);
        public TWrapper Filepath(string label, string key) => CreateControl(_core.CreateFilepath(label, key), WrapControl);
        public TWrapper NumberInput(string label, string key) => CreateControl(_core.CreateNumberInput(label, key), WrapControl);
        public TWrapper ColorPicker(string label, string key) => CreateControl(_core.CreateColorPicker(label, key), WrapControl);
        public TWrapper Dropdown(string label, string key) => CreateControl(_core.CreateDropdown(label, key), WrapControl);
        public TWrapper DynamicTextboxes(string label, string key) => CreateControl(_core.CreateDynamicTextboxes(label, key), WrapControl);
        public TWrapper PillInput(string label, string key) => CreateControl(_core.CreatePillInput(label, key), WrapControl);
    }
}
