// Tutorial 07 — Refreshable and pair-value dropdowns
// Dropdown.Refresh() to reload options, and WithPairValue to store display name + ID.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/CONTROLS.md.
//
// Previous: 06_DialogsAndFeedback.cs · Next: 08_AdvancedControls.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Tutorial 07 Dropdown Refresh And Pairs", "1.0"))
            return true;

        var options = new[] { "Option A", "Option B", "Option C" };

        FluentConfigUi.Create(CPH, "Tutorial 07 Dropdown Refresh And Pairs", "1.0")
            .Section("Dropdowns", "Dropdowns", d => d
                .Intro("Dropdown with Refresh; pair-value dropdown (display name + stored ID).")
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
                    }))
            .Show();

        return true;
    }
}
