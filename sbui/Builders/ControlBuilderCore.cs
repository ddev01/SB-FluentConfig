using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Shared factory for control builders. Used by PanelBuilder and SectionBuilder to avoid duplicating constructor calls.
    /// </summary>
    internal sealed class ControlBuilderCore
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        private readonly Sbui _ui;

        public ControlBuilderCore(IAddStrategy strategy, string tabName, Sbui ui)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public ToggleBuilder CreateToggle(string label, string key) => new ToggleBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public TextboxBuilder CreateTextbox(string label, string key) => new TextboxBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public SliderBuilder CreateSlider(string label, string key) => new SliderBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public ButtonBuilder CreateButton(string label) => new ButtonBuilder(_strategy, _ui, _tabName, label ?? "");
        public IntegerInputBuilder CreateIntegerInput(string label, string key) => new IntegerInputBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public DurationInputBuilder CreateDurationInput(string label, string key) => new DurationInputBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public FilepathBuilder CreateFilepath(string label, string key) => new FilepathBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public ResponseBoxBuilder CreateResponseBox(string label, string key) => new ResponseBoxBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public DecimalStepperBuilder CreateDecimalStepper(string label, string key) => new DecimalStepperBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public ColorPickerBuilder CreateColorPicker(string label, string key) => new ColorPickerBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public SliderWithToggleBuilder CreateSliderWithToggle(string label, string key) => new SliderWithToggleBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public RefreshableDropdownBuilder CreateRefreshableDropdown(string label, string key) => new RefreshableDropdownBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public CompetingTogglesBuilder CreateCompetingToggles(string label, string key) => new CompetingTogglesBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public DynamicTextboxesBuilder CreateDynamicTextboxes(string label, string key) => new DynamicTextboxesBuilder(_strategy, _tabName, label ?? "", key ?? "");
        public PillInputBuilder CreatePillInput(string label, string key) => new PillInputBuilder(_strategy, _ui, _tabName, label ?? "", key ?? "");
    }
}
