// Tutorial 09 — PillInput and nested item templates
// PillInput with ItemTemplate — each pill gets nested schema controls (no WPF Panel).
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/PILLS.md.
//
// Previous: 08_AdvancedControls.cs · Next: 10_ReadingSavedSettings.cs

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 09 Pills And Nested Items", "1.0", ui => ui
            .Section("Pills", "Pills", p => p
                .Intro(
                    "PillInput with a schema itemTemplate.\n\n"
                    + "Add items (press Enter or Add). Each item gets nested toggle + slider.\n"
                    + "Do not use WPF Panel types in callbacks — nested UI comes from ItemTemplate only.")
                .PillInput("Test items", "test_items")
                    .Hint("Add items. Each item gets nested toggle + slider.")
                    .WithItemTemplate(item => item
                        .Title("Item: {name}")
                        .Toggle("Enabled", "{name}_enabled")
                            .Hint("Enable this item.")
                            .Default(true)
                        .Slider("Value", "{name}_value")
                            .Range(0, 100)
                            .Default(50)
                    )
                    .OnPillAdded((itemName, ctx) =>
                    {
                        // Optional host-side side effects when a pill is added.
                    })
                    .OnPillRemoved((itemName, ctx) =>
                    {
                        ctx.RemoveSettingsKeys(itemName + "_enabled", itemName + "_value");
                    })));

        return true;
    }
}
