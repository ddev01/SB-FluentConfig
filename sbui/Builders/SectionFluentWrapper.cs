using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper so control chains return a type that has both option methods (Hint, Default, ...)
    /// and section methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class SectionFluentWrapper : FluentWrapperBase<SectionFluentWrapper>
    {
        private readonly SectionBuilder _section;

        internal SectionFluentWrapper(SectionBuilder section, IFlushableControlBuilder pending)
            : base(pending as IControlOptions)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));
        }

        private SectionFluentWrapper Next() => new SectionFluentWrapper(_section, _section.GetPending());

        // ---- Section structure/controls: flush and delegate to section ----
        public SectionFluentWrapper Intro(string text) { _section.FlushPending(); _section.Intro(text); return Next(); }
        public SectionFluentWrapper Title(string text) { _section.FlushPending(); _section.Title(text); return Next(); }
        public SectionFluentWrapper Separator() { _section.FlushPending(); _section.Separator(); return Next(); }
        public SectionFluentWrapper Toggle(string label, string key) { _section.FlushPending(); _section.Toggle(label, key); return Next(); }
        public SectionFluentWrapper Textbox(string label, string key) { _section.FlushPending(); _section.Textbox(label, key); return Next(); }
        public SectionFluentWrapper Slider(string label, string key) { _section.FlushPending(); _section.Slider(label, key); return Next(); }
        public SectionFluentWrapper Button(string label) { _section.FlushPending(); _section.Button(label); return Next(); }
        public SectionFluentWrapper IntegerInput(string label, string key) { _section.FlushPending(); _section.IntegerInput(label, key); return Next(); }
        public SectionFluentWrapper DurationInput(string label, string key) { _section.FlushPending(); _section.DurationInput(label, key); return Next(); }
        public SectionFluentWrapper Filepath(string label, string key) { _section.FlushPending(); _section.Filepath(label, key); return Next(); }
        public SectionFluentWrapper ResponseBox(string label, string key) { _section.FlushPending(); _section.ResponseBox(label, key); return Next(); }
        public SectionFluentWrapper DecimalStepper(string label, string key) { _section.FlushPending(); _section.DecimalStepper(label, key); return Next(); }
        public SectionFluentWrapper ColorPicker(string label, string key) { _section.FlushPending(); _section.ColorPicker(label, key); return Next(); }
        public SectionFluentWrapper SliderWithToggle(string label, string key) { _section.FlushPending(); _section.SliderWithToggle(label, key); return Next(); }
        public SectionFluentWrapper RefreshableDropdown(string label, string key) { _section.FlushPending(); _section.RefreshableDropdown(label, key); return Next(); }
        public SectionFluentWrapper CompetingToggles(string label, string key) { _section.FlushPending(); _section.CompetingToggles(label, key); return Next(); }
        public SectionFluentWrapper DynamicTextboxes(string label, string key) { _section.FlushPending(); _section.DynamicTextboxes(label, key); return Next(); }
        public SectionFluentWrapper PillInput(string label, string key) { _section.FlushPending(); _section.PillInput(label, key); return Next(); }
        public SectionFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build) { _section.FlushPending(); _section.WithVisibility(toggleKey, build); return Next(); }
        public SectionFluentWrapper WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build) { _section.FlushPending(); _section.WithVisibility(toggleKey, inverted, build); return Next(); }
    }
}
