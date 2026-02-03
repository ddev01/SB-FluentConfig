using System;
using Sbui.Elements;

namespace Sbui.Core
{
    public static class PendingItemConverter
    {
        public static UIElement ToElement(PendingItem item)
        {
            if (item?.Args == null) return null;

            var args = item.Args;
            var vk = item.VisibilityKey;

            switch (item.Kind)
            {
                case PendingKind.Header: return null;
                case PendingKind.Title: return new TitleElement((string)args[0], (string)args[1], vk);
                case PendingKind.Description: return new DescriptionElement((string)args[0], (string)args[1], vk);
                case PendingKind.ToggleSwitch: return new ToggleSwitchElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (bool)args[4], vk);
                case PendingKind.Textbox: return new TextboxElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (bool)args[5], vk);
                case PendingKind.Slider: return new SliderElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], vk);
                case PendingKind.InlineSeparator: return new InlineSeparatorElement((string)args[0], vk);
                case PendingKind.SliderWithToggleSwitch: return new SliderWithToggleSwitchElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (int)args[4], (int)args[5], (int)args[6], (bool)args[7], vk);
                case PendingKind.Filepath: return new FilepathElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                case PendingKind.ClickableButton: return new ClickableButtonElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (Action)args[5], vk);
                case PendingKind.RefreshableDropdown: return new RefreshableDropdownElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (Func<string[]>)args[5], (int)args[6], vk);
                case PendingKind.ResponseBox: return new ResponseBoxElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                case PendingKind.DecimalStepper: return new DecimalStepperElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (double)args[4], (double)args[5], (double)args[6], (double)args[7], vk);
                case PendingKind.CompetingToggleSwitches: return new CompetingToggleSwitchesElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], (int)args[5], vk);
                case PendingKind.DynamicTextboxesWithPreset: return new DynamicTextboxesWithPresetElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string[])args[4], vk);
                case PendingKind.ColorPicker: return new ColorPickerElement((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], vk);
                default: return null;
            }
        }

        public static string GetTabName(PendingItem item)
        {
            if (item?.Args == null || item.Args.Length == 0) return "";

            switch (item.Kind)
            {
                case PendingKind.Title:
                case PendingKind.Description: return (item.Args.Length > 1 ? item.Args[1] : null) as string ?? "";
                case PendingKind.InlineSeparator: return (item.Args[0] as string) ?? "";
                case PendingKind.ClickableButton: return (item.Args.Length > 4 ? item.Args[4] : null) as string ?? "";
                default: return (item.Args.Length > 2 ? item.Args[2] : null) as string ?? "";
            }
        }
    }
}
