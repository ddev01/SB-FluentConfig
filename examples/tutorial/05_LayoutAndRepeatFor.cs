// Tutorial 05 — Layout and dynamic field count
// Grid / Size (col-span, w-fit) + RepeatFor (integer-driven field count).
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/LAYOUT.md.
//
// Previous: 04_ConditionalVisibility.cs · Next: 06_DialogsAndFeedback.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Tutorial 05 Layout And RepeatFor", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "Tutorial 05 Layout And RepeatFor", "1.0")
            .Section("Settings", "Settings", s => s
                .Intro("First-Chatters-style points-per-place with a live count and a two-column layout.")
                .IntegerInput("Max places", "max_count")
                    .Hint("How many place/points fields to show (1–10).")
                    .Range(1, 10)
                    .Default(3)
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
            )
            .Show();

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
