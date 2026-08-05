// Streamer.bot C# action – mirrors the localhost:5173 mock bridge document.
// Same sections (General / Inputs / Lists), labels, saveKeys, and defaults as
// FluentConfig/web/src/bridge/mockDocument.ts so you can compare browser vs WebView2.
//
// Setup: Execute C# Method + Run on UI thread.
// Refs: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/REFERENCES.md.
//
// Note: the mock's update-notice banner is not included — that only appears when
// WithUpdateCheck finds a newer GitHub release (see UpdaterExample.cs).

using FluentConfig;
using System;
using System.Threading;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        FluentConfigUi.ShowOrFocus(CPH, "FluentConfig Dev Preview", "0.1.0-dev", ui => ui
            .Section("General", "General", g => g
                .Intro(
                    "# Dev preview\n\n"
                    + "Mirror of **localhost:5173** — every control type below.\n\n"
                    + "- Rich **bold** and `{code}` samples\n"
                    + "- Save persists to Streamer.bot globals\n\n"
                    + "See [FluentConfig docs](https://example.test/fluentconfig) for more.")
                .ConnectionStatus("Mock service", "connected", "Reconnect", ctx => ctx.Toast("Reconnect clicked"))
                .Title("Basics")
                .Toggle("Enable feature", "feature_enabled")
                    .Hint("Master switch for the sample feature.")
                    .Default(true)
                .Textbox("Display name", "display_name")
                    .Hint("Shown in notifications.")
                    .Default("Demo Stream")
                .Textbox("API token", "api_token")
                    .Hint("Password-style field (fake value only).")
                    .Password()
                    .Default("demo-token")
                .Textbox("Notes", "notes")
                    .Multiline()
                    .Default("Multiline notes for the mock form.")
                .Separator()
                .Toggle("Enable optional limit", "optional_limit_enabled")
                    .Hint("Gates the slider below via ShowWhen.")
                    .Default(false)
                .Slider("Optional limit", "optional_limit")
                    .Range(0, 100)
                    .Default(50)
                    .ShowWhen("optional_limit_enabled")
                .WithVisibilityWhenOff("feature_enabled", off => off
                    .Intro("Visible only when “Enable feature” is off (inverted WithVisibility).")
                    .Textbox("Fallback message", "fallback_message")
                        .Default("Feature is disabled.")
                )
            )
            .Section("Inputs", "Inputs", i => i
                .NumberInput("Rate", "rate_value")
                    .WithStepper()
                    .Range(0.1, 10.0)
                    .Step(0.1)
                    .Default(1.0)
                .IntegerInput("Retry count", "retry_count")
                    .WithStepper()
                    .Range(0, 20)
                    .Default(3)
                .Slider("Volume", "volume")
                    .Range(0, 100)
                    .Default(75)
                .ColorPicker("Accent color", "accent_color")
                    .Default("#3b82f6")
                .DurationInput("Cooldown", "cooldown")
                    .WithPermanentOption(true)
                    .Default("30seconds")
                .Filepath("Export path", "export_filepath")
                    .Hint("Pick an export path via the file browser.")
                    .Default("")
                .Dropdown("Channel reward", "reward_display")
                    .WithPairValue("reward_id")
                    .Options(new[]
                    {
                        ("reward-1", "Highlight Message"),
                        ("reward-2", "Hydrate Reminder"),
                        ("reward-3", "Song Request"),
                    })
                    .Refresh(() => new[]
                    {
                        ("reward-1", "Highlight Message"),
                        ("reward-2", "Hydrate Reminder"),
                        ("reward-3", "Song Request"),
                        ("reward-4", "Refreshed Reward"),
                    })
                    .DefaultByValue("reward-2")
                .Dropdown("Simple choice", "simple_choice")
                    .Options(new[] { "Option A", "Option B", "Option C" })
                    .DefaultIndex(0)
                .Toggle("Exclusive modes", "exclusive_modes")
                    .Hint("Multi-select exclusive toggles (max 2).")
                    .WithExclusive(new[] { "Quiet", "Party", "Focus", "AFK" })
                    .MaxSelected(2)
                    .DefaultIndices(new[] { 0 })
                .Button("Run progress demo")
                    .Hint("Triggers a fake host progress sequence.")
                    .Text("Run progress demo")
                    .Color("#2563eb")
                    .OnClick(ui =>
                    {
                        const int total = 10;
                        var progress = ui.ShowProgressWindow(
                            "Progress demo",
                            "Simulating work…",
                            "Progress",
                            total);
                        if (progress == null)
                            return;

                        Task.Run(() =>
                        {
                            for (int step = 0; step <= total; step++)
                            {
                                Thread.Sleep(150);
                                progress.Report(step);
                            }
                            progress.Close();
                            ui.Toast("Progress demo finished.");
                        });
                    })
                .Button("Show toast")
                    .Text("Show toast")
                    .Color("#059669")
                    .OnClick(ui => ui.Toast("Mock toast from Streamer.bot host."))
            )
            .Section("Lists", "Lists", l => l
                .DynamicTextboxes("Allowed users", "allowed_users")
                    .Hint("Add/remove string entries.")
                    .Preset(new[] { "viewer_one", "viewer_two" })
                    .AllowDuplicates(false)
                .PillInput("Test items", "test_items")
                    .Hint("Pills with nested schema children (no Panel leak).")
                    .WithItemTemplate(item => item
                        .Title("Item: {name}")
                        .Toggle("Enabled", "{name}_enabled")
                            .Default(true)
                        .Slider("Value", "{name}_value")
                            .Range(0, 100)
                            .Default(50)
                    )
                    .OnPillRemoved((itemName, ctx) =>
                    {
                        ctx.RemoveSettingsKeys(itemName + "_enabled", itemName + "_value");
                    })
                .WithRepeatableRows("command_rows", row => row
                    .Textbox("Command", "command")
                        .Default("")
                    .Textbox("Reply", "reply")
                        .Default("")
                    .Toggle("Enabled", "enabled")
                        .Default(true)
                )
            )
            .LogExistingSettings()
        );

        return true;
    }
}
