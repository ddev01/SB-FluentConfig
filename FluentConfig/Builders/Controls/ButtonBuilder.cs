using System;
using FluentConfig.Elements;

namespace FluentConfig
{
    public class ButtonBuilder : ControlBuilderBase
    {
        private readonly FluentConfig _ui;
        private string _buttonText;
        private string _colorHex;
        private Action<UiContext> _onClickCallback;

        internal ButtonBuilder(IAddStrategy strategy, FluentConfig ui, string tabName, string label)
            : base(strategy, tabName, label, "")
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public override void Text(string caption) { _buttonText = caption; }
        public override void Color(string hex) { _colorHex = hex; }
        public override void OnClick(Action<UiContext> callback) { _onClickCallback = callback; }

        public override void Add()
        {
            Action callback = null;
            if (_onClickCallback != null)
            {
                var cb = _onClickCallback;
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
            var el = new ClickableButtonElement(Label, HintText ?? "", _buttonText ?? "OK", _colorHex ?? "", TabName, callback);
            Strategy.Add(el, ShowWhenKey);
        }
    }
}
