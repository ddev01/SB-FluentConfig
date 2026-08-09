# Layout, sizing, and dynamic field count

Authoring guide for FluentConfig’s client-side layout APIs: **Grid**, **Row**, **Size**, **Span**, comparator visibility, and **RepeatFor**.

Wire shapes: [../FluentConfig/PROTOCOL.md](../FluentConfig/PROTOCOL.md). Runnable example: [`examples/menu/DynamicCountLayoutExample.cs`](../examples/menu/DynamicCountLayoutExample.cs).

## Mental model

| Host API (Tailwind-flavored) | What the web actually gets |
|------------------------------|----------------------------|
| `Grid("grid-cols-2 gap-3 items-center", …)` | `group` node with `grid: { mode, columns, gap, align }` |
| `Row("gap-3 items-center", …)` | same, `mode: "row"` |
| `.Size("fit min-w-20 max-w-96")` / `.Span(2)` | per-node `layout` with host-resolved CSS values |
| `RepeatFor("max_count", …)` | real controls + `group`s gated by `visibility.operator: "gte"` |

Tokens are parsed on the host into structured fields / final CSS strings. The Svelte UI applies **inline CSS** — never literal Tailwind class strings from JSON (Tailwind v4 has no safelist for dynamic names).

All of this is **client-side only**: changing a driver value or layout does not remount the window or round-trip to the host.

---

## Spacing scale (defaults)

Gap and bare `min-w-N` / `max-w-N` use the Tailwind spacing scale: **`N → N × 4` pixels**.

| Token | Pixels | Role in FluentConfig UI |
|-------|--------|-------------------------|
| `gap-1` / `space-y-1` | 4px | Default vertical field stack (`FormSection`, `FieldShell`) |
| `gap-2` | 8px | Inside a control (input + button, chips) |
| `gap-3` | 12px | **Default for `Grid` / `Row`** — sibling controls |
| `gap-4` | 16px | Page/overlay padding; use only when you want a roomier block |

**Default gap is `3` (12px)** when you omit `gap-N`. Prefer keeping the default; pass `gap-4` only for deliberate breathing room.

---

## Grid

Equal-width CSS columns. Spec string is space-separated; token order does not matter.

```csharp
.Grid("grid-cols-2 gap-3 items-center", g => g
    .Toggle("Announce", "announce").Default(true)
    .Toggle("Reset daily", "reset_daily").Default(false)
    .Textbox("Prefix", "announce_prefix")
        .Default("Winners:")
        .Span(2)
)
```

| Token | Required? | Meaning |
|-------|-----------|---------|
| `grid-cols-N` | Recommended | `N` equal columns. If omitted, renderer uses 1 column. |
| `gap-N` | No | Default **3** (12px). |
| `items-start\|center\|end\|stretch\|baseline` | No | CSS `align-items` |

Unrecognized tokens throw at build time. Empty Grid spec throws (almost always an author mistake).

Plain `WithVisibility` groups (no `grid`) keep the indented left-border stack; they are not grids.

---

## Row

Flex wrap row — children sit in a horizontal flow and wrap as needed. Same token grammar as Grid, minus `grid-cols-N`.

```csharp
.Row("gap-2 items-center", r => r
    .Textbox("Name", "name").Size("grow")
    .Button("Go").Text("Run").Size("shrink-0")
)

// Convenience: defaults (gap 3, no align)
.Row(r => r.Toggle("A", "a").Toggle("B", "b"))
```

| Token | Meaning |
|-------|---------|
| `gap-N` | Default **3** |
| `items-start\|center\|end\|stretch\|baseline` | CSS `align-items` |

`grid-cols-N` on a Row throws. Prefer **Grid** when you want aligned columns.

---

## Size (per control)

Compound, space-separated sizing — same style as Grid’s spec string. Host resolves tokens to final CSS before serializing.

```csharp
.IntegerInput("How many first chatters", "max_count")
    .Range(1, 10)
    .Default(3)
    .Size("fit min-w-20 max-w-40")
```

### Width tokens

| Token | Resolved CSS |
|-------|--------------|
| `fit` | `fit-content` |
| `full` | `100%` |
| `1/2`, `1/3`, `2/3`, `1/4`, `3/4` | percentages |
| `120px`, `5rem` | literal units |

### Min / max width

| Token | Resolved CSS |
|-------|--------------|
| `min-w-20` / `max-w-96` | Tailwind scale → `{N×4}px` (e.g. 80px / 384px) |
| `min-w-80px` / `max-w-5rem` | literal escape hatch |
| `min-w-fit` / `max-w-1/2` / … | same keyword/fraction set as width |

### Flex (Row only)

| Token | Effect |
|-------|--------|
| `grow` / `grow-0` | `flex-grow: 1` / `0` |
| `shrink` / `shrink-0` | `flex-shrink: 1` / `0` |

