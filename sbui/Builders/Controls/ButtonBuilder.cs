using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    public class ButtonBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly Sbui _ui;
        private readonly string _tabName;
        internal readonly string Label;
        internal string HintText { get; private set; }
        internal string ButtonText { get; private set; }
        internal string ColorHex { get; private set; }
        internal Action<UiContext> OnClickCallback { get; private set; }
        internal string ShowWhenKey { get; private set; }

        internal ButtonBuilder(IAddStrategy strategy, Sbui ui, string tabName, string label)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tabName = tabName ?? "";
            Label = label ?? "";
        }

        public ButtonBuilder Hint(string text) { HintText = text; return this; }
        public ButtonBuilder Text(string caption) { ButtonText = caption; return this; }
        public ButtonBuilder Color(string hex) { ColorHex = hex; return this; }
        public ButtonBuilder OnClick(Action<UiContext> callback) { OnClickCallback = callback; return this; }
        public ButtonBuilder ShowWhen(string key) { ShowWhenKey = key; return this; }

        void IControlOptions.Hint(string text) { HintText = text; }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { }
        void IControlOptions.Default(int value) { }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { ShowWhenKey = key; }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { ColorHex = hex; }
        void IControlOptions.Text(string caption) { ButtonText = caption; }
        void IControlOptions.OnClick(Action<UiContext> callback) { OnClickCallback = callback; }
        void IControlOptions.WithPermanentOption(bool value) { }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }

        public void Add()
        {
            Action callback = null;
            if (OnClickCallback != null)
            {
                var cb = OnClickCallback;
                var window = ((IRenderContext)_ui).Window;
                callback = () =>
                {
                    var ctx = new UiContext(_ui);
                    if (window != null && !window.Dispatcher.CheckAccess())
                        window.Dispatcher.Invoke(() => cb(ctx));
                    else
                        cb(ctx);
                };
            }
            var el = new ClickableButtonElement(Label, HintText ?? "", ButtonText ?? "OK", ColorHex ?? "", _tabName, callback);
            _strategy.Add(el, ShowWhenKey);
        }
    }
}
