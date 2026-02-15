# FluentConfig Plugin Developer Guide

This guide explains how to build settings UIs for Streamer.bot extensions using FluentConfig. For a full control reference, see [ELEMENTS.md](ELEMENTS.md). For assembly references, see [REFERENCES.md](REFERENCES.md).

---

## 0. C# Execute action references

FluentConfig actions that use WPF types (Panel, StackPanel, ShowConfirmDialog, etc.) need framework assembly references. Add them **by name** so they resolve on any Windows system:

- **PresentationFramework**
- **PresentationCore**
- **WindowsBase**
- **FluentConfig** (path to your `FluentConfig.dll` in Streamer.bot's `dlls/` folder)

See **[REFERENCES.md](REFERENCES.md)** for full details and path fallbacks.

---

## 1. DLL check and version flow

Before creating the UI:

1. **DLL check** — Ensure the FluentConfig DLL is available in your extension's dependencies (or Streamer.bot's `dlls/` folder).
2. **Version check** — Use `FluentConfig.FluentConfig.GetVersion()` to verify the DLL version for compatibility.
3. **Avoid duplicates** — Call `FluentConfig.FluentConfig.AlreadyOpened(title, version)` before creating a new FluentConfig instance to prevent duplicate windows.

```csharp
FluentConfig.FluentConfig.SetLogCallback(msg => CPH.LogInfo($"[FluentConfig] {msg}"));

if (FluentConfig.FluentConfig.AlreadyOpened("My Extension", "1.0"))
    return true; // UI already open

var version = FluentConfig.FluentConfig.GetVersion();
```

---

## 2. Minimal FluentConfig usage (Create, Section, Show)

Use `FluentConfigUi.Create()` for the fluent DSL:

```csharp
FluentConfigUi.Create(CPH, "My Extension", "1.0")
    .Section("General", "General", s => s
        .Intro("Configure your settings below.")
        .Toggle("Enable Feature", "enabled")
        .Textbox("Username", "username")
        .Slider("Volume", "volume").Range(0, 100).Default(50))
    .Show();
```

- **Create(CPH, title, version)** — Creates the FluentConfig instance. `title` and `version` are used for the window title and settings storage key.
- **Section(title, tabId, build)** — Adds a tab. The `build` action receives a `SectionBuilder` for fluent controls.
- **Show()** — Displays the window.

Optional: `.Header(imageUrl)` to add a header image at the top.

---

## 3. Runtime access with GetValue&lt;T&gt; and withUi: false

When you need to read settings without showing the UI (e.g. in event handlers):

```csharp
// Create without UI — only for reading settings
var config = new FluentConfig.FluentConfig(CPH, "My Extension", "1.0", withUi: false);

// Load settings from CPH and read values
int volume = config.GetValue<int>("volume");
string username = config.GetValue<string>("username");
bool enabled = config.GetValue<bool>("enabled");
```

Use `withUi: false` when you only need to read/write settings and never show a window. This avoids STA/UI initialization.

---

## 4. Control-to-JSON mapping (saveKey, nested paths)

Each control has a `saveKey` that maps to a JSON path in the stored settings:

| saveKey              | Stored as                    |
|----------------------|------------------------------|
| `"volume"`           | `{ "volume": 50 }`           |
| `"rows[0].name"`     | `{ "rows": [{ "name": "..." }] }` |
| `"settings.timeout"` | `{ "settings": { "timeout": 30 } }` |

- **Simple keys** — One control per key (e.g. `Textbox("Label", "key")`).
- **Nested paths** — Use dot notation: `"parent.child"` or array: `"items[0].value"`.
- **Repeatable rows** — `WithRepeatableRows("rows", ...)` uses `rows[0]`, `rows[1]`, etc.

Settings are saved as JSON to Streamer.bot global variables (`FluentConfig_Settings_{extensionName}`). See [ELEMENTS.md](ELEMENTS.md) for the full nested keys table.

---

## 5. Dropdown variants (simple, refresh, pair-value)

**Simple (hardcoded options):**
```csharp
.Dropdown("Color", "color")
    .Options(new[] { "Random", "Blue", "Green" })
    .DefaultIndex(0)
```

**Refreshable (dynamic options):**
```csharp
.Dropdown("Group", "excludedGroup")
    .Options(groups)
    .Refresh(() => RefreshGroups())
    .DefaultIndex(0)
```

**Pair value (display name + stored ID):**
```csharp
.Dropdown("Device", "deviceName")
    .WithPairValue("deviceId")
    .Options(deviceTuples)  // IEnumerable<(string Value, string Display)>
    .DefaultByValue(lastId)
```

**Both refresh and pair value:**
```csharp
.Dropdown("Playlist", "playlistName")
    .WithPairValue("playlistId")
    .Options(playlists)
    .Refresh(() => FetchPlaylists())
    .DefaultByValue(lastId)
```

---

## 6. DurationInput usage

`DurationInput` stores durations as `"30seconds"`, `"3minutes"`, `"1days"`, or `"permanent"`:

```csharp
.DurationInput("Ban duration", "ban_duration")
    .Default("5minutes")
    .WithPermanentOption(true)
```

