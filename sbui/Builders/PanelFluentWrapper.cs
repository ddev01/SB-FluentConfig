using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper for panel so control chains return a type that has both option methods (Hint, Default, ...)
    /// and panel methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class PanelFluentWrapper : FluentWrapperBase<PanelFluentWrapper>
    {
        private readonly PanelBuilder _panel;

        internal PanelFluentWrapper(PanelBuilder panel, IFlushableControlBuilder pending)
            : base(pending as IControlOptions)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
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
    }
}
