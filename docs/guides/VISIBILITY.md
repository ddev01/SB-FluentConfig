# Visibility

Show or hide controls based on other settings — single control or whole blocks.

Runnable example: [examples/tutorial/04_ConditionalVisibility.cs](../../examples/tutorial/04_ConditionalVisibility.cs).
Single-control `ShowWhen`: [examples/tutorial/03_DropdownAndButtons.cs](../../examples/tutorial/03_DropdownAndButtons.cs).

Public API: sugar overloads (`ShowWhen("key")`, `WithVisibilityWhenOff`, …) plus `When` condition objects. There is no public trailing `inverted:` bool. The **wire** still carries `inverted` on `VisibilityCondition` (how Off/Neq are encoded).

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
// same: .ShowWhen(When.Eq("mode", "loud")) / .ShowWhen(When.Neq("mode", "free"))
```

Wire: `{ "saveKey": "mode", "equals": "loud" }`. Invert with `.ShowWhenNot` or `When.Neq` (`inverted: true` on the wire). An optional `int` overload matches dropdown `"1"` to `1`.

Leaf show-when-off (boolean off, not string `"true"`):

```csharp
.Textbox("Free note", "free_note").ShowWhen(When.Off("premium_mode"))
```

`ShowWhenNot(key, "true")` does **not** invert a boolean toggle — the protocol compares booleans strictly. Use `When.Off(key)`.

Comparator form (numeric driver):

```csharp
.IntegerInput("Bonus", "bonus")
    .ShowWhen("max_count", Comparator.GreaterOrEqual, 5)
// same: .ShowWhen(When.Cmp("max_count", Comparator.GreaterOrEqual, 5))
```

| `Comparator` | Wire |
|--------------|------|
| `GreaterOrEqual` | `gte` |
| `LessOrEqual` | `lte` |
| `GreaterThan` | `gt` |
| `LessThan` | `lt` |

Do not use nested comparators to fake equality. Do not add `Comparator.Equal`.

## Block — `WithVisibility` / `WithVisibilityWhenOff` / `When`

```csharp
.Toggle("Show extras", "show_extra")
.WithVisibility("show_extra", inner => inner
    .Textbox("Extra", "extra_value"))

.Toggle("Premium", "premium_mode")
.WithVisibilityWhenOff("premium_mode", inner => inner
    .Textbox("Free tier", "free_tier_setting"))
// equivalent: .WithVisibility(When.Off("premium_mode"), inner => …)
```

Value-equals block (flat chrome — no extra left rail; a nested `Grid` is unchanged):

```csharp
.WithVisibility("mode", "quiet", inner => inner
    .Grid("grid-cols-2 items-center", g => g
        .Textbox("Whisper prefix", "whisper_prefix")
        .Textbox("Whisper suffix", "whisper_suffix")))
// equivalent: .WithVisibility(When.Eq("mode", "quiet"), inner => …)

.WithVisibilityWhenNot("mode", "free", inner => inner
    .Textbox("Paid-only", "paid_only"))
```

AND is nested groups via `When.All` (no OR on the wire). `ShowWhen(When.All(...))` throws — AND is not a leaf condition; use `WithVisibility`:

```csharp
.WithVisibility(When.All(When.On("show_extra"), When.Neq("mode", "quiet")), inner => inner
    .Textbox("Combo", "combo_note"))
```

Rare: keep a rail on a value gate with `VisibilityChrome.Indented` as the last argument (not a `bool`).

## Notes

- Visibility is **client-side only** — toggling a driver does not remount the window.
- Nested layout (`Grid`, `Row`, `RepeatFor`) works inside visibility blocks.
- Prefer `.ShowWhen` for one control; use `WithVisibility` when a group should appear/disappear together.
- Pair-value dropdowns gate on the **id** (`valueSaveKey` / `defaultByValue`), including when bootstrap `options` are still empty.

## See also

- [LAYOUT.md](LAYOUT.md) — comparator + RepeatFor recipes
- [BREAKING.md](BREAKING.md) — 0.2 `When` / `inverted:` break
- [CONTROLS.md](CONTROLS.md)
