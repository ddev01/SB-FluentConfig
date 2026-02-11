using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper for panel so control chains return a type that has both option methods (Hint, Default, ...)
    /// and panel methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class PanelFluentWrapper : FluentWrapperBase<PanelFluentWrapper, PanelBuilder>
    {
        internal PanelFluentWrapper(PanelBuilder panel, IFlushableControlBuilder pending)
            : base(panel, pending as IControlOptions) { }

        private PanelFluentWrapper Next() => new PanelFluentWrapper(Host, Host.GetPending());

        public PanelFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public PanelFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public PanelFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }
        public PanelFluentWrapper Toggle(string label, string key) => Control(p => p.Toggle(label, key));
        public PanelFluentWrapper Textbox(string label, string key) => Control(p => p.Textbox(label, key));
        public PanelFluentWrapper Slider(string label, string key) => Control(p => p.Slider(label, key));
        public PanelFluentWrapper Button(string label) => Control(p => p.Button(label));
        public PanelFluentWrapper IntegerInput(string label, string key) => Control(p => p.IntegerInput(label, key));
        public PanelFluentWrapper DurationInput(string label, string key) => Control(p => p.DurationInput(label, key));
        public PanelFluentWrapper Filepath(string label, string key) => Control(p => p.Filepath(label, key));
        public PanelFluentWrapper DecimalStepper(string label, string key) => Control(p => p.DecimalStepper(label, key));
        public PanelFluentWrapper ColorPicker(string label, string key) => Control(p => p.ColorPicker(label, key));
        public PanelFluentWrapper Dropdown(string label, string key) => Control(p => p.Dropdown(label, key));
        public PanelFluentWrapper DynamicTextboxes(string label, string key) => Control(p => p.DynamicTextboxes(label, key));
        public PanelFluentWrapper PillInput(string label, string key) => Control(p => p.PillInput(label, key));
        public PanelFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, inverted, build); return Next(); }
        public PanelFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow) { Host.FlushPending(); Host.WithRepeatableRows(saveKey, buildRow); return Next(); }
    }
}
