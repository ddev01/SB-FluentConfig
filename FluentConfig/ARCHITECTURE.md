# FluentConfig — Architecture

Decision record for the WebView2 + Svelte implementation. Read this before
changing host/web boundaries or reintroducing WPF control-tree patterns.

Historical port notes (old WPF tree → this layout) are kept below for context.
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

## Port classification (historical: old WPF → WebView2)

Legend:

| Tag | Meaning |
|-----|---------|
| **ported as-is** | Logic is UI-agnostic; copy with minimal namespace/project fixes |
| **ported-with-review** | Reusable core, but review for WebView2/schema sync before committing |
| **not ported, reimplemented** | WPF-tree / control rendering — replace with schema nodes + Svelte |
| **not ported, dropped** | Obsolete with the new architecture |

### `FluentConfig/Core/`

| Old WPF file | Classification | Current destination / notes |
|----------|----------------|--------------------------|
| `SettingsManager.cs` | **ported-with-review** | `Host/Core/` — CPH JSON load/save; null-CPH empty settings preserved |
| `SettingsPathHelper.cs` | **ported as-is** | `Host/Core/` — nested `rows[0].name` paths; behavior locked by tests |
| `PerformanceTracer.cs` | **ported-with-review** | `Host/Core/` — cold/warm timing; phases will rename (no WPF ThemeManager) |
| `FluentConfigWindowManager.cs` | **ported as-is** | `Host/Core/` — duplicate-window guard; keep lock semantics |
| `SettingsSynchronizer.cs` | **not ported, reimplemented** | Values-blob merge via path helper + save RPC (no control-tree walk) |
| `ControlRegistry.cs` | **not ported, reimplemented** | Schema id/saveKey registry + RPC handlers, not WPF tag maps |
| `WindowBuilder.cs` | **not ported, reimplemented** | Thin WPF shell hosting WebView2 only |

### `FluentConfig/App/`

| Old WPF file | Classification | Current destination / notes |
|----------|----------------|--------------------------|
| `FluentConfig.cs` | **ported-with-review** | Public facade (`AlreadyOpened`, `GetVersion`, `withUi: false` read path) |
| `FluentConfigPanelContext.cs` | **not ported, reimplemented** | Sub-builder / schema context; no Panel |
| `CallbackContext.cs` | **ported-with-review** | Pending values, RemoveSettingsKeys, dialogs — dialogs become RPC/web |
| `UiContext.cs` | **ported-with-review** | Same; Toast/Popup/Confirm map to protocol methods |

### `FluentConfig/Builders/`

| Old WPF file | Classification | Current destination / notes |
|----------|----------------|--------------------------|
| `SectionBuilder.cs` | **ported-with-review** | Emit `SectionSchema` + child `SchemaNode`s |
| `SectionFluentWrapper.cs` | **ported-with-review** | Preserve fluent chaining |
| `PanelBuilder.cs` | **not ported, reimplemented** | Schema group/sub-builder (no `Panel`) |
| `PanelFluentWrapper.cs` | **not ported, reimplemented** | Same |
| `FluentWrapperBase.cs` | **ported-with-review** | Keep if still useful for chaining |
| `ControlHostBuilder.cs` | **ported-with-review** | Host-level Create/Section/Show |
| `ControlBuilderBase.cs` | **ported-with-review** | Builders produce nodes, not WPF elements |
| `ControlBuilderCore.cs` | **ported-with-review** | Shared options (Hint, Default, ShowWhen) |
| `ControlOptionsBase.cs` | **ported-with-review** | Same |
| `Interfaces/IAddStrategy.cs` | **not ported, dropped** | WPF add-to-panel strategy |
| `Interfaces/IControlOptions.cs` | **ported-with-review** | If still needed for fluent options |
| `Interfaces/IFlushableControlBuilder.cs` | **ported-with-review** | Or drop if schema build is eager |
| `Controls/ToggleBuilder.cs` | **ported-with-review** | → `ToggleNode` (+ exclusive options) |
| `Controls/TextboxBuilder.cs` | **ported-with-review** | → `TextboxNode` |
| `Controls/SliderBuilder.cs` | **ported-with-review** | → `SliderNode` |
| `Controls/ButtonBuilder.cs` | **ported-with-review** | → `ButtonNode` + `button.click` RPC |
| `Controls/DropdownBuilder.cs` | **ported-with-review** | → `DropdownNode` + refresh RPC |
| `Controls/ColorPickerBuilder.cs` | **ported-with-review** | → `ColorPickerNode` |
| `Controls/DurationInputBuilder.cs` | **ported-with-review** | → `DurationInputNode` |
| `Controls/FilepathBuilder.cs` | **ported-with-review** | → `FilepathNode` + browse RPC |
| `Controls/NumberInputBuilder.cs` | **ported-with-review** | → `NumberInputNode` |
| `Controls/IntegerInputBuilder.cs` | **ported-with-review** | Fold into `NumberInputNode` (valueType) |
| `Controls/InputBuilder.cs` | **ported-with-review** | Same |
| `Controls/DynamicTextboxesBuilder.cs` | **ported-with-review** | → `DynamicTextboxesNode` |
| `Controls/PillInputBuilder.cs` | **not ported, reimplemented** | **API fix:** `WithItemTemplate` / sub-builder; no `WithSectionsPanel(Panel)` |
| `Controls/ImageBuilder.cs` | **ported-with-review** | Header/image may stay host-side or become a schema node — decide in A |
| `Controls/StatusIndicatorBuilder.cs` | **ported-with-review** | Map to schema if retained; else drop for v1 |

