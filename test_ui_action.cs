// Streamer.bot C# Action Code
// Copy and paste this into a new C# action in Streamer.bot
// Tests all Sbui functionality: existing controls, Phase 1–4 (GetPendingValue, dialogs, Phase 2–4 controls, Toast, Log),
// visibility API (showWhenEnabled + WithVisibility), and UI fixes (progress on background thread, etc.).

using Sbui;
using System;
using System.IO;
using System.Windows;

public class CPHInline
{
    private static string GetLogPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string logDir = Path.Combine(baseDir, "logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "ui.txt");
    }

    private static void WriteLog(string message)
    {
        try
        {
            string logPath = GetLogPath();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(logPath, $"[{timestamp}] {message}" + Environment.NewLine);
        }
        catch (Exception ex)
        {
            try
            {
                string backupPath = Path.Combine(Path.GetTempPath(), "streamerbot_ui_log.txt");
                File.AppendAllText(backupPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message} - Error: {ex.Message}{Environment.NewLine}");
            }
            catch { }
        }
    }

    public bool Execute()
    {
        try
        {
            WriteLog("=== Sbui Full Test Action Started ===");
            WriteLog($"Application.Current is null: {Application.Current == null}");

            Sbui.Sbui.SetLogCallback((msg) => WriteLog($"[Sbui] {msg}"));

            if (Sbui.Sbui.AlreadyOpened("Sbui Full Test", "1.0"))
            {
                WriteLog("=== Action skipped - UI already open ===");
                return true;
            }

            WriteLog($"Sbui.GetVersion() = {Sbui.Sbui.GetVersion()}");

            var ui = new Sbui.Sbui(CPH, "Sbui Full Test", "1.0", true);

            // --- General tab: existing + Phase 2/3 controls ---
            ui.AddHeader("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTYxAXPiJKEN56xb3g_LJzPaNVuOu2nW9a4VQ&s");
            ui.AddTitle("General Settings", "General");
            ui.AddDescription("Test all Sbui controls. Use Save to persist; buttons below exercise dialogs and GetPendingValue.", "General");

            ui.AddToggleSwitch("Enable Requirement", "Turn this on to require a custom condition.", "General", "custom_requirement_enabled", false);
            ui.AddTextbox("Requirement Name", "The name of your requirement (e.g. 'Level', 'AccessKey').", "General", "custom_requirement_name", "", false);
            ui.AddTextbox("Secret Key", "Optional secret (password field).", "General", "secret_key", "", isPassword: true);
            ui.AddSlider("Required Value", "Minimum value needed (1–1000000).", "General", "custom_required_value", 1, 1000000, 250);

            ui.AddInlineSeparator("General");

            ui.AddSliderWithToggleSwitch("Optional Limit", "Enable and set a limit (0–100).", "General", "optional_limit", 0, 100, 50, false);
            ui.AddFilepath("Export Filepath", "Path for export file. Leave empty for Streamer.bot directory.", "General", "export_filepath", "");
            ui.AddResponseBox("Response Template", "Multiline template for responses.", "General", "response_template", "Hello {user}!");
            ui.AddDecimalStepper("Rate", "Rate value (0.1–10, step 0.1).", "General", "rate_value", 0.1, 10.0, 0.1, 1.0);
            ui.AddColorPicker("Accent Color", "Hex color (e.g. #714bfd).", "General", "accent_color", "#714bfd");

            // Conditional visibility: toggle controls single slider (showWhenEnabled)
            ui.AddToggleSwitch("Min points required", "When on, the min points slider below is visible.", "General", "minpointsrequired", false);
            ui.AddSlider("Min points", "Only visible when 'Min points required' is on.", "General", "minpoints", 0, 100, 10, showWhenEnabled: "minpointsrequired");

            // --- Buttons & Dialogs tab: GetPendingValue, AddPopupWindow, ShowConfirmDialog, ShowProgressWindow, Toast, Log ---
            ui.AddTitle("Buttons & Dialogs", "Buttons");
            ui.AddDescription("Click buttons to test GetPendingValue, AddPopupWindow, ShowConfirmDialog, ShowProgressWindow, Toast, and Log.", "Buttons");

            ui.AddClickableButton("Test GetPendingValue & Popup", "Reads multiple control types from General/Dropdowns and shows in a popup.", "Show values", "#714bfd", "Buttons", () =>
            {
                string name = ui.GetPendingValue<string>("custom_requirement_name");
                string path = ui.GetPendingValue<string>("export_filepath");
                int sliderVal = ui.GetPendingValue<int>("custom_required_value");
                bool enabled = ui.GetPendingValue<bool>("custom_requirement_enabled");
                double rate = ui.GetPendingValue<double>("rate_value");
                int dropdownIdx = ui.GetPendingValue<int>("dropdown_choice");
                string dropdownText = ui.GetPendingValue<string>("dropdown_choice");
                ui.AddPopupWindow("GetPendingValue Test", $"Requirement Name: '{name}'\nExport Filepath: '{path}'\nRequired Value: {sliderVal}\nEnable Requirement: {enabled}\nRate: {rate}\nDropdown index: {dropdownIdx} text: '{dropdownText}'");
            });

            ui.AddClickableButton("Test Confirm Dialog", "Shows Yes/No dialog and displays result in a popup.", "Confirm", "#31a8ff", "Buttons", () =>
            {
                var result = ui.ShowConfirmDialog("Confirm Test", "Do you want to continue?", "Yes", "No");
                ui.AddPopupWindow("Confirm Result", $"You chose: {result}");
            });

            ui.AddClickableButton("Test Progress Window", "Shows progress window, simulates 10 steps, then closes.", "Progress", "#1ba489", "Buttons", () =>
            {
                const int total = 10;
                var progress = ui.ShowProgressWindow("Progress Test", "Simulating work...", "Progress", total);
                if (progress != null)
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        for (int i = 0; i <= total; i++)
                        {
                            System.Threading.Thread.Sleep(200);
                            progress.Report(i);
                        }
                        progress.Close();
                        ui.AddPopupWindow("Progress Done", "Progress window completed.");
                    });
                }
            });

            ui.AddClickableButton("Test Toast", "Shows a short-lived toast notification.", "Toast", "#f09930", "Buttons", () =>
            {
                ui.Toast("Test toast – auto-dismiss in 3 seconds");
            });

            ui.AddClickableButton("Test Log", "Writes a line to the log callback.", "Log", "#636b9a", "Buttons", () =>
            {
                ui.Log("Test log message from button click");
            });

            // --- Dropdowns tab: AddRefreshableDropdown, UpdateDropdown ---
            ui.AddTitle("Refreshable Dropdown", "Dropdowns");
            ui.AddDescription("Dropdown with Refresh button; callback returns new options and UpdateDropdown is called.", "Dropdowns");

            var options = new[] { "Option A", "Option B", "Option C" };
            ui.AddRefreshableDropdown("Choice", "Select an option. Click Refresh to reload list.", "Dropdowns", "dropdown_choice", options, () =>
            {
                return new[] { "Option A", "Option B", "Option C", "Option D (refreshed)" };
            }, 0);

            // --- Advanced tab: AddCompetingToggleSwitches, AddDynamicTextboxesWithPreset, WithVisibility (section) ---
            ui.AddTitle("Advanced Controls", "Advanced");
            ui.AddDescription("Competing toggles (one active), dynamic textbox list, and WithVisibility section.", "Advanced");

            ui.AddCompetingToggleSwitches("Mode", "Only one mode can be active.", "Advanced", "mode_index", new[] { "Mode 1", "Mode 2", "Mode 3" }, 0);
            ui.AddDynamicTextboxesWithPreset("Custom List", "Add/remove textboxes; persisted as array.", "Advanced", "custom_list", new[] { "Item 1", "Item 2" });

            // WithVisibility (section): toggle controls visibility of multiple controls in a block
            ui.AddToggleSwitch("Show extra options", "When on, the extra options block below is visible.", "Advanced", "show_extra_options", false);
            ui.WithVisibility("show_extra_options", "Advanced", () =>
            {
                ui.AddTextbox("Extra option A", "Only visible when 'Show extra options' is on.", "Advanced", "extra_option_a", "", false);
                ui.AddSlider("Extra option B", "Only visible when 'Show extra options' is on.", "Advanced", "extra_option_b", 0, 50, 25);
            });

            ui.LogExistingSettings();
            WriteLog("Sbui instance built, calling ShowUI...");

            ui.ShowUI();
            WriteLog("ShowUI() completed");
            WriteLog("=== Action completed - UI should now be visible ===");
        }
        catch (Exception ex)
        {
            WriteLog("=== ERROR OCCURRED ===");
            WriteLog($"Error Type: {ex.GetType().Name}");
            WriteLog($"Error Message: {ex.Message}");
            WriteLog($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                WriteLog($"Inner Exception: {ex.InnerException.Message}");
                WriteLog($"Inner Stack: {ex.InnerException.StackTrace}");
            }
        }

        return true;
    }
}
