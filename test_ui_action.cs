// Streamer.bot C# Action Code
// Copy and paste this into a new C# action in Streamer.bot
// Tests Sbui fluent DSL: all controls, GetPendingValue, dialogs, Toast, Log, visibility, dropdown refresh.

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

            var options = new[] { "Option A", "Option B", "Option C" };

            SbuiUi.Create(CPH, "Sbui Full Test", "1.0")
                .Header("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTYxAXPiJKEN56xb3g_LJzPaNVuOu2nW9a4VQ&s")
                .Section("General Settings", "General", g => g
                    .Intro("Test all Sbui controls. Use Save to persist; buttons below exercise dialogs and GetPendingValue.")
                    .Toggle("Enable Requirement", "custom_requirement_enabled")
                        .Hint("Turn this on to require a custom condition.")
                    .Textbox("Requirement Name", "custom_requirement_name")
                        .Hint("The name of your requirement (e.g. 'Level', 'AccessKey').")
                    .Textbox("Secret Key", "secret_key")
                        .Hint("Optional secret (password field).")
                        .Password()
                    .Slider("Required Value", "custom_required_value")
                        .Hint("Minimum value needed (1–1000000).")
                        .Range(1, 1000000)
                        .Default(250)
                    .Separator()
                    .Toggle("Enable Optional Limit", "optional_limit_enabled")
                        .Hint("Enable and set a limit (0–100).")
                        .Default(false)
                    .Slider("Optional Limit", "optional_limit")
                        .Hint("Limit value when enabled.")
                        .Range(0, 100)
                        .Default(50)
                        .ShowWhen("optional_limit_enabled")
                    .Filepath("Export Filepath", "export_filepath")
                        .Hint("Path for export file. Leave empty for Streamer.bot directory.")
                    .Textbox("Response Template", "response_template")
                        .Multiline()
                        .Hint("Multiline template for responses.")
                        .Default("Hello {user}!")
                    .DecimalStepper("Rate", "rate_value")
                        .Hint("Rate value (0.1–10, step 0.1).")
                        .Range(0.1, 10.0)
                        .Step(0.1)
                        .Default(1.0)
                    .ColorPicker("Accent Color", "accent_color")
                        .Hint("Hex color (e.g. #714bfd).")
                        .Default("#714bfd")
                    .Toggle("Min points required", "minpointsrequired")
                        .Hint("When on, the min points slider below is visible.")
                    .Slider("Min points", "minpoints")
                        .Hint("Only visible when 'Min points required' is on.")
                        .Range(0, 100)
                        .Default(10)
                        .ShowWhen("minpointsrequired")
                )
                .Section("Buttons & Dialogs", "Buttons", b => b
                    .Intro("Click buttons to test GetPendingValue, AddPopupWindow, ShowConfirmDialog, ShowProgressWindow, Toast, and Log.")
                    .Button("Test GetPendingValue & Popup")
                        .Hint("Reads multiple control types from General/Dropdowns and shows in a popup.")
                        .Text("Show values")
                        .Color("#714bfd")
                        .OnClick(ui =>
                        {
                            string name = ui.Pending<string>("custom_requirement_name");
                            string path = ui.Pending<string>("export_filepath");
                            int sliderVal = ui.Pending<int>("custom_required_value");
                            bool enabled = ui.Pending<bool>("custom_requirement_enabled");
                            double rate = ui.Pending<double>("rate_value");
                            int dropdownIdx = ui.Pending<int>("dropdown_choice");
                            string dropdownText = ui.Pending<string>("dropdown_choice");
                            ui.Popup("GetPendingValue Test", $"Requirement Name: '{name}'\nExport Filepath: '{path}'\nRequired Value: {sliderVal}\nEnable Requirement: {enabled}\nRate: {rate}\nDropdown index: {dropdownIdx} text: '{dropdownText}'");
                        })
                    .Button("Test Confirm Dialog")
                        .Hint("Shows Yes/No dialog and displays result in a popup.")
                        .Text("Confirm")
                        .Color("#31a8ff")
                        .OnClick(ui =>
                        {
                            var result = ui.ShowConfirmDialog("Confirm Test", "Do you want to continue?", "Yes", "No");
                            ui.Popup("Confirm Result", $"You chose: {result}");
                        })
                    .Button("Test Progress Window")
                        .Hint("Shows progress window, simulates 10 steps, then closes.")
                        .Text("Progress")
                        .Color("#1ba489")
                        .OnClick(ui =>
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
                                    ui.Popup("Progress Done", "Progress window completed.");
                                });
                            }
                        })
                    .Button("Test Toast")
                        .Hint("Shows a short-lived toast notification.")
                        .Text("Toast")
                        .Color("#f09930")
                        .OnClick(ui => ui.Toast("Test toast – auto-dismiss in 3 seconds"))
                    .Button("Test Log")
                        .Hint("Writes a line to the log callback.")
                        .Text("Log")
                        .Color("#636b9a")
                        .OnClick(ui => ui.Log("Test log message from button click"))
                )
                .Section("Refreshable Dropdown", "Dropdowns", d => d
                    .Intro("Dropdown with Refresh button; callback returns new options and UpdateDropdown is called.")
                    .Dropdown("Choice", "dropdown_choice")
                        .Hint("Select an option. Click Refresh to reload list.")
                        .Options(options)
                        .Refresh(() => new[] { "Option A", "Option B", "Option C", "Option D (refreshed)" })
                        .DefaultIndex(0)
                )
                .Section("Advanced Controls", "Advanced", a => a
                    .Intro("Exclusive toggles (one active), dynamic textbox list, and WithVisibility section.")
                    .Toggle("Mode", "mode_index")
                        .Hint("Only one mode can be active.")
                        .WithExclusive(new[] { "Mode 1", "Mode 2", "Mode 3" })
                        .DefaultIndex(0)
                    .DynamicTextboxes("Custom List", "custom_list")
                        .Hint("Add/remove textboxes; persisted as array.")
                        .Preset(new[] { "Item 1", "Item 2" })
                    .Toggle("Show extra options", "show_extra_options")
                        .Hint("When on, the extra options block below is visible.")
                    .WithVisibility("show_extra_options", inner => inner
                        .Textbox("Extra option A", "extra_option_a")
                            .Hint("Only visible when 'Show extra options' is on.")
                        .Slider("Extra option B", "extra_option_b")
                            .Hint("Only visible when 'Show extra options' is on.")
                            .Range(0, 50)
                            .Default(25)
                    )
                )
                .LogExistingSettings()
                .Show();

            WriteLog("Show() completed");
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
