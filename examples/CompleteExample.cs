// Streamer.bot C# action – complete FluentConfig example.
// Demonstrates all controls: Toggle, Textbox, Slider, Button, Dropdown, PillInput, WithVisibility,
// WithRepeatableRows, GetPendingValue, dialogs, Toast, Log, DurationInput, IntegerInput, ColorPicker, etc.
// Copy into a new C# action to explore the full FluentConfig API.
//
// Required references: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll (path).
// See docs/REFERENCES.md for details.

using FluentConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
            WriteLog("=== FluentConfig Complete Example Started ===");
            FluentConfig.FluentConfig.SetLogCallback((msg) => WriteLog($"[FluentConfig] {msg}"));

            if (FluentConfig.FluentConfig.AlreadyOpened("FluentConfig Complete Example", "1.0"))
            {
                WriteLog("=== Action skipped - UI already open ===");
                return true;
            }

            var options = new[] { "Option A", "Option B", "Option C" };
            var pillPanels = new Dictionary<string, Panel>();

            void AddPillSection(Panel container, string itemName, CallbackContext ctx)
            {
                ctx.WithPanel(container, "Pills", pb => pb
                    .Title($"Item: {itemName}")
                    .Toggle("Enabled", itemName + "_enabled")
                        .Hint("Enable this item.")
                        .Default(true)
                    .Slider("Value", itemName + "_value")
                        .Range(0, 100)
                        .Default(50)
                );
            }

            FluentConfigUi.Create(CPH, "FluentConfig Complete Example", "1.0")
                .Section("General settings", "General", g => g
                    .Intro("All FluentConfig controls. Use Save to persist; buttons exercise dialogs and GetPendingValue.")
                    .Toggle("Enable requirement", "custom_requirement_enabled")
                        .Hint("Turn this on to require a custom condition.")
                    .Textbox("Requirement name", "custom_requirement_name")
                        .Hint("The name of your requirement (e.g. 'Level', 'AccessKey').")
                    .Textbox("Secret key", "secret_key")
                        .Hint("Optional secret (password field).")
                        .Password()
                    .Slider("Required value", "custom_required_value")
                        .Hint("Minimum value needed (1–1000000).")
                        .Range(1, 1000000)
                        .Default(250)
                    .Separator()
                    .Toggle("Enable optional limit", "optional_limit_enabled")
                        .Hint("Enable and set a limit (0–100).")
                        .Default(false)
                    .Slider("Optional limit", "optional_limit")
                        .Hint("Limit value when enabled.")
                        .Range(0, 100)
                        .Default(50)
                        .ShowWhen("optional_limit_enabled")
                    .Filepath("Export filepath", "export_filepath")
                        .Hint("Path for export file. Leave empty for Streamer.bot directory.")
                    .Textbox("Response template", "response_template")
                        .Multiline()
                        .Hint("Multiline template for responses.")
                        .Default("Hello {user}!")
                    .NumberInput("Rate", "rate_value")
                        .Hint("Rate value (0.1–10, step 0.1).")
                        .WithStepper()
                        .Range(0.1, 10.0)
                        .Step(0.1)
                        .Default(1.0)
                    .ColorPicker("Accent color", "accent_color")
                        .Hint("Hex color (e.g. #714bfd).")
                        .Default("#714bfd")
                    .IntegerInput("Max retries", "max_retries")
                        .Hint("Number of retry attempts (0–100).")
                        .Range(0, 100)
                        .Default(3)
                    .DurationInput("Timeout", "timeout")
                        .Hint("Connection timeout. Use full unit names: 30seconds, 5minutes, etc.")
                        .WithPermanentOption(false)
                        .Default("30seconds")
                    .Toggle("Min points required", "minpointsrequired")
                        .Hint("When on, the min points slider below is visible.")
                    .Slider("Min points", "minpoints")
                        .Hint("Only visible when 'Min points required' is on.")
                        .Range(0, 100)
                        .Default(10)
                        .ShowWhen("minpointsrequired")
                )
                .Section("Buttons & dialogs", "Buttons", b => b
                    .Intro("Click buttons to test GetPendingValue, AddPopupWindow, ShowConfirmDialog, ShowProgressWindow, Toast, and Log.")
                    .Button("Test GetPendingValue & popup")
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
                            string deviceId = ui.Pending<string>("device_id");
                            int maxRetries = ui.Pending<int>("max_retries");
                            string timeout = ui.Pending<string>("timeout");
                            ui.Popup("GetPendingValue Test", $"Requirement: '{name}'\nFilepath: '{path}'\nRequired Value: {sliderVal}\nEnabled: {enabled}\nRate: {rate}\nDropdown: {dropdownIdx}='{dropdownText}'\nDevice ID (pair): '{deviceId}'\nMaxRetries: {maxRetries}\nTimeout: '{timeout}'");
                        })
                    .Button("Test confirm dialog")
                        .Hint("Shows Yes/No dialog and displays result in a popup.")
                        .Text("Confirm")
                        .Color("#31a8ff")
                        .OnClick(ui =>
                        {
                            var result = ui.ShowConfirmDialog("Confirm Test", "Do you want to continue?", "Yes", "No");
                            ui.Popup("Confirm Result", $"You chose: {result}");
                        })
                    .Button("Test progress window")
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
                    .Button("Test toast")
                        .Hint("Shows a short-lived toast notification.")
                        .Text("Toast")
                        .Color("#f09930")
                        .OnClick(ui => ui.Toast("Test toast – auto-dismiss in 3 seconds"))
                    .Button("Test log")
                        .Hint("Writes a line to the log callback.")
                        .Text("Log")
                        .Color("#636b9a")
                        .OnClick(ui => ui.Log("Test log message from button click"))
                )
                .Section("Refreshable Dropdown", "Dropdowns", d => d
                    .Intro("Dropdown with Refresh; pair-value dropdown (display name + stored ID).")
                    .Dropdown("Choice", "dropdown_choice")
                        .Hint("Select an option. Click Refresh to reload list.")
                        .Options(options)
                        .Refresh(() => new[] { "Option A", "Option B", "Option C", "Option D (refreshed)" })
                        .DefaultIndex(0)
                    .Dropdown("Device (pair value)", "device_display")
                        .Hint("Stores display name in device_display, ID in device_id.")
                        .WithPairValue("device_id")
                        .Options(new[] { ("id1", "Device A"), ("id2", "Device B"), ("id3", "Device C") })
                        .DefaultByValue("id2")
                )
                .Section("Advanced controls", "Advanced", a => a
                    .Intro("Exclusive toggles, dynamic textbox list, WithVisibility, WithRepeatableRows, inverted visibility.")
                    .Toggle("Mode", "mode_index")
                        .Hint("Only one mode can be active (single-select).")
                        .WithExclusive(new[] { "Mode 1", "Mode 2", "Mode 3" })
                        .DefaultIndex(0)
                    .Toggle("Features (multi-select)", "features")
                        .Hint("Select up to 2 features; uses MaxSelected and DefaultIndices.")
                        .WithExclusive(new[] { "Feature A", "Feature B", "Feature C" })
                        .MaxSelected(2)
                        .DefaultIndices(new[] { 0, 1 })
                    .DynamicTextboxes("Custom list", "custom_list")
                        .Hint("Add/remove textboxes; persisted as array.")
                        .Preset(new[] { "Item 1", "Item 2" })
                    .Toggle("Show extra options", "show_extra_options")
                        .Hint("When on, extra options and repeatable rows below are visible.")
                    .WithVisibility("show_extra_options", inner => inner
                        .Textbox("Extra option A", "extra_option_a")
                            .Hint("Only visible when 'Show extra options' is on.")
                        .Slider("Extra option B", "extra_option_b")
                            .Hint("Only visible when 'Show extra options' is on.")
                            .Range(0, 50)
                            .Default(25)
                        .WithRepeatableRows("extra_rows", row => row
                            .Textbox("Row label", "label")
                                .Hint("Label for this row.")
                            .IntegerInput("Amount", "amount")
                                .Range(0, 999)
                                .Default(10)
                        )
                    )
                    .Toggle("Premium mode (inverted visibility)", "premium_mode")
                        .Hint("When ON, the block below is HIDDEN (inverted).")
                    .WithVisibility("premium_mode", true, inner => inner
                        .Intro("Visible only when Premium mode is OFF.")
                        .Textbox("Free tier setting", "free_tier_setting")
                            .Hint("This shows when premium_mode toggle is OFF.")
                            .Default("default for free")
                    )
                )
                .Section("PillInput & callbacks", "Pills", p => p
                    .Intro("PillInput with WithSectionsPanel, OnPillAdded, OnPillRemoved. Add items; each gets a config panel.")
                    .PillInput("Test items", "test_items")
                        .Hint("Add items (press Enter or Add). Each item gets a section with toggle and slider.")
                        .WithSectionsPanel((sections, tab, ctx) =>
                        {
                            tab.Children.Add(sections);
                            var items = ctx.GetValue<string[]>("test_items") ?? Array.Empty<string>();
                            foreach (var item in items)
                            {
                                var wrapper = new StackPanel();
                                sections.Children.Add(wrapper);
                                pillPanels[item] = wrapper;
                                AddPillSection(wrapper, item, ctx);
                            }
                        })
                        .OnPillAdded((item, sections, ctx) =>
                        {
                            var wrapper = new StackPanel();
                            sections.Children.Add(wrapper);
                            pillPanels[item] = wrapper;
                            AddPillSection(wrapper, item, ctx);
                        })
                        .OnPillRemoved((item, sections, ctx) =>
                        {
                            if (pillPanels.TryGetValue(item, out var wrapper))
                            {
                                sections.Children.Remove(wrapper);
                                pillPanels.Remove(item);
                                ctx.RemoveSettingsKeys(item + "_enabled", item + "_value");
                            }
                        })
                )
                .LogExistingSettings()
                .Show();

            WriteLog("=== Action completed - UI should now be visible ===");
        }
        catch (Exception ex)
        {
            WriteLog($"=== ERROR: {ex.GetType().Name}: {ex.Message} ===");
            WriteLog(ex.StackTrace ?? "");
        }

        return true;
    }
}
