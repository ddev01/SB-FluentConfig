// Tutorial 10 — Reading saved settings from a runtime action
// Companion to 03_DropdownAndButtons.cs (not a settings UI).
//
// Prerequisite: open Tutorial 03 at least once and click Save so the global exists.
// The title string must match 03 exactly ("Tutorial 03 Dropdown And Buttons").
//
// Settings blob: FluentConfig_Settings_Tutorial 03 Dropdown And Buttons (JSON)
// Save keys: mode, level, show_advanced, advanced_offset
//
// Refs: Newtonsoft.Json (same as FluentConfig host — usually in Streamer.bot dlls/).
// See docs/guides/DIALOGS_AND_RUNTIME_VALUES.md.
//
// Previous: 09_PillsAndNestedItems.cs

using Newtonsoft.Json.Linq;
using System;

public class CPHInline
{
    private const string MenuTitle = "Tutorial 03 Dropdown And Buttons";
    private const string SettingsKey = "FluentConfig_Settings_" + MenuTitle;

    public bool Execute()
    {
        string json = CPH.GetGlobalVar<string>(SettingsKey, true);
        if (string.IsNullOrWhiteSpace(json))
        {
            CPH.LogInfo("[Tutorial10] No saved settings — open Tutorial 03 and Save first.");
            return true;
        }

        JObject settings;
        try
        {
            settings = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[Tutorial10] Failed to parse settings JSON: " + ex.Message);
            return true;
        }

        string mode = settings["mode"]?.Value<string>() ?? "Normal";
        int level = settings["level"]?.Value<int>() ?? 75;
        bool showAdvanced = settings["show_advanced"]?.Value<bool>() ?? false;
        int advancedOffset = settings["advanced_offset"]?.Value<int>() ?? 0;

        CPH.LogInfo($"[Tutorial10] mode={mode}, level={level}, show_advanced={showAdvanced}, advanced_offset={advancedOffset}");

        // Example: branch on saved mode in a trigger action
        switch (mode)
        {
            case "Quiet":
                CPH.SetArgument("output_gain", Math.Max(0, level / 2));
                break;
            case "Loud":
                CPH.SetArgument("output_gain", Math.Min(100, level + 10));
                break;
            default:
                CPH.SetArgument("output_gain", level);
                break;
        }

        if (showAdvanced)
            CPH.SetArgument("offset", advancedOffset);

        return true;
    }
}
