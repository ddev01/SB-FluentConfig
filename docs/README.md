# FluentConfig docs

Host-side documentation for the WebView2 FluentConfig library.

| Doc | Contents |
|-----|----------|
| [REFERENCES.md](REFERENCES.md) | Streamer.bot C# action assembly references + Run on UI thread |
| [PLUGIN_DEVELOPER_GUIDE.md](PLUGIN_DEVELOPER_GUIDE.md) | Authoring extensions against the host DSL |
| [LAYOUT.md](LAYOUT.md) | Grid / Row / Size (1:1 Tailwind) / RepeatFor / comparator visibility |
| [EXTENSION_UPDATES.md](EXTENSION_UPDATES.md) | Self-update vs notify-only extension updates |
| [performance/README.md](performance/README.md) | Cold/warm open baselines |
| [../FluentConfig/PROTOCOL.md](../FluentConfig/PROTOCOL.md) | Host ↔ web message contract |
| [../FluentConfig/Host/PACKAGING.md](../FluentConfig/Host/PACKAGING.md) | Deploy footprint: FluentConfig.dll (+ Newtonsoft); WebView2 from Streamer.bot |
| [../FluentConfig/ARCHITECTURE.md](../FluentConfig/ARCHITECTURE.md) | Module boundaries |
| [../examples/README.md](../examples/README.md) | Example actions |

## Layout

```
FluentConfig/
  Host/           C# net481 WPF + WebView2 host (AssemblyName FluentConfig)
  web/            Svelte 5 UI
  UpdaterHelper/  Detached swap-and-relaunch exe
  PROTOCOL.md     Shared wire contract
docs/             This folder (plugin author docs)
examples/         Copy-paste Streamer.bot actions
```
