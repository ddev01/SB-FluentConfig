namespace Sbui.Core
{
    public enum PendingKind
    {
        Header,
        Title,
        Description,
        ToggleSwitch,
        Textbox,
        Slider,
        InlineSeparator,
        SliderWithToggleSwitch,
        RefreshableDropdown,
        Filepath,
        ClickableButton,
        ResponseBox,
        DecimalStepper,
        CompetingToggleSwitches,
        DynamicTextboxesWithPreset,
        ColorPicker
    }

    public class PendingItem
    {
        public PendingKind Kind { get; set; }
        public object[] Args { get; set; }
        public string VisibilityKey { get; set; }

        public PendingItem(PendingKind kind, object[] args)
        {
            Kind = kind;
            Args = args;
        }
    }
}
