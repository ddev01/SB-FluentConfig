using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Base for PanelFluentWrapper and SectionFluentWrapper. Forwards option methods to IControlOptions;
    /// control methods (Toggle, Textbox, etc.) delegate to the host builder via Control().
    /// </summary>
    public abstract class FluentWrapperBase<TWrapper, THost> where TWrapper : FluentWrapperBase<TWrapper, THost>
    {
        protected readonly THost Host;
        protected readonly IControlOptions PendingOptions;

        protected FluentWrapperBase(THost host, IControlOptions pendingOptions)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            Host = host;
            PendingOptions = pendingOptions;
        }

        protected TWrapper Option(Action<IControlOptions> apply)
        {
            if (PendingOptions != null && apply != null) apply(PendingOptions);
            return (TWrapper)this;
        }

        /// <summary>Delegate a control call to the host builder.</summary>
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
        public TWrapper ToggleDefault(bool value) => Option(o => o.ToggleDefault(value));
        public TWrapper Color(string hex) => Option(o => o.Color(hex));
        public TWrapper Text(string caption) => Option(o => o.Text(caption));
        public TWrapper OnClick(Action<UiContext> callback) => Option(o => o.OnClick(callback));
        public TWrapper WithPermanentOption(bool value) => Option(o => o.WithPermanentOption(value));
        public TWrapper WithStepper(bool value = true) => Option(o => o.WithStepper(value));
        public TWrapper WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) => Option(o => o.WithSectionsPanel(build));
        public TWrapper OnPillAdded(Action<string, StackPanel, CallbackContext> build) => Option(o => o.OnPillAdded(build));
        public TWrapper OnPillRemoved(Action<string, StackPanel, CallbackContext> build) => Option(o => o.OnPillRemoved(build));
        public TWrapper Type(string value) => Option(o => o.Type(value));
    }
}
