// Tutorial 07 — Refreshable, pair-value, and searchable dropdowns
// Dropdown.Refresh() to reload options, WithPairValue for display+ID, Searchable/AllowCustom/Multiple.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/CONTROLS.md.
//
// Previous: 06_DialogsAndFeedback.cs · Next: 08_AdvancedControls.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        var options = new[] { "Option A", "Option B", "Option C" };

        Fc.Open(CPH, "Tutorial 07 Dropdown Refresh And Pairs", "1.0", ui => ui
            .Section("Dropdowns", "Dropdowns", d => d
                .Intro(
                    "Dropdown with Refresh; pair-value dropdown (display name + stored ID);\n"
                    + "searchable combobox (type to filter, optional custom value).\n\n"
                    + "Refresh callbacks are author-supplied. There is no built-in timer list helper.")
                .Dropdown("Choice", "dropdown_choice")
                    .Hint("Select an option. Click Refresh to reload the list.")
                    .Options(options)
                    .Refresh(() => new[] { "Option A", "Option B", "Option C", "Option D (refreshed)" })
                    .DefaultIndex(0)
                .Dropdown("Device (pair value)", "device_display")
                    .Hint("Stores display name in device_display, ID in device_id.")
                    .WithPairValue("device_id")
                    .Options(new[] { ("id1", "Device A"), ("id2", "Device B"), ("id3", "Device C") })
                    .DefaultByValue("id2")
                .Combobox("Auto-end timer", "timer_id")
                    .Hint("Type to filter, or paste a custom id. Refresh uses a fake list you supply.")
                    .AllowCustom()
                    .RefreshPairs(() => new[] { ("timer-a", "Timer A"), ("timer-b", "Timer B") })
                    .Default("timer-a")
                .Dropdown("Include groups", "include_groups")
                    .Hint("Pick one or more group names; type a custom name if needed.")
                    .Searchable()
                    .Multiple()
                    .AllowCustom()
                    .Options(new[] { "vip", "sub", "mod" })
                .Button("Show pair values")
                    .Hint("Reads both the display key and the paired ID key.")
                    .Text("Show values")
                    .Color("#714bfd")
                    .OnClick(ui =>
                    {
                        string display = ui.Pending<string>("device_display");
                        string id = ui.Pending<string>("device_id");
                        string choice = ui.Pending<string>("dropdown_choice");
                        ui.Popup("Dropdown values", $"Choice: '{choice}'\nDevice display: '{display}'\nDevice ID: '{id}'");
                    })));

        return true;
    }
}
