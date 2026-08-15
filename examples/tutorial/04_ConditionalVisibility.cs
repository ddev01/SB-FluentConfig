// Tutorial 04 — Conditional visibility blocks
// WithVisibility / WithVisibilityWhenOff — show or hide groups of controls together.
// Value-equals gates for dropdowns: ShowWhen(key, "value") / WithVisibility(key, "value", …).
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/VISIBILITY.md.
//
// Previous: 03_DropdownAndButtons.cs · Next: 05_LayoutAndRepeatFor.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 04 Conditional Visibility", "1.0", ui => ui
            .Section("Visibility", "Visibility", s => s
                .Intro(
                    "Block-level visibility.\n\n"
                    + "- **WithVisibility** — children show when the toggle is ON\n"
                    + "- **WithVisibilityWhenOff** — children show when the toggle is OFF\n"
                    + "- **ShowWhen(key, value)** — show when a dropdown (or any saved value) equals that value\n\n"
                    + "For a single control, prefer `.ShowWhen(\"key\")` (see tutorial 03).")
                .Toggle("Show extra options", "show_extra_options")
                    .Hint("When on, the block below is visible.")
                    .Default(false)
                .WithVisibility("show_extra_options", inner => inner
                    .Textbox("Extra option A", "extra_option_a")
                        .Hint("Only visible when 'Show extra options' is on.")
                    .Slider("Extra option B", "extra_option_b")
                        .Hint("Only visible when 'Show extra options' is on.")
                        .Range(0, 50)
                        .Default(25)
                )
                .Toggle("Premium mode", "premium_mode")
                    .Hint("When ON, the free-tier block below is hidden.")
                    .Default(false)
                .WithVisibilityWhenOff("premium_mode", inner => inner
                    .Intro("Visible only when Premium mode is OFF.")
                    .Textbox("Free tier setting", "free_tier_setting")
                        .Hint("This shows when premium_mode is OFF.")
                        .Default("default for free")
                )
                .Dropdown("Mode", "mode")
                    .Hint("quiet / normal / loud — fields below depend on this value.")
                    .Options(new[] { "quiet", "normal", "loud" })
                    .DefaultByValue("normal")
                .Textbox("Shout text", "shout_text")
                    .Hint("Only when mode is loud.")
                    .ShowWhen("mode", "loud")
                .WithVisibility("mode", "quiet", inner => inner
                    .Grid("grid-cols-2 items-center", g => g
                        .Textbox("Whisper prefix", "whisper_prefix")
                        .Textbox("Whisper suffix", "whisper_suffix")
                    )
                )));

        return true;
    }
}