- **Full unit names** — `"seconds"`, `"minutes"`, `"hours"`, `"days"`, `"weeks"`, `"permanent"`.
- **WithPermanentOption** — Adds a "permanent" option when true.

---

## 7. Layout: Grid, Flex, Div

FluentConfig provides Tailwind CSS-inspired layout controls for side-by-side and grouped layouts. Use `Grid` for multi-column layouts, `Flex` for horizontal rows with optional wrapping, and `Div` to group multiple controls into a single cell.

**Basic Grid (equal columns):**
```csharp
.Grid(3, g => g
    .Toggle("A", "a")
    .Toggle("B", "b")
    .Toggle("C", "c")
)
```

**ColSpan (full-width control):**
```csharp
.Grid(3, g => g
    .IntegerInput("Price", "price")
    .IntegerInput("Qty", "qty")
    .Toggle("Enabled", "enabled")
    .Input("Notes", "notes")
        .Type("string")
        .Width("full")
        .ColSpan(3)
)
```

**Div (grouped columns):**
```csharp
.Grid(2, g => g
    .Div(d => d
        .Title("Left column")
        .IntegerInput("Price", "left_price")
        .DurationInput("Duration", "left_duration").WithPermanentOption(true)
    )
    .Div(d => d
        .Title("Right column")
        .Toggle("Enabled", "right_enabled")
        .Button("Action").Text("Do thing").OnClick(ui => ui.Toast("Done"))
    )
)
```

**Custom column widths:**
```csharp
.Grid(new[] { "auto", "1*", "2*", "auto" }, g => g
    .Toggle("Auto", "a")
    .Input("Star 1", "b").Width("full")
    .Input("Star 2", "c").Width("full")
    .Toggle("Auto", "d")
)
```
Column syntax: `"auto"` (content-sized), `"*"`/`"1*"` (1 fraction), `"2*"` (2 fractions), `"100"` (100px).

**Per-cell alignment (Justify, Align):**
```csharp
.Grid(4, g => g
    .IntegerInput("A", "a")
    .IntegerInput("B", "b").Justify("right")
    .IntegerInput("C", "c").Justify("center").Align("center")
    .IntegerInput("D", "d")
, gap: 8, padding: 4)
```
`Justify` = horizontal (left/right/center/stretch). `Align` = vertical (top/center/bottom/stretch).

**Flex with wrap:**
```csharp
.Flex(f => f
    .IntegerInput("X", "x")
    .IntegerInput("Y", "y")
    .IntegerInput("Z", "z")
, wrap: true, align: "center")
```

See [ELEMENTS.md](ELEMENTS.md) for the full layout option reference (ColSpan, RowSpan, Padding, layout-level justify/align).

---

## 8. Visibility: ShowWhen and WithVisibility

Controls and blocks can be shown or hidden based on conditions.

**Toggle-based (simple):** Control or block is visible when a toggle is checked.

```csharp
.Toggle("TTS costs points", "tts_costs_points").Default(true)
.NumberInput("Points cost", "default_voice_points_cost")
    .ShowWhen("tts_costs_points")

.WithVisibility("tts_costs_points", v => v
    .Intro("Pricing options when TTS costs points.")
    .NumberInput("Flat price", "flat_price").Default(100)
)
```

**Predicate-based (multi-logic):** Control or block is visible when a predicate returns `true`. Use `ctx.GetPendingValue<T>(key)` to read control values. Re-evaluates when any dependency control changes.

```csharp
.Dropdown("Pricing mode", "pricing_mode")
    .OptionsPairs(new[] { ("flat", "Flat only"), ("per_char", "Per character"), ("both", "Both") })
    .WithPairValue("pricing_mode_value")
    .DefaultByValue("flat")

.NumberInput("Flat price", "default_voice_flat_price")
    .ShowWhen(new[] { "pricing_mode_value" }, ctx =>
        ctx.GetPendingValue<string>("pricing_mode_value") == "flat" ||
        ctx.GetPendingValue<string>("pricing_mode_value") == "both")

.WithVisibility(new[] { "enabled", "mode" }, ctx =>
    ctx.GetPendingValue<bool>("enabled") && ctx.GetPendingValue<int>("mode") >= 1, v => v
    .Intro("Advanced options.")
    .Input("Setting", "advanced_setting")
)
```

- **ShowWhen(key)** — Visible when the toggle is on.
- **ShowWhen(dependencyKeys, predicate)** — Visible when predicate returns `true`. List all keys the predicate reads so visibility re-evaluates on change.
- **WithVisibility(toggleKey, build)** — Block visible when toggle is on.
- **WithVisibility(dependencyKeys, predicate, build)** — Block visible when predicate returns `true`.

---

## Elements reference

See **[ELEMENTS.md](ELEMENTS.md)** for a complete reference of all controls, their options, parameter types, defaults, and constraints. Use it when configuring any element without code examples.

---

## Programmatic updates

Use `SetValue<T>(key, value)` to update settings and controls at runtime (e.g. after connection success):

```csharp
config.SetValue("username", "new_user");
config.SetValue("enabled", true);
```

When the window is open, the corresponding control is updated. Call `MarkDirty()` if you need the change persisted on next Save.
