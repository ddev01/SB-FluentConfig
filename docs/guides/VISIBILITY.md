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

Value-equals (dropdown or any saved value — not a numeric comparator):

```csharp
.Dropdown("Mode", "mode").Options(new[] { "quiet", "normal", "loud" })
.Textbox("Shout", "shout_text").ShowWhen("mode", "loud")
.Textbox("Not free", "paid_note").ShowWhenNot("mode", "free")
```

Wire: `{ "saveKey": "mode", "equals": "loud" }`. Invert with `.ShowWhenNot` (`inverted: true`). An optional `int` overload matches dropdown `"1"` to `1`.

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

Do not use nested comparators to fake equality. Do not add `Comparator.Equal`.

## Block — `WithVisibility` / `WithVisibilityWhenOff` / `WithVisibilityWhenNot`

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

Value-equals block (flat chrome — no extra left rail; a nested `Grid` is unchanged):

```csharp
.WithVisibility("mode", "quiet", inner => inner
    .Grid("grid-cols-2 items-center", g => g
        .Textbox("Whisper prefix", "whisper_prefix")
        .Textbox("Whisper suffix", "whisper_suffix")))

.WithVisibilityWhenNot("mode", "free", inner => inner
    .Textbox("Paid-only", "paid_only"))
```

Rare: keep a rail on a value gate with `VisibilityChrome.Indented` as the last argument (not a `bool`).

## Notes

- Visibility is **client-side only** — toggling a driver does not remount the window.
- Nested layout (`Grid`, `Row`, `RepeatFor`) works inside visibility blocks.
- Prefer `.ShowWhen` for one control; use `WithVisibility` when a group should appear/disappear together.

## See also

- [LAYOUT.md](LAYOUT.md) — comparator + RepeatFor recipes
- [CONTROLS.md](CONTROLS.md)
