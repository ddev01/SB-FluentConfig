// Tutorial 01 — Basic controls
// Minimal FluentConfig menu: Toggle, Textbox, Slider.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md for reference details.
//
// Next: 02_Pages.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Tutorial 01 Basic Controls", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "Tutorial 01 Basic Controls", "1.0")
            .Section("Settings", "Settings", s => s
                .Intro("A minimal example with a few basic controls.")
                .Toggle("Enable feature", "enabled")
                    .Hint("Turn this on to enable the feature.")
                    .Default(true)
                .Textbox("Name", "name")
                    .Hint("Enter a display name.")
                    .Default("")
                .Slider("Volume", "volume")
                    .Hint("Adjust volume (0–100).")
                    .Range(0, 100)
                    .Default(50))
            .Show();

        return true;
    }
}
