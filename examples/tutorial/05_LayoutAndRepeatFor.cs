// Tutorial 05 — Layout and dynamic field count
// Grid / Row / Size (col-span, grow, w-fit) + RepeatFor (integer-driven field count).
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/LAYOUT.md.
//
// Previous: 04_ConditionalVisibility.cs · Next: 06_DialogsAndFeedback.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 05 Layout And RepeatFor", "1.0", ui => ui
            .Section("Settings", "Settings", s => s
                .Intro("Grid columns, a flex Row, and RepeatFor for a live field count.")
                .IntegerInput("Max places", "max_count")
                    .Hint("How many place/points fields to show (1–10).")
                    .Range(1, 10)
                    .Default(3)
                    .Size("w-fit")
                .Grid("grid-cols-2 gap-3", g => g
                    .Toggle("Announce winners", "announce")
                        .Hint("Post a chat message when places fill.")
                        .Default(true)
                    .Toggle("Reset daily", "reset_daily")
                        .Hint("Clear places at midnight.")
                        .Default(false)
                    .Textbox("Announcement prefix", "announce_prefix")
                        .Hint("Shown before the winner list.")
                        .Default("Winners:")
                        .Size("col-span-2")
                )
                .Row("gap-2 items-center", r => r
                    .Textbox("Command prefix", "command_prefix")
                        .Hint("Grows to fill leftover space in the row.")
                        .Default("!")
                        .Size("grow")
                    .Toggle("Require prefix", "require_prefix")
                        .Hint("Does not shrink when the textbox grows.")
                        .Default(true)
                        .Size("shrink-0")
                )
            )
            .Section("Points", "Points", s => s
                .Intro("Points awarded per place. Extra fields appear as you raise Max places.")
                .RepeatFor("max_count", (row, i) => row
                    .IntegerInput($"Points for place #{i}", $"points_{i}")
                        .Hint($"Awarded to the {Ordinal(i)} chatter.")
                        .Range(0, 100000)
                        .Default(i == 1 ? 100 : i == 2 ? 50 : 25)
                        .Size("w-fit")
                )
            ));

        return true;
    }

    private static string Ordinal(int n)
    {
        int mod100 = n % 100;
        if (mod100 >= 11 && mod100 <= 13) return n + "th";
        switch (n % 10)
        {
            case 1: return n + "st";
            case 2: return n + "nd";
            case 3: return n + "rd";
            default: return n + "th";
        }
    }
}
