# FluentConfig — Architecture

Decision record for the WebView2 + Svelte implementation. Read this before
changing host/web boundaries or reintroducing WPF control-tree patterns.

The old WPF project has been removed; this tree is the product.

---

## Protocol contract

Host ↔ web messages are defined once and shared:

- Spec: [PROTOCOL.md](PROTOCOL.md) (source of truth — do not duplicate shapes here)
- C# DTOs: `Host/Protocol/`
- TypeScript types: `web/src/protocol/`

Wire format is **camelCase JSON**. No raw WPF / UI-framework objects cross the
boundary. Pill nested panels are `SchemaNode` trees (`itemTemplate` / `items`),
not `Panel` references.

---

## Module boundaries

| Module | Path | Owns | Must not touch |
|--------|------|------|----------------|
| Host | `Host/` | WPF+WebView2 window, DSL → schema, SettingsManager, updater, Protocol DTOs | `web/**` |
| Web | `web/` | Svelte 5 schema renderer, RPC client, mock bridge, Tailwind theme, CSP | `Host/**` |
| Protocol | `Host/Protocol/` + `web/src/protocol/` + `PROTOCOL.md` | Message shapes only | Business logic / UI chrome |
| Tests | `FluentConfig.Tests/` | Behavior specs (paths, validation, merge, protocol round-trip) | Shipping product code |
| Examples | `../examples/` (repo root) | Copy-paste Streamer.bot actions documenting the public DSL | Host/web internals |
| Docs | `../docs/`, this file | Integration guides, design notes | — |

---

## Runtime flow (summary)

```text
Plugin action  →  FluentConfigUi.Create  →  Host window + WebView2
Host pushes bootstrap (UiDocument)  →  Svelte renders schema
User edits  →  web store; save RPC  →  SettingsPathHelper merge  →  SettingsManager / CPH
Dynamic data (dropdown.refresh, etc.)  →  correlation-id RPC
Updater  →  host CheckForUpdate / StageUpdate; web only shows update-notice
```

---

## Legacy WPF port (historical)

The old WPF control-tree project was removed after Phase 1. **Do not** look for
per-control files like `Controls/ToggleBuilder.cs` — that layout no longer exists.

Today's DSL lives under `Host/Builders/`:

- `SectionBuilder` / `PanelBuilder` / fluent wrappers — author-facing API
- `PendingControl` — consolidated state + serialization for all control kinds
- `SchemaBuilderHelpers` — shared visibility / repeatable-rows helpers

Protocol DTOs are in `Host/Protocol/`; the Svelte renderer is schema-driven
(`web/src/lib/controls/`).

**DSL smell fixes (still required):**

1. Pill callbacks must never expose `System.Windows.Controls.Panel`.
2. Prefer `WithVisibilityWhenOff` or named `inverted:` — avoid positional-bool
   visibility overloads on wrappers.

### New in WebView2 host (no old WPF counterpart)

| Piece | Notes |
|-------|-------|
| `Host/Protocol/*` | Wire contract DTOs |
| WebView2 host window | Thin WPF shell |
| Updater (`CheckForUpdate` / `EnsureInstalled` / `StageUpdate` + swap helper) | Generic GitHub-releases module |
| `update-notice` schema node | Banner control |
| Svelte schema renderer + mock bridge | Web UI |
| Embedded HTML bundle | Release build step |

---

## Tests note

`FluentConfig.Tests` uses a `ProjectReference` to `Host/`.
Pure helpers (`InputValidation`, `DurationParsing`) live under `Host/Helpers/` as
**internal** experimental utilities (tested, not yet wired into the RPC/save path).
`InternalsVisibleTo("FluentConfig.Tests")` covers WindowManager test hooks.
