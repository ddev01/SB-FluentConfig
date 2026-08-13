// Tutorial 02 — Pages (Sections)
// Multiple Section tabs — each Section becomes a page in the settings window.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md for reference details.
//
// Previous: 01_BasicControls.cs · Next: 03_DropdownAndButtons.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 02 Pages", "1.0", ui => ui
            .Section("General", "General", s => s
                .Intro("First page — general options.")
                .Toggle("Enable feature", "enabled")
                    .Hint("Master switch.")
                    .Default(true)
                .Textbox("Display name", "display_name")
                    .Hint("Shown in notifications.")
                    .Default("My Extension"))
            .Section("Advanced", "Advanced", s => s
                .Intro("Second page — optional extras.")
                .Slider("Retry count", "retries")
                    .Hint("How many times to retry on failure.")
                    .Range(0, 10)
                    .Default(3)
                .Textbox("Notes", "notes")
                    .Hint("Free-form notes (not used by the runtime).")
                    .Default("")));

        return true;
    }
}
