# FluentConfig Elements Reference

Complete reference for all FluentConfig controls and their options. For usage patterns and getting started, see [PLUGIN_DEVELOPER_GUIDE.md](PLUGIN_DEVELOPER_GUIDE.md). For assembly references, see [REFERENCES.md](REFERENCES.md).

---

## Common options

These options apply across many controls. Type or behavior may vary per element.

| Option | Params | Description |
|--------|--------|-------------|
| **Hint** | `string` | Helper text below the label. |
| **Default** | `bool` \| `string` \| `int` \| `double` | Initial value. Type depends on control. |
| **ShowWhen** | `key` | Control visible only when toggle `key` is `true`. Toggle must exist earlier. |

---

## Section-level controls

Available in `SectionBuilder` (inside `Section(...)`).

| Method | Params | Description |
|--------|--------|-------------|
| `Intro(text)` | `string` | Description text at top of tab. |
| `Title(text)` | `string` | Section title block. |
| `Separator()` | — | Horizontal divider line. |
| `WithVisibility(toggleKey, build)` | `string`, `Action<PanelBuilder>` | Block visible when toggle `toggleKey` is `true`. `build` receives `PanelBuilder` to add controls. |
| `WithVisibility(toggleKey, inverted, build)` | `string`, `bool`, `Action<PanelBuilder>` | Same, but `inverted`: when `true`, block visible when toggle is `false`. |

---

## Quick reference

| Element | Stored type | Options |
|---------|-------------|---------|
| Toggle | `bool` | Hint, Default, ShowWhen, WithExclusive + sub-options |
| Textbox | `string` | Hint, Default, Password, Multiline, ShowWhen |
| Slider | `int` | Hint, Range, Default, ShowWhen |
| Button | *(none)* | Hint, Text, Color, OnClick, ShowWhen |
| Input | `string` \| `int` \| `double` \| `float` | Hint, Type, Range, Step, Default, WithStepper, ShowWhen |
| IntegerInput | `int` | *(alias for Input with Type "int")* Hint, Range, Default, ShowWhen |
| DurationInput | `string` | Hint, Default, WithPermanentOption, ShowWhen |
| Filepath | `string` | Hint, Default, ShowWhen |
| NumberInput | `double` | *(alias for Input with Type "double")* Hint, WithStepper, Range, Step, Default, ShowWhen |
| ColorPicker | `string` (hex) | Hint, Default, ShowWhen |
| Dropdown | `string` | Hint, Options, WithPairValue, Refresh, DefaultIndex, DefaultByValue, ShowWhen |
| DynamicTextboxes | `string[]` | Hint, Preset, AllowDuplicates, ShowWhen |
| PillInput | `string[]` | Hint, WithSectionsPanel, OnPillAdded, OnPillRemoved, ShowWhen |

---

## Toggle

**Signature:** `Toggle(label, key)`  
**Stored as:** `bool`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Default | `bool` | `false` |
| ShowWhen | `key` | — |
| **WithExclusive** | `string[]` | — |

**WithExclusive(options)** — Renders multiple toggles; at most one (or N) can be true. Sub-options:

| Sub-option | Params | Default | Notes |
|------------|--------|---------|-------|
| MaxSelected | `int` | `1` | Max toggles that can be true. Min 1. |
| DefaultIndex | `int` | `0` | Initial selected index (single). |
| DefaultIndices | `int[]` | — | Initial selected indices (multi). |

Without WithExclusive: single on/off switch.

---

## Textbox

**Signature:** `Textbox(label, key)`  
**Stored as:** `string`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Default | `string` | `""` |
| Password | — | — |
| Multiline | — | — |
| ShowWhen | `key` | — |

**Password** — Masks input. **Multiline** — Multiple lines.

---

## Slider

**Signature:** `Slider(label, key)`  
**Stored as:** `int`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Range | `(int min, int max)` | — *required* |
| Default | `int` | — |
| ShowWhen | `key` | — |

---

## Button

**Signature:** `Button(label)`  
**Stored as:** Nothing

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Text | `string` | `"OK"` |
| Color | `string` (hex) | — |
| OnClick | `Action<UiContext>` | — |
| ShowWhen | `key` | — |

**UiContext:** `Pending<T>(key)`, `Popup(title, message)`, `Toast(message)`, `ShowConfirmDialog(...)`, `ShowProgressWindow(...)`, `Log(message)`.

---

## Input

**Signature:** `Input(label, key)`  
**Stored as:** `string` (default), `int`, `double`, or `float` depending on `Type`

