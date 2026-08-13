# Controls

Concise catalog of FluentConfig controls and their key chained options.
Wire shapes: [../../FluentConfig/PROTOCOL.md](../../FluentConfig/PROTOCOL.md).

Runnable intro: [examples/tutorial/01_BasicControls.cs](../../examples/tutorial/01_BasicControls.cs).

## Structure

| API | Role |
|-----|------|
| `Fc.Open(cph, title, version, build)` | Preferred menu entry (focus or create) |
| `Section(label, id, build)` | Tab / page |
| `Intro(markdown)` | Rich markdown blurb at the top of a section |
| `Title(text)` | In-section heading |
| `Separator()` | Visual divider |
| `ConnectionStatus(...)` | Status row with optional action button |

## Input controls

| Control | Common options | Tutorial |
|---------|----------------|----------|
| `Toggle` | `.Hint`, `.Default`, `.ShowWhen`, `.WithExclusive`, `.MaxSelected` | 01, 08 |
| `Textbox` | `.Hint`, `.Default`, `.Password()`, `.Multiline()` | 01 |
| `Slider` | `.Range`, `.Default`, `.Step`, `.ShowWhen` | 01 |
| `Dropdown` | `.Options`, `.DefaultIndex` / `.DefaultByValue`, `.Refresh`, `.WithPairValue` | 03, 07 |
| `IntegerInput` | `.Range`, `.Default`, `.WithStepper()`, `.Size` | 05 |
| `NumberInput` | `.Range`, `.Step`, `.Default`, `.WithStepper()` | reference |
| `DurationInput` | `.Default("30seconds")`, `.WithPermanentOption` | reference |
| `Filepath` | `.Hint`, `.Default` | reference |
| `ColorPicker` | `.Default("#714bfd")` | reference |
| `DynamicTextboxes` | `.Preset`, `.AllowDuplicates` | 08 |
| `PillInput` | `.WithItemTemplate` / `.ItemTemplate`, `.OnPillAdded`, `.OnPillRemoved` | 09 |
| `Button` | `.Text`, `.Color`, `.OnClick` | 03, 06 |

## Layout helpers

| API | Role | Guide |
|-----|------|-------|
| `Grid(spec, build)` | CSS grid columns | [LAYOUT.md](LAYOUT.md) |
| `Row(spec, build)` | Flex row | [LAYOUT.md](LAYOUT.md) |
| `.Size(tokens)` | Per-control width / span / grow | [LAYOUT.md](LAYOUT.md) |
| `RepeatFor(driverKey, build)` | Integer-driven field count | [LAYOUT.md](LAYOUT.md) |
| `WithRepeatableRows(key, build)` | User-managed list of structured rows | 08 |
| `WithVisibility` / `WithVisibilityWhenOff` | Block-level show/hide | [VISIBILITY.md](VISIBILITY.md) |

## Chaining model

```csharp
.Toggle("Enable feature", "enabled")
    .Hint("Turn this on to enable the feature.")
    .Default(true)
.Slider("Volume", "volume")
    .Range(0, 100)
    .Default(50)
    .ShowWhen("enabled")
```

Each method call adds a control. Options chain onto the control just added. That's the whole model.

## See also

- [VISIBILITY.md](VISIBILITY.md)
- [LAYOUT.md](LAYOUT.md)
- [PILLS.md](PILLS.md)
- [DIALOGS_AND_RUNTIME_VALUES.md](DIALOGS_AND_RUNTIME_VALUES.md) — Button `OnClick`, runtime reads
- [examples/reference/FullControlShowcase.cs](../../examples/reference/FullControlShowcase.cs)