**DSL smell fixes (required in this architecture):**

1. Pill callbacks must never expose `System.Windows.Controls.Panel`.
2. `WithVisibility(key, true, …)` bare bool → named `inverted:` or `WithVisibilityWhenOff`.

### `FluentConfig/Elements/`

| Old WPF file | Classification | Current destination / notes |
|----------|----------------|--------------------------|
| `Abstractions/UIElement.cs` | **not ported, dropped** | Replaced by `SchemaNode` |
| `Abstractions/IRenderContext.cs` | **not ported, dropped** | Svelte render context |
| `Controls/ToggleSwitchElement.cs` | **not ported, reimplemented** | Web component |
| `Controls/CompetingToggleSwitchesElement.cs` | **not ported, reimplemented** | Exclusive toggle in web |
| `Controls/TextboxElement.cs` | **not ported, reimplemented** | Web |
| `Controls/SliderElement.cs` | **not ported, reimplemented** | Web |
| `Controls/ClickableButtonElement.cs` | **not ported, reimplemented** | Web |
| `Controls/DropdownElement.cs` | **not ported, reimplemented** | Web |
| `Controls/ColorPickerElement.cs` | **not ported, reimplemented** | Web |
| `Controls/DurationInputElement.cs` | **not ported, reimplemented** | Web (+ pure parse on host) |
| `Controls/FilepathElement.cs` | **not ported, reimplemented** | Web + host browse |
| `Controls/InputElement.cs` | **not ported, reimplemented** | Web |
| `Controls/DynamicTextboxesElement.cs` | **not ported, reimplemented** | Web |
| `Controls/PillInputElement.cs` | **not ported, reimplemented** | Web pill-input |
| `Controls/DescriptionElement.cs` | **not ported, reimplemented** | Web |
| `Controls/TitleElement.cs` | **not ported, reimplemented** | Web |
| `Controls/InlineSeparatorElement.cs` | **not ported, reimplemented** | Web |
| `Controls/ImageElement.cs` | **not ported, reimplemented** | Web or host header |
| `Controls/StatusIndicatorElement.cs` | **not ported, reimplemented** | Optional v1 |

### `FluentConfig/Components/`

| Old WPF file | Classification | Current destination / notes |
|----------|----------------|--------------------------|
| `FluentConfigComponentFactory.cs` | **not ported, dropped** | WPF-UI factory |
| `TabManager.cs` | **not ported, reimplemented** | Sidebar/tabs in Svelte from `sections[]` |
| `RichTextParser.cs` | **ported-with-review** | Only if rich intro text is kept; else simplify to plain/markdown in web |

### Related old folders (outside Core/App/Builders/Elements/Components)

| Old file | Classification | Notes |
|----------|----------------|-------|
| `FluentConfigUi.cs` | **ported-with-review** | Entry DSL |
| `Helpers/InputValidation.cs` | **ported-with-review** | Pure parse/format/hex/duplicates; **drop** WPF PreviewTextInput handlers |
| `Helpers/ControlExtractionHelper.cs` | **not ported, reimplemented** | Keep pure `ParseDuration` only; drop panel walkers |
| `Helpers/VisualTreeHelper.cs` | **not ported, dropped** | WPF tree |
| `Helpers/DialogHelper.cs` | **not ported, reimplemented** | `dialog.confirm` / popup RPC |
| `Helpers/ProgressReporter.cs` | **ported-with-review** | Progress push events |
| `Helpers/ProgressWindowHelper.cs` | **not ported, reimplemented** | Web progress UI |
| `Helpers/FluentConfigTags.cs` | **not ported, dropped** | WPF Tag prefixes |
| `Interfaces/IProgressReporter.cs` | **ported-with-review** | Keep interface if useful |

### New in WebView2 host (no old WPF counterpart)

| Piece | Notes |
|-------|-------|
| `Host/Protocol/*` | Phase 0 contract |
| WebView2 host window | Thin WPF shell |
| Updater (`CheckForUpdate` / `EnsureInstalled` / `StageUpdate` + swap helper) | Generic GitHub-releases module |
| `update-notice` schema node | Banner control |
| Svelte schema renderer + mock bridge | Web UI |
| Embedded HTML bundle | Phase 2 build step |

---

## Tests note

`FluentConfig.Tests` uses a `ProjectReference` to `Host/`.
Pure helpers (`InputValidation`, `DurationParsing`) live under `Host/Helpers/`.
`InternalsVisibleTo("FluentConfig.Tests")` covers WindowManager test hooks.
