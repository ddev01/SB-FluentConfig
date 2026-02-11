# Sbui Plugin Developer Guide

This guide explains how to build settings UIs for Streamer.bot extensions using Sbui.

---

## 1. DLL check and version flow

Before creating the UI:

1. **DLL check** — Ensure the Sbui DLL is available in your extension's dependencies (or Streamer.bot's `dlls/` folder).
2. **Version check** — Use `Sbui.Sbui.GetVersion()` to verify the DLL version for compatibility.
3. **Avoid duplicates** — Call `Sbui.Sbui.AlreadyOpened(title, version)` before creating a new Sbui instance to prevent duplicate windows.

```csharp
Sbui.Sbui.SetLogCallback(msg => CPH.LogInfo($"[Sbui] {msg}"));

if (Sbui.Sbui.AlreadyOpened("My Extension", "1.0"))
    return true; // UI already open

var version = Sbui.Sbui.GetVersion();
```

---

## 2. Minimal Sbui usage (Create, Section, Show)

Use `SbuiUi.Create()` for the fluent DSL:

```csharp
SbuiUi.Create(CPH, "My Extension", "1.0")
    .Section("General", "General", s => s
        .Intro("Configure your settings below.")
        .Toggle("Enable Feature", "enabled")
        .Textbox("Username", "username")
        .Slider("Volume", "volume").Range(0, 100).Default(50))
    .Show();
```

- **Create(CPH, title, version)** — Creates the Sbui instance. `title` and `version` are used for the window title and settings storage key.
- **Section(title, tabId, build)** — Adds a tab. The `build` action receives a `SectionBuilder` for fluent controls.
- **Show()** — Displays the window.

Optional: `.Header(imageUrl)` to add a header image at the top.

---

## 3. Runtime access with GetValue&lt;T&gt; and withUi: false

When you need to read settings without showing the UI (e.g. in event handlers):

```csharp
// Create without UI — only for reading settings
var sbui = new Sbui.Sbui(CPH, "My Extension", "1.0", withUi: false);

// Load settings from CPH and read values
int volume = sbui.GetValue<int>("volume");
string username = sbui.GetValue<string>("username");
bool enabled = sbui.GetValue<bool>("enabled");
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

Settings are saved as JSON to Streamer.bot global variables (`Sbui_Settings_{extensionName}`).

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
    .WithRefresh(() => RefreshGroups())
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
    .WithRefresh(() => FetchPlaylists())
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
- **Legacy support** — Compact form (`"30s"`, `"3m"`) is still parsed when loading.
- **WithPermanentOption** — Adds a "permanent" option when true.

---

## Programmatic updates

Use `SetValue<T>(key, value)` to update settings and controls at runtime (e.g. after connection success):

```csharp
sbui.SetValue("username", "new_user");
sbui.SetValue("enabled", true);
```

When the window is open, the corresponding control is updated. Call `MarkDirty()` if you need the change persisted on next Save.
