# FluentConfig examples

Copy-paste Streamer.bot C# Execute actions targeting the WebView2 host (`FluentConfig/Host`).
Use **Execute C# Method** with **Run on UI thread** enabled. Assembly refs: see [docs/REFERENCES.md](../docs/REFERENCES.md).

| File | Description |
|------|-------------|
| **SimpleExample.cs** | Minimal: toggle, textbox, slider. |
| **MediumExample.cs** | Dropdown, slider, button, `ShowWhen`. |
| **DevPreviewExample.cs** | Mirrors `localhost:5173` mock document (General / Inputs / Lists) for in-Streamer.bot comparison. |
| **CompleteExample.cs** | Full control surface with Pill / `WithVisibility` APIs (schema sub-builders — no raw WPF `Panel`). |
| **UpdaterExample.cs** | Notify-only extension update banner (`WithExtensionUpdateNotice`). |

Call patterns match [PROTOCOL.md](../FluentConfig/PROTOCOL.md).
