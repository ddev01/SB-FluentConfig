using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>
    /// Base fluent wrapper: option methods + control creation methods.
    /// </summary>
    public abstract class FluentWrapperBase<TWrapper, THost>
        where TWrapper : FluentWrapperBase<TWrapper, THost>
        where THost : ControlHostBuilder<TWrapper>
    {
        protected readonly THost Host;
        protected readonly IControlOptions PendingOptions;

        protected FluentWrapperBase(THost host, IControlOptions pendingOptions)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            PendingOptions = pendingOptions;
        }

        protected TWrapper Option(Action<IControlOptions> apply)
        {
            if (PendingOptions == null)
                throw new InvalidOperationException(
                    "No pending control to apply options to. Call a control method (e.g. .Toggle) before option methods, or avoid chaining options after structural calls like Intro/Separator.");
            apply?.Invoke(PendingOptions);
            return (TWrapper)this;
        }

        protected TWrapper Control(Func<THost, TWrapper> invoke) => invoke(Host);

        public TWrapper Hint(string text) => Option(o => o.Hint(text));
        public TWrapper Default(bool value) => Option(o => o.Default(value));
        public TWrapper Default(string value) => Option(o => o.Default(value));
        public TWrapper Default(int value) => Option(o => o.Default(value));
        public TWrapper Default(double value) => Option(o => o.Default(value));
        public TWrapper Range(int min, int max) => Option(o => o.Range(min, max));
        public TWrapper Range(double min, double max) => Option(o => o.Range(min, max));
        public TWrapper Step(double value) => Option(o => o.Step(value));
        public TWrapper Password() => Option(o => o.Password());
        public TWrapper Multiline() => Option(o => o.Multiline());
        public TWrapper ShowWhen(string key) => Option(o => o.ShowWhen(key));
        public TWrapper ShowWhen(string key, Comparator op, int value) => Option(o => o.ShowWhen(key, op, value));
        public TWrapper ShowWhen(string key, Comparator op, string compareKey) => Option(o => o.ShowWhen(key, op, compareKey));
        public TWrapper Size(string spec) => Option(o => o.Size(spec));
        public TWrapper Options(string[] options) => Option(o => o.Options(options));
        public TWrapper Options(IEnumerable<(string Value, string Display)> pairOptions) => Option(o => o.OptionsPairs(pairOptions));
        public TWrapper WithPairValue(string valueKey) => Option(o => o.WithPairValue(valueKey));
        public TWrapper DefaultByValue(string value) => Option(o => o.DefaultByValue(value));
        public TWrapper DefaultIndex(int index) => Option(o => o.DefaultIndex(index));
        public TWrapper WithExclusive(string[] options) => Option(o => o.WithExclusive(options));
        public TWrapper MaxSelected(int n) => Option(o => o.MaxSelected(n));
        public TWrapper DefaultIndices(int[] indices) => Option(o => o.DefaultIndices(indices));
        public TWrapper Refresh(Func<string[]> callback) => Option(o => o.Refresh(callback));
        public TWrapper Refresh(Func<IEnumerable<(string Value, string Display)>> callback) => Option(o => o.RefreshPairs(callback));
        public TWrapper Preset(string[] values) => Option(o => o.Preset(values));
        public TWrapper AllowDuplicates(bool value = true) => Option(o => o.AllowDuplicates(value));
        public TWrapper Color(string hex) => Option(o => o.Color(hex));
        public TWrapper Text(string caption) => Option(o => o.Text(caption));
        public TWrapper OnClick(Action<UiContext> callback) => Option(o => o.OnClick(callback));
        public TWrapper WithPermanentOption(bool value) => Option(o => o.WithPermanentOption(value));
        public TWrapper WithStepper(bool value = true) => Option(o => o.WithStepper(value));
        /// <summary>Nested pill item schema via fluent sub-builder.</summary>
        public TWrapper ItemTemplate(Action<PanelBuilder> build) => Option(o => o.ItemTemplate(build));
        /// <summary>Alias for <see cref="ItemTemplate"/>.</summary>
        public TWrapper WithItemTemplate(Action<PanelBuilder> build) => ItemTemplate(build);
        public TWrapper OnPillAdded(Action<string, CallbackContext> callback) => Option(o => o.OnPillAdded(callback));
        public TWrapper OnPillRemoved(Action<string, CallbackContext> callback) => Option(o => o.OnPillRemoved(callback));

        public TWrapper Toggle(string label, string key) => Control(h => h.Toggle(label, key));
        public TWrapper Textbox(string label, string key) => Control(h => h.Textbox(label, key));
        public TWrapper Slider(string label, string key) => Control(h => h.Slider(label, key));
        public TWrapper Button(string label) => Control(h => h.Button(label));
        public TWrapper IntegerInput(string label, string key) => Control(h => h.IntegerInput(label, key));
        public TWrapper DurationInput(string label, string key) => Control(h => h.DurationInput(label, key));
        public TWrapper Filepath(string label, string key) => Control(h => h.Filepath(label, key));
        public TWrapper NumberInput(string label, string key) => Control(h => h.NumberInput(label, key));
        public TWrapper ColorPicker(string label, string key) => Control(h => h.ColorPicker(label, key));
        public TWrapper Dropdown(string label, string key) => Control(h => h.Dropdown(label, key));
        public TWrapper DynamicTextboxes(string label, string key) => Control(h => h.DynamicTextboxes(label, key));
        public TWrapper PillInput(string label, string key) => Control(h => h.PillInput(label, key));
    }
}
