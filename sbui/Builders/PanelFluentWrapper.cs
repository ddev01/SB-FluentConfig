using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper for panel so control chains return a type that has both option methods (Hint, Default, ...)
    /// and panel methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class PanelFluentWrapper
    {
        private readonly PanelBuilder _panel;
        private readonly IFlushableControlBuilder _pending;

        internal PanelFluentWrapper(PanelBuilder panel, IFlushableControlBuilder pending)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _pending = pending;
        }

        private PanelFluentWrapper Next() => new PanelFluentWrapper(_panel, _panel.GetPending());

        // ---- Panel structure/controls: flush and delegate to panel ----
        public PanelFluentWrapper Title(string text) { _panel.FlushPending(); _panel.Title(text); return Next(); }
        public PanelFluentWrapper Intro(string text) { _panel.FlushPending(); _panel.Intro(text); return Next(); }
        public PanelFluentWrapper Separator() { _panel.FlushPending(); _panel.Separator(); return Next(); }
        public PanelFluentWrapper Toggle(string label, string key) { _panel.FlushPending(); _panel.Toggle(label, key); return Next(); }
        public PanelFluentWrapper Textbox(string label, string key) { _panel.FlushPending(); _panel.Textbox(label, key); return Next(); }
        public PanelFluentWrapper Slider(string label, string key) { _panel.FlushPending(); _panel.Slider(label, key); return Next(); }
        public PanelFluentWrapper Button(string label) { _panel.FlushPending(); _panel.Button(label); return Next(); }
        public PanelFluentWrapper IntegerInput(string label, string key) { _panel.FlushPending(); _panel.IntegerInput(label, key); return Next(); }
        public PanelFluentWrapper DurationInput(string label, string key) { _panel.FlushPending(); _panel.DurationInput(label, key); return Next(); }
        public PanelFluentWrapper Filepath(string label, string key) { _panel.FlushPending(); _panel.Filepath(label, key); return Next(); }
        public PanelFluentWrapper ResponseBox(string label, string key) { _panel.FlushPending(); _panel.ResponseBox(label, key); return Next(); }
        public PanelFluentWrapper DecimalStepper(string label, string key) { _panel.FlushPending(); _panel.DecimalStepper(label, key); return Next(); }
        public PanelFluentWrapper ColorPicker(string label, string key) { _panel.FlushPending(); _panel.ColorPicker(label, key); return Next(); }
        public PanelFluentWrapper SliderWithToggle(string label, string key) { _panel.FlushPending(); _panel.SliderWithToggle(label, key); return Next(); }
        public PanelFluentWrapper RefreshableDropdown(string label, string key) { _panel.FlushPending(); _panel.RefreshableDropdown(label, key); return Next(); }
        public PanelFluentWrapper CompetingToggles(string label, string key) { _panel.FlushPending(); _panel.CompetingToggles(label, key); return Next(); }
        public PanelFluentWrapper DynamicTextboxes(string label, string key) { _panel.FlushPending(); _panel.DynamicTextboxes(label, key); return Next(); }
        public PanelFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { _panel.FlushPending(); _panel.WithVisibility(toggleKey, inverted, build); return Next(); }
        public PanelFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow) { _panel.FlushPending(); _panel.WithRepeatableRows(saveKey, buildRow); return Next(); }

        // ---- Option methods: forward to current pending builder (dynamic) ----
        public PanelFluentWrapper Hint(string text) { if (_pending != null) { ((dynamic)_pending).Hint(text); } return this; }
        public PanelFluentWrapper Default(bool value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public PanelFluentWrapper Default(string value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public PanelFluentWrapper Default(int value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public PanelFluentWrapper Default(double value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public PanelFluentWrapper Range(int min, int max) { if (_pending != null) { ((dynamic)_pending).Range(min, max); } return this; }
        public PanelFluentWrapper Range(double min, double max) { if (_pending != null) { ((dynamic)_pending).Range(min, max); } return this; }
        public PanelFluentWrapper Step(double value) { if (_pending != null) { ((dynamic)_pending).Step(value); } return this; }
        public PanelFluentWrapper Password() { if (_pending != null) { ((dynamic)_pending).Password(); } return this; }
        public PanelFluentWrapper ShowWhen(string key) { if (_pending != null) { ((dynamic)_pending).ShowWhen(key); } return this; }
        public PanelFluentWrapper Options(string[] options) { if (_pending != null) { ((dynamic)_pending).Options(options); } return this; }
        public PanelFluentWrapper DefaultIndex(int index) { if (_pending != null) { ((dynamic)_pending).DefaultIndex(index); } return this; }
        public PanelFluentWrapper Refresh(Func<string[]> callback) { if (_pending != null) { ((dynamic)_pending).Refresh(callback); } return this; }
        public PanelFluentWrapper Preset(string[] values) { if (_pending != null) { ((dynamic)_pending).Preset(values); } return this; }
        public PanelFluentWrapper ToggleDefault(bool value) { if (_pending != null) { ((dynamic)_pending).ToggleDefault(value); } return this; }
        public PanelFluentWrapper Color(string hex) { if (_pending != null) { ((dynamic)_pending).Color(hex); } return this; }
        public PanelFluentWrapper Text(string caption) { if (_pending != null) { ((dynamic)_pending).Text(caption); } return this; }
        public PanelFluentWrapper OnClick(Action<UiContext> callback) { if (_pending != null) { ((dynamic)_pending).OnClick(callback); } return this; }
        public PanelFluentWrapper WithPermanentOption(bool value) { if (_pending != null) { ((dynamic)_pending).WithPermanentOption(value); } return this; }
    }
}
