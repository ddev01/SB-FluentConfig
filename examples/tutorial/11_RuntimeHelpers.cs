// Tutorial 11 — Runtime helpers (logger, templates, settings write, data blob)
// Not a settings UI. Reads Tutorial 03's saved settings; stores runtime state in this action's `{slug}_data`.
//
// Demonstrates:
//   Fc.HasSavedSettings / SetSetting
//   Fc.Logger + Init / Failed
//   Fc.CaptureEvent + ApplyTemplate
//   Fc.LoadData / SaveData ({slug}_data — separate from settings)
//
// Refs: FluentConfig.dll (Streamer.bot dlls/).
// See docs/guides/DIALOGS_AND_RUNTIME_VALUES.md.
//
// Previous: 10_ReadingSavedSettings.cs

using FluentConfig;
using System;

public class CPHInline
{
    private const string MenuTitle = "Tutorial 03 Dropdown And Buttons";
    private const string Title = "Tutorial 11 Runtime Helpers";
    private const string Version = "1.0";

    private class RuntimeState
    {
        public int RunCount { get; set; } = 0;
        public string LastUser { get; set; } = "";
    }

    public bool Execute()
    {
        var log = Fc.Logger(CPH, Title, Version);
        log.Init();

        if (!Fc.HasSavedSettings(CPH, MenuTitle))
        {
            log.Info("Open Tutorial 03 and Save first.");
            return true;
        }

        var ev = Fc.CaptureEvent(CPH);
        string chat = Fc.ApplyTemplate("Hello %user%! (from {userName})", ev);
        log.Info(chat);

        // Example: clear a secret from code without opening the UI
        // Fc.SetSetting(CPH, MenuTitle, "access_token", "");

        try
        {
            var state = Fc.LoadData<RuntimeState>(CPH, Title);
            state.RunCount++;
            state.LastUser = string.IsNullOrEmpty(ev.User) ? ev.UserName : ev.User;
            Fc.SaveData(CPH, Title, state);
            log.Info($"run_count={state.RunCount}, last_user={state.LastUser}");
        }
        catch (Exception ex)
        {
            log.Failed("update runtime state", ex);
        }

        return true;
    }
}
