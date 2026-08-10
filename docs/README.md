# FluentConfig docs

Plugin-author documentation for the WebView2 FluentConfig library.

**New here?** Start with the [root README quick start](../README.md#quick-start), then work through [examples/tutorial/](../examples/tutorial/) (01 → 10).

## Guides

| Doc | Contents |
|-----|----------|
| [guides/README.md](guides/README.md) | Author landing page + quick start snippet |
| [guides/CONTROLS.md](guides/CONTROLS.md) | Control catalog and chained options |
| [guides/VISIBILITY.md](guides/VISIBILITY.md) | `ShowWhen` / `WithVisibility` / `WithVisibilityWhenOff` |
| [guides/LAYOUT.md](guides/LAYOUT.md) | Grid / Row / Size / RepeatFor |
| [guides/DIALOGS_AND_RUNTIME_VALUES.md](guides/DIALOGS_AND_RUNTIME_VALUES.md) | Buttons, dialogs, reading saved values |
| [guides/PILLS.md](guides/PILLS.md) | PillInput + nested ItemTemplate |
| [guides/UPDATES.md](guides/UPDATES.md) | FluentConfig.dll vs extension update paths |
| [guides/EXTRAS.md](guides/EXTRAS.md) | Known bots, window chrome, perf pointer |

## Setup

| Doc | Contents |
|-----|----------|
| [setup/REFERENCES.md](setup/REFERENCES.md) | Streamer.bot C# action assembly references + Run on UI thread |

## Examples

| Doc | Contents |
|-----|----------|
| [../examples/README.md](../examples/README.md) | Guided tutorial + reference / deployment / dev buckets |

## Contributor / architecture (colocated with code)

| Doc | Contents |
|-----|----------|
| [../FluentConfig/PROTOCOL.md](../FluentConfig/PROTOCOL.md) | Host ↔ web message contract |
| [../FluentConfig/Host/PACKAGING.md](../FluentConfig/Host/PACKAGING.md) | Deploy footprint: FluentConfig.dll (+ Newtonsoft); WebView2 from Streamer.bot |
| [../FluentConfig/ARCHITECTURE.md](../FluentConfig/ARCHITECTURE.md) | Module boundaries |
| [../FluentConfig/README.md](../FluentConfig/README.md) | Host/web package overview |
| [performance/README.md](performance/README.md) | Cold/warm open baselines |
| [internal/CODE_REVIEW_AND_REFACTOR_PLAN.md](internal/CODE_REVIEW_AND_REFACTOR_PLAN.md) | Point-in-time production-readiness audit |

## Layout

```
docs/
  guides/       Plugin author guides (start here)
  setup/        Assembly refs / Streamer.bot setup
  performance/  Cold/warm open baselines
  internal/     Non-user docs (audits, handoffs)
examples/
  tutorial/     Guided 01 → 10 path
  reference/    Full control showcase
  deployment/   DllCheck + extension update notice
  dev/          Contributor mock-parity preview
FluentConfig/
  Host/         C# net481 WPF + WebView2 host
  web/          Svelte 5 UI
  PROTOCOL.md   Shared wire contract
```
