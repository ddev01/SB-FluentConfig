using System;
using Sbui.Elements;

namespace Sbui
{
    public class ButtonBuilder : IFlushableControlBuilder
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
