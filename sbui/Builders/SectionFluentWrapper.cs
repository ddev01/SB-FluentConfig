using System;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Fluent wrapper so control chains return a type that has both option methods (Hint, Default, ...)
    /// and section methods (Toggle, Textbox, ...), allowing .Toggle().Hint().Textbox() without .Add().
    /// </summary>
    public class SectionFluentWrapper
    {
        private readonly SectionBuilder _section;
        private readonly IFlushableControlBuilder _pending;

        internal SectionFluentWrapper(SectionBuilder section, IFlushableControlBuilder pending)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));
            _pending = pending;
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

        // ---- Option methods: forward to current pending builder (dynamic) ----
        public SectionFluentWrapper Hint(string text) { if (_pending != null) { ((dynamic)_pending).Hint(text); } return this; }
        public SectionFluentWrapper Default(bool value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public SectionFluentWrapper Default(string value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public SectionFluentWrapper Default(int value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public SectionFluentWrapper Default(double value) { if (_pending != null) { ((dynamic)_pending).Default(value); } return this; }
        public SectionFluentWrapper Range(int min, int max) { if (_pending != null) { ((dynamic)_pending).Range(min, max); } return this; }
        public SectionFluentWrapper Range(double min, double max) { if (_pending != null) { ((dynamic)_pending).Range(min, max); } return this; }
        public SectionFluentWrapper Step(double value) { if (_pending != null) { ((dynamic)_pending).Step(value); } return this; }
        public SectionFluentWrapper Password() { if (_pending != null) { ((dynamic)_pending).Password(); } return this; }
        public SectionFluentWrapper ShowWhen(string key) { if (_pending != null) { ((dynamic)_pending).ShowWhen(key); } return this; }
        public SectionFluentWrapper Options(string[] options) { if (_pending != null) { ((dynamic)_pending).Options(options); } return this; }
        public SectionFluentWrapper DefaultIndex(int index) { if (_pending != null) { ((dynamic)_pending).DefaultIndex(index); } return this; }
        public SectionFluentWrapper Refresh(Func<string[]> callback) { if (_pending != null) { ((dynamic)_pending).Refresh(callback); } return this; }
        public SectionFluentWrapper Preset(string[] values) { if (_pending != null) { ((dynamic)_pending).Preset(values); } return this; }
        public SectionFluentWrapper ToggleDefault(bool value) { if (_pending != null) { ((dynamic)_pending).ToggleDefault(value); } return this; }
        public SectionFluentWrapper Color(string hex) { if (_pending != null) { ((dynamic)_pending).Color(hex); } return this; }
        public SectionFluentWrapper Text(string caption) { if (_pending != null) { ((dynamic)_pending).Text(caption); } return this; }
        public SectionFluentWrapper OnClick(Action<UiContext> callback) { if (_pending != null) { ((dynamic)_pending).OnClick(callback); } return this; }
        public SectionFluentWrapper WithPermanentOption(bool value) { if (_pending != null) { ((dynamic)_pending).WithPermanentOption(value); } return this; }
        public SectionFluentWrapper WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { if (_pending != null) { ((dynamic)_pending).WithSectionsPanel(build); } return this; }
        public SectionFluentWrapper OnPillAdded(Action<string, StackPanel, CallbackContext> build) { if (_pending != null) { ((dynamic)_pending).OnPillAdded(build); } return this; }
        public SectionFluentWrapper OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { if (_pending != null) { ((dynamic)_pending).OnPillRemoved(build); } return this; }
    }
}
