// Tutorial 04 — Conditional visibility blocks
// When / WithVisibility / WithVisibilityWhenOff — show or hide groups together.
// Public inverted: is gone. Sugar overloads still work; When.On/Off/Eq/Neq/All is the full API.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/VISIBILITY.md.
//
// Previous: 03_DropdownAndButtons.cs · Next: 05_LayoutAndRepeatFor.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "tutorial_04", "Tutorial 04 Conditional Visibility", "1.0", ui => ui
            .Section("Visibility", "Visibility", s => s
                .Intro(
                    "Block-level visibility.\n\n"
                    + "- **WithVisibility** — children show when the toggle is ON (`When.On`)\n"
                    + "- **WithVisibilityWhenOff** / `When.Off` — children show when the toggle is OFF\n"
                    + "- **ShowWhen(key, value)** / `When.Eq` — show when a saved value equals that value\n"
                    + "- **When.All** — AND (nested groups). There is no OR.\n\n"
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
                // Leaf show-when-off (When.Off). ShowWhenNot(key, "true") would NOT invert a boolean.
                .Textbox("Also hidden when premium is on", "free_leaf")
                    .Hint("ShowWhen(When.Off(...)) — boolean off, not string \"true\".")
                    .ShowWhen(When.Off("premium_mode"))
                .Dropdown("Mode", "mode")
                    .Hint("quiet / normal / loud — fields below depend on this value.")
                    .Options(new[] { "quiet", "normal", "loud" })
                    .DefaultByValue("normal")
                .Textbox("Shout text", "shout_text")
                    .Hint("Only when mode is loud.")
                    .ShowWhen("mode", "loud")
                .WithVisibility(When.Eq("mode", "quiet"), inner => inner
                    .Grid("grid-cols-2 items-center", g => g
                        .Textbox("Whisper prefix", "whisper_prefix")
                        .Textbox("Whisper suffix", "whisper_suffix")
                    )
                )
                .WithVisibility(When.All(When.On("show_extra_options"), When.Neq("mode", "quiet")), inner => inner
                    .Textbox("Extras except quiet", "combo_note")
                        .Hint("When.All → nested groups. Visible when extras are on and mode is not quiet.")
                )));

        return true;
    }
}