`grow` / `shrink-*` **throw at build time** if the control is not directly inside a `Row(...)` (plain stack or Grid).

**Notes**

- `fit` shrinks the **input row only** (label/hint stay full width via `FieldShell`). Min/max with `fit` apply to that same input row.
- Non-`fit` width / min / max apply to the control’s layout wrapper.
- Anything unrecognized throws at build time.

---

## Span (per control, Grid only)

How many grid columns the control occupies.

```csharp
.Grid("grid-cols-2 gap-3", g => g
    .Toggle("Left", "left")
    .Toggle("Right", "right")
    .Textbox("Full width row", "full_row").Span(2)
)
```

Outside a Grid, Span has little visual effect. Prefer Size in a plain stack.

---

## Size vs Span (pick one job)

| Goal | Use |
|------|-----|
| Compact control in a vertical list | `.Size("fit")` or `.Size("1/2")` |
| Compact with bounds | `.Size("fit min-w-20 max-w-48")` |
| Side-by-side aligned columns | `.Grid("grid-cols-2 …")` |
| One control across all columns | `.Span(columns)` |
| Input + button strip | `.Row("items-center", r => r.….Size("grow")….Size("shrink-0"))` |

---

## Comparator visibility

Numeric gates on any control or group (additive to toggle `ShowWhen` / `WithVisibility`).

```csharp
.IntegerInput("Bonus", "bonus")
    .ShowWhen("max_count", Comparator.GreaterOrEqual, 5)

.WithVisibility("level", Comparator.GreaterThan, 0, inner => inner
    .Textbox("Detail", "detail"))
```

| `Comparator` | Wire `operator` |
|--------------|-----------------|
| `GreaterOrEqual` | `gte` |
| `LessOrEqual` | `lte` |
| `GreaterThan` | `gt` |
| `LessThan` | `lt` |

Legacy `.ShowWhen("toggle_key")` (equals `true`) is unchanged. Missing/NaN driver values hide comparator-gated nodes (safe default).

---

## RepeatFor (dynamic field count)

Materializes one set of controls per index up to a max, and shows/hides extras with live `gte` gates. No reopen, no value loss when the count shrinks.

```csharp
.Section("Settings", "Settings", s => s
    .IntegerInput("Max places", "max_count")
        .Range(1, 10)
        .Default(3)
        .Size("fit")
)
.Section("Points", "Points", s => s
    .RepeatFor("max_count", (row, i) => row
        .IntegerInput($"Place #{i} points", $"points_{i}")
            .Range(0, 100000)
            .Default(i == 1 ? 1000 : 0)
            .Size("fit")
    )
)
```

| Behavior | Detail |
|----------|--------|
| Max | From driver’s `.Range(min, max)` if `max:` omitted; else explicit `max:` |
| Min | Indices `<=` driver’s min are **always visible** (no gate) |
| Higher indices | Wrapped in a `group` with `ShowWhen(driver, gte, i)` |
| Driver location | May live in an earlier section — range is registered when that control flushes |
| Missing range + no `max:` | Throws a clear build-time error |
| Values | Pre-built saveKeys stay in the values blob when hidden |

`startIndex` defaults to `1`. Pass `max:` to cap below the driver’s declared max.

**Not the same as `WithRepeatableRows`:** that is an author-add/remove list of row objects. `RepeatFor` is a fixed index range driven by an integer setting.

---

## Recipes

**Compact count + side-by-side toggles**

```csharp
.IntegerInput("How many", "max_count").Range(1, 10).Default(3).Size("fit min-w-16")
.Grid("grid-cols-2 items-center", g => g
    .Toggle("Exclude broadcaster", "exclude_broadcaster").Default(true)
    .Toggle("Exclude bots", "exclude_bot").Default(true)
)
```

**Input + action in a Row**

```csharp
.Row("gap-2 items-center", r => r
    .Textbox("Path", "path").Size("grow")
    .Button("Browse").Text("…").Size("shrink-0")
)
```

**Points per place (live)**

```csharp
.WithVisibility("award_points", inner => inner
    .Textbox("Points variable", "points_variable").Default("points").Size("1/2")
    .RepeatFor("max_count", (row, i) => row
        .IntegerInput($"{i} place points", $"points_{i}")
            .Range(0, 10000000)
            .Size("fit")
    )
)
```

---

## What layout does *not* do

- Change label/input internal chrome beyond the `fit` FieldShell special-case.
- Accept arbitrary Tailwind class strings (`w-auto`, `col-span-full`, etc.) — curated tokens only.
- Remount the window or call the host when visibility/layout updates.
- Replace `WithRepeatableRows` for user-managed lists.

Protocol-level details and JSON examples: [PROTOCOL.md — Layout / Visibility](../FluentConfig/PROTOCOL.md).
