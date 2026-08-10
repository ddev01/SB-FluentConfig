// Tutorial 10 — Reading saved settings from a runtime action
// Companion to 03_DropdownAndButtons.cs (not a settings UI).
//
// Prerequisite: open Tutorial 03 at least once and click Save so the global exists.
// The title string must match 03 exactly ("Tutorial 03 Dropdown And Buttons").
//
// Settings blob: tutorial_03_dropdown_and_buttons_settings (JSON)
// Save keys: mode, level, show_advanced, advanced_offset
//
// Refs: FluentConfig.dll (Streamer.bot dlls/).
// See docs/guides/DIALOGS_AND_RUNTIME_VALUES.md.
//
// Previous: 09_PillsAndNestedItems.cs

using FluentConfig;
using System;

public class CPHInline
{
    private const string MenuTitle = "Tutorial 03 Dropdown And Buttons";

    private class Settings
    {
        public string Mode { get; set; } = "Normal";
        public int Level { get; set; } = 75;
        public bool ShowAdvanced { get; set; } = false;
        public int AdvancedOffset { get; set; } = 0;
    }

    public bool Execute()
    {
        var settings = Fc.LoadSettings<Settings>(CPH, MenuTitle);

        // Detect "never saved" by checking the global key directly (LoadSettings always
        // returns a populated defaults object).
        string key = Fc.SettingsKeyFor(MenuTitle);
        string raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw))
        {
            CPH.LogInfo("[Tutorial10] No saved settings — open Tutorial 03 and Save first.");
            return true;
        }

        CPH.LogInfo($"[Tutorial10] mode={settings.Mode}, level={settings.Level}, show_advanced={settings.ShowAdvanced}, advanced_offset={settings.AdvancedOffset}");

        // Example: branch on saved mode in a trigger action
        switch (settings.Mode)
        {
            case "Quiet":
                CPH.SetArgument("output_gain", Math.Max(0, settings.Level / 2));
                break;
            case "Loud":
                CPH.SetArgument("output_gain", Math.Min(100, settings.Level + 10));
                break;
            default:
                CPH.SetArgument("output_gain", settings.Level);
                break;
        }

        if (settings.ShowAdvanced)
            CPH.SetArgument("offset", settings.AdvancedOffset);

        return true;
    }
}
