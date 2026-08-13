// Tutorial 01 — Basic controls
// Minimal FluentConfig menu: Toggle, Textbox, Slider.
// Fc.Open focuses an existing window with the same title (no AlreadyOpened check).
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md for reference details.
//
// Next: 02_Pages.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 01 Basic Controls", "1.0", ui => ui
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
                    .Default(50)));

        return true;
    }
}
