using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper so control chains return a type that has both option methods (Hint, Default, ...)
    /// and section methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class SectionFluentWrapper : FluentWrapperBase<SectionFluentWrapper, SectionBuilder>
    {
        internal SectionFluentWrapper(SectionBuilder section, IFlushableControlBuilder pending)
            : base(section, pending as IControlOptions) { }

        private SectionFluentWrapper Next() => new SectionFluentWrapper(Host, Host.GetPending());

        public SectionFluentWrapper Intro(string text) { Host.FlushPending(); Host.Intro(text); return Next(); }
        public SectionFluentWrapper Title(string text) { Host.FlushPending(); Host.Title(text); return Next(); }
        public SectionFluentWrapper Separator() { Host.FlushPending(); Host.Separator(); return Next(); }
        public SectionFluentWrapper Toggle(string label, string key) => Control(s => s.Toggle(label, key));
        public SectionFluentWrapper Textbox(string label, string key) => Control(s => s.Textbox(label, key));
        public SectionFluentWrapper Slider(string label, string key) => Control(s => s.Slider(label, key));
        public SectionFluentWrapper Button(string label) => Control(s => s.Button(label));
        public SectionFluentWrapper IntegerInput(string label, string key) => Control(s => s.IntegerInput(label, key));
        public SectionFluentWrapper DurationInput(string label, string key) => Control(s => s.DurationInput(label, key));
        public SectionFluentWrapper Filepath(string label, string key) => Control(s => s.Filepath(label, key));
        public SectionFluentWrapper ResponseBox(string label, string key) => Control(s => s.ResponseBox(label, key));
        public SectionFluentWrapper DecimalStepper(string label, string key) => Control(s => s.DecimalStepper(label, key));
        public SectionFluentWrapper ColorPicker(string label, string key) => Control(s => s.ColorPicker(label, key));
        public SectionFluentWrapper SliderWithToggle(string label, string key) => Control(s => s.SliderWithToggle(label, key));
        public SectionFluentWrapper Dropdown(string label, string key) => Control(s => s.Dropdown(label, key));
        public SectionFluentWrapper CompetingToggles(string label, string key) => Control(s => s.CompetingToggles(label, key));
        public SectionFluentWrapper DynamicTextboxes(string label, string key) => Control(s => s.DynamicTextboxes(label, key));
        public SectionFluentWrapper PillInput(string label, string key) => Control(s => s.PillInput(label, key));
        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, build); return Next(); }
        public SectionFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { Host.FlushPending(); Host.WithVisibility(toggleKey, inverted, build); return Next(); }
    }
}
