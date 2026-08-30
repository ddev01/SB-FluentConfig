# Dialogs and runtime values

Buttons, feedback surfaces, and how to read/write settings — inside the UI and from other actions.

Menu actions use `Fc.Open(cph, id, title, version, build)` (focus if already open). Runtime actions use the `Fc.*` helpers below — **same menu id** as the menu (not the window title).

Runnable examples:

- Dialogs: [examples/tutorial/06_DialogsAndFeedback.cs](../../examples/tutorial/06_DialogsAndFeedback.cs)
- Reading saved JSON: [examples/tutorial/10_ReadingSavedSettings.cs](../../examples/tutorial/10_ReadingSavedSettings.cs)
- Runtime helpers: [examples/tutorial/11_RuntimeHelpers.cs](../../examples/tutorial/11_RuntimeHelpers.cs)

## Button + OnClick

```csharp
.Button("Test action")
    .Text("Show values")
    .Color("#714bfd")
    .OnClick(ui =>
    {
        string mode = ui.Pending<string>("mode");
        int level = ui.Pending<int>("level");
        ui.Popup("Current values", $"Mode: {mode}\nLevel: {level}");
    })
```

## Feedback surfaces (`UiContext`)

| Method | Role |
|--------|------|
| `Popup(title, body)` | Modal message |
| `Toast(message)` | Short-lived notification |
| `ShowConfirmDialog(title, body, yes, no)` | Yes/No → `bool` |
| `ShowProgressWindow(title, subtitle, label, total)` | Progress reporter (`.Report` / `.Close`) |
| `Log(message)` | Host log callback |

## Reading values by context

| Context | API |
|---------|-----|
| After the user opens/saves the UI | `Fc.LoadSettings<T>(CPH, id)` (or `Fc.GetSetting<T>` / `Fc.SettingsKeyFor(id)`) |
| Write from a runtime action | `Fc.SetSetting` / `Fc.SaveSettings` / `Fc.HasSavedSettings` |
| Runtime state JSON (not the menu) | `Fc.LoadData<T>` / `Fc.SaveData` / `Fc.GetData` / `Fc.SetData` → `{id}_data` |
| Event args + templates + logger | `Fc.CaptureEvent` / `Fc.ApplyTemplate` / `Fc.Logger` |
| Button `OnClick` handlers | `UiContext.Pending<T>(saveKey)`, `SetPending` (live `values.patch`), `ItemName` for pill-template buttons |
| Pill `OnPillAdded` / `OnPillRemoved` | `CallbackContext.GetValue<T>(saveKey)` |

### Runtime action (outside the UI)

Settings persist as JSON under `{id}_settings`. The **id** must match the menu's id (`[a-z][a-z0-9_]*`); the window title is a label only.

```csharp
using FluentConfig;

private const string Id = "my_extension"; // → my_extension_settings

if (!Fc.HasSavedSettings(CPH, Id))
{
    Fc.Logger(CPH, "My Extension", "1.0").Info("Open settings and Save first.");
    return true;
}

var settings = Fc.LoadSettings<MySettings>(CPH, Id);
// Or a single field:
string mode = Fc.GetSetting(CPH, Id, "mode", "Normal");

// Write without opening the UI (other keys preserved):
Fc.SetSetting(CPH, Id, "access_token", "");
Fc.SaveSettings(CPH, Id, o =>
{
    o["access_token"] = "";
    o["refresh_token"] = "";
});
```

### Event args, templates, logger

```csharp
var log = Fc.Logger(CPH, "My Extension", "1.0");
log.Init(); // one banner with action/user/command

var ev = Fc.CaptureEvent(CPH);
string chat = Fc.ApplyTemplate("Hi %user%!", ev); // also supports {user}
```

`.LogExistingSettings()` on the menu builder redacts secrets (`token`, `password`, `secret`, …) before logging.

### Runtime data blob (`{id}_data`)

For giveaway/timer-style state that is **not** the settings menu:

```csharp
var state = Fc.LoadData<MyState>(CPH, Id); // → my_extension_data
state.Count++;
Fc.SaveData(CPH, Id, state);

// Or nested paths:
Fc.SetData(CPH, Id, "count", 9);
int n = Fc.GetData(CPH, Id, "count", 0);
```

See [examples/tutorial/10_ReadingSavedSettings.cs](../../examples/tutorial/10_ReadingSavedSettings.cs) and [11_RuntimeHelpers.cs](../../examples/tutorial/11_RuntimeHelpers.cs).

## See also

- [PILLS.md](PILLS.md) — `CallbackContext` in pill callbacks
- [CONTROLS.md](CONTROLS.md)
