# PillInput (schema nesting)

PillInput lets users add named items; each item gets nested controls from an `ItemTemplate`.

Runnable example: [examples/tutorial/09_PillsAndNestedItems.cs](../../examples/tutorial/09_PillsAndNestedItems.cs).

## Basic usage

```csharp
.PillInput("Items", "items")
    .ItemTemplate(pb => pb
        .Title("Item: {name}")
        .Toggle("Enabled", "{name}_enabled").Default(true)
        .Slider("Value", "{name}_value").Range(0, 100).Default(50))
    .OnPillRemoved((item, ctx) =>
    {
        ctx.RemoveSettingsKeys(item + "_enabled", item + "_value");
    })
```

`.WithItemTemplate(...)` is an alias for `.ItemTemplate(...)`.

## Rules

- Nested UI comes from the **item template only** — do not use WPF `Panel` / `StackPanel` types in callbacks.
- Prefer `{name}` placeholders in nested saveKeys so each pill gets unique keys.
- `OnPillAdded` / `OnPillRemoved` receive `(itemName, CallbackContext)` for host-side side effects (e.g. cleaning up nested keys).

## See also

- Wire shapes: [../../FluentConfig/PROTOCOL.md](../../FluentConfig/PROTOCOL.md)
- [DIALOGS_AND_RUNTIME_VALUES.md](DIALOGS_AND_RUNTIME_VALUES.md) — `CallbackContext.GetValue`
- [examples/reference/FullControlShowcase.cs](../../examples/reference/FullControlShowcase.cs)
