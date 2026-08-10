// Tutorial 08 — Advanced controls
// WithExclusive (single / multi-select), DynamicTextboxes, WithRepeatableRows.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/CONTROLS.md.
//
// Previous: 07_DropdownRefreshAndPairs.cs · Next: 09_PillsAndNestedItems.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Tutorial 08 Advanced Controls", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "Tutorial 08 Advanced Controls", "1.0")
            .Section("Advanced", "Advanced", a => a
                .Intro("Exclusive toggles, dynamic textbox list, and repeatable rows.")
                .Toggle("Mode", "mode_index")
                    .Hint("Only one mode can be active (single-select).")
                    .WithExclusive(new[] { "Mode 1", "Mode 2", "Mode 3" })
                    .DefaultIndex(0)
                .Toggle("Features (multi-select)", "features")
                    .Hint("Select up to 2 features; uses MaxSelected and DefaultIndices.")
                    .WithExclusive(new[] { "Feature A", "Feature B", "Feature C" })
                    .MaxSelected(2)
                    .DefaultIndices(new[] { 0, 1 })
                .DynamicTextboxes("Custom list", "custom_list")
                    .Hint("Add/remove textboxes; persisted as an array.")
                    .Preset(new[] { "Item 1", "Item 2" })
                .Separator()
                .Intro("Repeatable rows — user-managed list of structured fields.")
                .WithRepeatableRows("extra_rows", row => row
                    .Textbox("Row label", "label")
                        .Hint("Label for this row.")
                    .IntegerInput("Amount", "amount")
                        .Range(0, 999)
                        .Default(10)
                ))
            .Show();

        return true;
    }
}
