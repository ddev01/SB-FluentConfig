// Tutorial 03 — Dropdown, ShowWhen, and buttons
// Dropdown, slider, conditional ShowWhen, and a Button with OnClick + Popup.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md for reference details.
//
// Previous: 02_Pages.cs · Next: 04_ConditionalVisibility.cs
// Companion runtime reader: 10_ReadingSavedSettings.cs (reads this menu's saved JSON)

using FluentConfig;
using System;

public class CPHInline
{
    public bool Execute()
    {
        var modes = new[] { "Quiet", "Normal", "Loud" };

        Fc.Open(CPH, "Tutorial 03 Dropdown And Buttons", "1.0", ui => ui
            .Section("Main", "Main", s => s
                .Intro("Demonstrates dropdown, slider, button, and conditional visibility.")
                .Dropdown("Mode", "mode")
                    .Hint("Select output mode.")
                    .Options(modes)
                    .DefaultIndex(1)
                .Slider("Level", "level")
                    .Hint("Output level when enabled.")
                    .Range(0, 100)
                    .Default(75)
                .Toggle("Show advanced", "show_advanced")
                    .Hint("Enable to reveal additional options.")
                    .Default(false)
                .Slider("Advanced offset", "advanced_offset")
                    .Hint("Only visible when advanced is on.")
                    .Range(-50, 50)
                    .Default(0)
                    .ShowWhen("show_advanced")
                .Button("Test action")
                    .Hint("Click to show current values in a popup.")
                    .Text("Show values")
                    .Color("#714bfd")
                    .OnClick(ui =>
                    {
                        string mode = ui.Pending<string>("mode");
                        int level = ui.Pending<int>("level");
                        ui.Popup("Current values", $"Mode: {mode}\nLevel: {level}");
                    })));

        return true;
    }
}