Generic input with type-based validation. Use `Type("string")`, `Type("int")`, `Type("double")`, or `Type("float")` to enforce validation and formatting. Double/float values use comma as decimal separator.

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| **Type** | `string` | `"string"` — `"string"`, `"int"`, `"double"`, `"float"` |
| Range | `(int min, int max)` or `(double min, double max)` | int: `(0, int.MaxValue)`; double/float: `(0, 100)` |
| Step | `double` | `1` — double/float only |
| Default | `string` \| `int` \| `double` | Type-dependent |
| WithStepper | `bool` | `false` — double/float only, adds +/- buttons |
| ShowWhen | `key` | — |

---

## IntegerInput

**Signature:** `IntegerInput(label, key)`  
**Stored as:** `int` — Alias for `Input(label, key).Type("int")`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Range | `(int min, int max)` | `(0, int.MaxValue)` |
| Default | `int` | `0` |
| ShowWhen | `key` | — |

---

## DurationInput

**Signature:** `DurationInput(label, key)`  
**Stored as:** `string` — `"Nseconds"`, `"Nminutes"`, `"Nhours"`, `"Ndays"`, `"Nweeks"`, `"permanent"`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Default | `string` | `"permanent"` |
| WithPermanentOption | `bool` | `true` |
| ShowWhen | `key` | — |

---

## Filepath

**Signature:** `Filepath(label, key)`  
**Stored as:** `string`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Default | `string` | `""` |
| ShowWhen | `key` | — |

---

## NumberInput

**Signature:** `NumberInput(label, key)`  
**Stored as:** `double` — Alias for `Input(label, key).Type("double")`. Double values use comma as decimal separator.

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| **WithStepper** | `bool` | `false` |
| Range | `(double min, double max)` | `(0, 100)` |
| Step | `double` | `1` |
| Default | `double` | `0` |
| ShowWhen | `key` | — |

Without WithStepper: plain numeric text input. With WithStepper: text input plus +/- stepper buttons.

---

## ColorPicker

**Signature:** `ColorPicker(label, key)`  
**Stored as:** `string` (hex)

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Default | `string` | `"#000000"` |
| ShowWhen | `key` | — |

---

## Dropdown

**Signature:** `Dropdown(label, key)`  
**Stored as:** `string`; with WithPairValue: `key` = display, `valueKey` = value

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Options | `string[]` | `[]` |
| Options | `IEnumerable<(Value, Display)>` | — |
| **WithPairValue** | `valueKey` | — |
| Refresh | `Func<string[]>` or `Func<IEnumerable<(Value, Display)>>` | — |
| DefaultIndex | `int` | `0` |
| DefaultByValue | `string` | — |
| ShowWhen | `key` | — |

**WithPairValue(valueKey)** — Store value in `valueKey`, display in `key`. Use with tuple Options and DefaultByValue.

---

## DynamicTextboxes

**Signature:** `DynamicTextboxes(label, key)`  
**Stored as:** `string[]`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| Preset | `string[]` | `[]` |
| **AllowDuplicates** | `bool` | `true` — When `false`, duplicate values show a validation error (red border). |
| ShowWhen | `key` | — |

User adds/removes rows. Each row = one array element.

---

## PillInput

**Signature:** `PillInput(label, key)`  
**Stored as:** `string[]`

| Option | Params | Default |
|--------|--------|---------|
| Hint | `string` | — |
| WithSectionsPanel | `(sections, tab, ctx) => void` | — |
| OnPillAdded | `(pill, sections, ctx) => void` | — |
| OnPillRemoved | `(pill, sections, ctx) => void` | — |
| ShowWhen | `key` | — |

**Validation:** Pill names must be unique (case-insensitive). Duplicate names are rejected with a log message.

**CallbackContext (`ctx`):** `GetValue<T>(key)`, `RemoveSettingsKeys(...)`, `WithPanel(panel, tabName, build)`.

WithSectionsPanel hydrates existing pills. OnPillAdded/OnPillRemoved handle changes. Track panels in `Dictionary<string, Panel>`.

---

## Panel-level controls

Inside WithVisibility, WithRepeatableRows, or PillInput callbacks: use `PanelBuilder`.

- All controls above
- `Title`, `Intro`, `Separator`
- `WithVisibility(toggleKey, build)` and `WithVisibility(toggleKey, inverted, build)`
- `WithRepeatableRows(saveKey, buildRow)` — **PanelBuilder only.** Rows: `saveKey[0]`, `saveKey[1]`, …

---

## Nested keys

| Key pattern | Stored as |
|-------------|-----------|
| `"volume"` | `{ "volume": 50 }` |
| `"settings.timeout"` | `{ "settings": { "timeout": 30 } }` |
| `"rows[0].name"` | `{ "rows": [{ "name": "..." }] }` |
