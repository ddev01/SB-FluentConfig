# Layout, sizing, and dynamic field count

Authoring guide for FluentConfig’s client-side layout APIs: **Grid**, **Row**, **Size**, comparator visibility, and **RepeatFor**.

Wire shapes: [../../FluentConfig/PROTOCOL.md](../../FluentConfig/PROTOCOL.md). Runnable example: [`examples/tutorial/05_LayoutAndRepeatFor.cs`](../../examples/tutorial/05_LayoutAndRepeatFor.cs).

## Mental model

| Host API (1:1 Tailwind class names) | What the web actually gets |
|-------------------------------------|----------------------------|
| `Grid("grid-cols-2 gap-3 items-center", …)` | `group` with `grid: { mode, columns, gap, align }` |
| `Row("gap-3 items-center", …)` | same, `mode: "row"` |
| `.Size("w-fit min-w-20 col-span-2")` | per-node `layout` with host-resolved CSS |
| `RepeatFor("max_count", …)` | real controls + `group`s gated by `visibility.operator: "gte"` |

Authors write familiar Tailwind classes. The host parses them into structured fields / final CSS strings. The Svelte UI applies **inline CSS** — never literal class strings from JSON (Tailwind v4 has no safelist for dynamic names).

All of this is **client-side only**: changing a driver value or layout does not remount the window or round-trip to the host.

---

## Spacing scale (defaults)

`gap-N`, `min-w-N`, and `max-w-N` use the Tailwind spacing scale: **`N → N × 4` pixels**.

| Token | Pixels | Role in FluentConfig UI |
|-------|--------|-------------------------|
| `gap-1` / `space-y-1` | 4px | Default vertical field stack |
| `gap-2` | 8px | Inside a control (input + button) |
| `gap-3` | 12px | **Default for `Grid` / `Row`** |
| `gap-4` | 16px | Page/overlay padding |

**Default gap is `3` (12px)** when you omit `gap-N`.

---

## Grid

```csharp
.Grid("grid-cols-2 gap-3 items-center", g => g
    .Toggle("Announce", "announce").Default(true)
    .Toggle("Reset daily", "reset_daily").Default(false)
    .Textbox("Prefix", "announce_prefix")
        .Default("Winners:")
        .Size("col-span-2")
)
```

| Token | Required? | Meaning |
|-------|-----------|---------|
| `grid-cols-N` | Recommended | `N` equal columns (default render: 1 if omitted) |
| `gap-N` | No | Default **3** |
| `items-start\|center\|end\|stretch\|baseline` | No | CSS `align-items` |

Empty Grid spec throws. Plain `WithVisibility` groups (no `grid`) keep the indented stack.

---

## Row

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

`grid-cols-N` on a Row throws.

---

## Size (per control)

One method. Tokens are **Tailwind class names**, space-separated, order-independent. Host resolves to CSS before serializing.

```csharp
.IntegerInput("How many first chatters", "max_count")
    .Range(1, 10)
    .Default(3)
    .Size("w-fit min-w-20 max-w-40")
```

### Width — `w-*`

| Class | Resolved CSS |
|-------|--------------|
| `w-fit` | `fit-content` |
| `w-full` | `100%` |
| `w-1/2`, `w-1/3`, `w-2/3`, `w-1/4`, `w-3/4` | percentages |
| `w-[120px]`, `w-[5rem]` | arbitrary value inside brackets |

Bare `fit` / `1/2` / `120px` are **not** accepted — always prefix with `w-`.

### Min / max — `min-w-*` / `max-w-*`

| Class | Resolved CSS |
|-------|--------------|
| `min-w-20` / `max-w-96` | scale → `{N×4}px` |
| `min-w-fit` / `max-w-full` | `fit-content` / `100%` |
| `min-w-min` / `max-w-max` | `min-content` / `max-content` |
| `min-w-[80px]` / `max-w-[5rem]` | arbitrary |

### Grid column span — `col-span-N`

| Class | Effect |
|-------|--------|
| `col-span-2` | `grid-column: span 2` |

**Throws** if used outside a direct `Grid(...)` (same idea as grow/shrink + Row).

### Flex — Row only

| Class | Effect |
|-------|--------|
| `grow` / `grow-0` | `flex-grow: 1` / `0` |
| `shrink` / `shrink-0` | `flex-shrink: 1` / `0` |

**Throws** outside a direct `Row(...)`.

**Notes**

- `w-fit` shrinks the **input row only** (label/hint stay full width via `FieldShell`). Matching `min-w-*` / `max-w-*` apply to that same input row.
- Combine freely: `.Size("w-full col-span-2")`, `.Size("grow shrink-0")`.

---

## Size cheatsheet

| Goal | Use |
|------|-----|
| Compact control in a vertical list | `.Size("w-fit")` or `.Size("w-1/2")` |
| Compact with bounds | `.Size("w-fit min-w-20 max-w-48")` |
| Side-by-side columns | `.Grid("grid-cols-2 …")` |
| Full-bleed row inside a Grid | `.Size("col-span-2")` |
| Input + button strip | `.Row("items-center", r => r.….Size("grow")….Size("shrink-0"))` |

---

## Comparator visibility

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

---

## RepeatFor (dynamic field count)

```csharp
.Section("Settings", "Settings", s => s
    .IntegerInput("Max places", "max_count")
        .Range(1, 10)
        .Default(3)
        .Size("w-fit")
)
.Section("Points", "Points", s => s
    .RepeatFor("max_count", (row, i) => row
        .IntegerInput($"Place #{i} points", $"points_{i}")
            .Range(0, 100000)
            .Default(i == 1 ? 1000 : 0)
            .Size("w-fit")
    )
)
```

Max from driver’s `.Range` (or explicit `max:`). Indices `<=` min always visible; higher indices gate with `gte`. Not the same as `WithRepeatableRows` (user-managed lists).

---

## Recipes

```csharp
.IntegerInput("How many", "max_count").Range(1, 10).Default(3).Size("w-fit min-w-16")
.Grid("grid-cols-2 items-center", g => g
    .Toggle("Exclude broadcaster", "exclude_broadcaster").Default(true)
    .Toggle("Exclude bots", "exclude_bot").Default(true)
)

.Row("gap-2 items-center", r => r
    .Textbox("Path", "path").Size("grow")
    .Button("Browse").Text("…").Size("shrink-0")
)

.WithVisibility("award_points", inner => inner
    .Textbox("Points variable", "points_variable").Default("points").Size("w-1/2")
    .RepeatFor("max_count", (row, i) => row
        .IntegerInput($"{i} place points", $"points_{i}")
            .Range(0, 10000000)
            .Size("w-fit")
    )
)
```

---

## What layout does *not* do

- Accept non-prefixed aliases (`fit`, `1/2`) — use `w-fit`, `w-1/2`.
- Accept arbitrary Tailwind beyond the curated Size/Grid/Row set.
- Remount the window when visibility/layout updates.
- Replace `WithRepeatableRows` for user-managed lists.

Protocol: [PROTOCOL.md — Layout / Visibility](../../FluentConfig/PROTOCOL.md).

## See also

- [VISIBILITY.md](VISIBILITY.md)
- [CONTROLS.md](CONTROLS.md)
