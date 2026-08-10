# Dialogs and runtime values

Buttons, feedback surfaces, and how to read settings — inside the UI and from other actions.

Runnable examples:

- Dialogs: [examples/tutorial/06_DialogsAndFeedback.cs](../../examples/tutorial/06_DialogsAndFeedback.cs)
- Reading saved JSON: [examples/tutorial/10_ReadingSavedSettings.cs](../../examples/tutorial/10_ReadingSavedSettings.cs)

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
| After the user opens/saves the UI | `Fc.LoadSettings<T>(CPH, title)` (or `Fc.GetSetting<T>` / `Fc.SettingsKeyFor(title)`) |
| Button `OnClick` handlers | `UiContext.Pending<T>(saveKey)` |
| Pill `OnAdded` / `OnRemoved` | `CallbackContext.GetValue<T>(saveKey)` |

### Runtime action (outside the UI)

Settings persist as JSON under `{slug}_settings` (title slugified to snake_case). The **title** must match the menu's title:

```csharp
using FluentConfig;

private const string Title = "My Extension"; // → my_extension_settings

var settings = Fc.LoadSettings<MySettings>(CPH, Title);
// Or a single field:
string mode = Fc.GetSetting(CPH, Title, "mode", "Normal");
```

See [examples/tutorial/10_ReadingSavedSettings.cs](../../examples/tutorial/10_ReadingSavedSettings.cs) for a full companion action.

## See also

- [PILLS.md](PILLS.md) — `CallbackContext` in pill callbacks
- [CONTROLS.md](CONTROLS.md)
