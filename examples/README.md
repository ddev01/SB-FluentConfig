# FluentConfig examples

Copy-paste Streamer.bot C# Execute actions targeting the WebView2 host (`FluentConfig/Host`).
Use **Execute C# Method** with **Run on UI thread** enabled. Assembly refs: see [docs/REFERENCES.md](../docs/REFERENCES.md).

Update paths (DllCheck vs extension modal) are documented in [docs/EXTENSION_UPDATES.md](../docs/EXTENSION_UPDATES.md).

## menu/

Settings UI examples — building and opening FluentConfig menus.

| File | Description |
|------|-------------|
| **SimpleExample.cs** | Minimal: toggle, textbox, slider. |
| **MediumExample.cs** | Dropdown, slider, button, `ShowWhen`. |
| **MediumValuesExample.cs** | Runtime action: read saved settings from Medium Example via `CPH.GetGlobalVar`. |
| **CompleteExample.cs** | Full control surface with Pill / `WithVisibility` APIs. |
| **DevPreviewExample.cs** | Mirrors `localhost:5173` mock document (General / Inputs / Lists). |

## updater/

| File | Description |
|------|-------------|
| **DllCheckExample.cs** | No FluentConfig ref — install + daily auto-stage of `FluentConfig.dll`. |
| **ExtensionUpdateExample.cs** | Menu + `.WithExtensionUpdateNotice` (latest or tagged) + optional mins / guide URL. |

Action chain order: **DllCheck → Menu**. Call patterns match [PROTOCOL.md](../FluentConfig/PROTOCOL.md).
