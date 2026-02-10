using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Base for PanelFluentWrapper and SectionFluentWrapper. Forwards option methods to IControlOptions (no dynamic).
    /// </summary>
    public abstract class FluentWrapperBase<TWrapper> where TWrapper : FluentWrapperBase<TWrapper>
    {
        protected readonly IControlOptions PendingOptions;

        protected FluentWrapperBase(IControlOptions pendingOptions)
        {
            PendingOptions = pendingOptions;
        }

        protected TWrapper Option(Action<IControlOptions> apply)
        {
            if (PendingOptions != null) apply(PendingOptions);
            return (TWrapper)this;
        }

        public TWrapper Hint(string text) => Option(o => o.Hint(text));
        public TWrapper Default(bool value) => Option(o => o.Default(value));
        public TWrapper Default(string value) => Option(o => o.Default(value));
        public TWrapper Default(int value) => Option(o => o.Default(value));
        public TWrapper Default(double value) => Option(o => o.Default(value));
        public TWrapper Range(int min, int max) => Option(o => o.Range(min, max));
        public TWrapper Range(double min, double max) => Option(o => o.Range(min, max));
        public TWrapper Step(double value) => Option(o => o.Step(value));
        public TWrapper Password() => Option(o => o.Password());
        public TWrapper ShowWhen(string key) => Option(o => o.ShowWhen(key));
        public TWrapper Options(string[] options) => Option(o => o.Options(options));
        public TWrapper DefaultIndex(int index) => Option(o => o.DefaultIndex(index));
        public TWrapper Refresh(Func<string[]> callback) => Option(o => o.Refresh(callback));
        public TWrapper Preset(string[] values) => Option(o => o.Preset(values));
        public TWrapper ToggleDefault(bool value) => Option(o => o.ToggleDefault(value));
        public TWrapper Color(string hex) => Option(o => o.Color(hex));
        public TWrapper Text(string caption) => Option(o => o.Text(caption));
        public TWrapper OnClick(Action<UiContext> callback) => Option(o => o.OnClick(callback));
        public TWrapper WithPermanentOption(bool value) => Option(o => o.WithPermanentOption(value));
        public TWrapper WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) => Option(o => o.WithSectionsPanel(build));
        public TWrapper OnPillAdded(Action<string, StackPanel, CallbackContext> build) => Option(o => o.OnPillAdded(build));
        public TWrapper OnPillRemoved(Action<string, StackPanel, CallbackContext> build) => Option(o => o.OnPillRemoved(build));
    }
}
