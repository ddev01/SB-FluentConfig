# Visibility

Show or hide controls based on other settings — single control or whole blocks.

Runnable example: [examples/tutorial/04_ConditionalVisibility.cs](../../examples/tutorial/04_ConditionalVisibility.cs).
Single-control `ShowWhen`: [examples/tutorial/03_DropdownAndButtons.cs](../../examples/tutorial/03_DropdownAndButtons.cs).

## Single control — `ShowWhen`

```csharp
.Toggle("Show advanced", "show_advanced").Default(false)
.Slider("Advanced offset", "advanced_offset")
    .Range(-50, 50)
    .Default(0)
    .ShowWhen("show_advanced")
```

Comparator form (numeric driver):

```csharp
.IntegerInput("Bonus", "bonus")
    .ShowWhen("max_count", Comparator.GreaterOrEqual, 5)
```

| `Comparator` | Wire |
|--------------|------|
| `GreaterOrEqual` | `gte` |
| `LessOrEqual` | `lte` |
| `GreaterThan` | `gt` |
| `LessThan` | `lt` |

## Block — `WithVisibility` / `WithVisibilityWhenOff`

```csharp
.Toggle("Show extras", "show_extra")
.WithVisibility("show_extra", inner => inner
    .Textbox("Extra", "extra_value"))

.Toggle("Premium", "premium_mode")
.WithVisibilityWhenOff("premium_mode", inner => inner
    .Textbox("Free tier", "free_tier_setting"))
// equivalent: .WithVisibility("premium_mode", inner => ..., inverted: true)
```

Prefer the named form (`.WithVisibilityWhenOff` or `inverted: true`) over a bare positional bool.

## Notes

- Visibility is **client-side only** — toggling a driver does not remount the window.
- Nested layout (`Grid`, `Row`, `RepeatFor`) works inside visibility blocks.
- Prefer `.ShowWhen` for one control; use `WithVisibility` when a group should appear/disappear together.

## See also

- [LAYOUT.md](LAYOUT.md) — comparator + RepeatFor recipes
- [CONTROLS.md](CONTROLS.md)
