// Streamer.bot C# action – read saved settings from MediumExample.
// Companion to examples/menu/MediumExample.cs (not a settings UI).
//
// Prerequisite: open Medium Example at least once and click Save so the global exists.
// The title string must match MediumExample exactly ("Medium Example").
//
// Settings blob: FluentConfig_Settings_Medium Example (JSON)
// Save keys: mode, level, show_advanced, advanced_offset
//
// Refs: Newtonsoft.Json (same as FluentConfig host — usually in Streamer.bot dlls/).

using Newtonsoft.Json.Linq;
using System;

public class CPHInline
{
    private const string MediumTitle = "Medium Example";
    private const string SettingsKey = "FluentConfig_Settings_" + MediumTitle;

    public bool Execute()
    {
        string json = CPH.GetGlobalVar<string>(SettingsKey, true);
        if (string.IsNullOrWhiteSpace(json))
        {
            CPH.LogInfo("[MediumValues] No saved settings — open Medium Example and Save first.");
            return true;
        }

        JObject settings;
        try
        {
            settings = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[MediumValues] Failed to parse settings JSON: " + ex.Message);
            return true;
        }

        string mode = settings["mode"]?.Value<string>() ?? "Normal";
        int level = settings["level"]?.Value<int>() ?? 75;
        bool showAdvanced = settings["show_advanced"]?.Value<bool>() ?? false;
        int advancedOffset = settings["advanced_offset"]?.Value<int>() ?? 0;

        CPH.LogInfo($"[MediumValues] mode={mode}, level={level}, show_advanced={showAdvanced}, advanced_offset={advancedOffset}");

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
