# FluentConfig — Host ↔ Web Protocol

Single source of truth for messages between the C# WebView2 host (`Host/Protocol/`) and the Svelte UI (`web/src/protocol/`). Host and web must stay in sync against these shapes; do not invent parallel ad-hoc JSON.

Wire format: **camelCase JSON** on both sides (`Newtonsoft.Json` camelCase resolver on the host; TypeScript interfaces use the same names).

---

## Boundary rule

**No raw WPF / UI-framework objects ever cross this boundary.** The web UI receives only JSON schema nodes and values. The host never posts `Panel`, `StackPanel`, `UIElement`, or any other framework type through `PostWebMessageAsJson`.

### Fixing the old Pill panel leak

In the WPF FluentConfig API, pill callbacks historically handed plugin authors a raw `System.Windows.Controls.Panel` to mutate (see `examples/reference/FullControlShowcase.cs` / `examples/tutorial/09_PillsAndNestedItems.cs`). That leaks the UI toolkit into author code and cannot work with a schema-driven web renderer. Use `.ItemTemplate` / `.WithItemTemplate` instead.

**New shape:** `pill-input` nodes carry nested controls as more `SchemaNode`s:

- `itemTemplate` — reusable child nodes (relative `saveKey`s may use `"{name}"` for the pill item name, e.g. `"{name}_enabled"`).
- `items` — host-expanded `{ name, children }` entries for pills that already exist (bootstrap or after `pill.changed`).

Author callbacks on the host receive a fluent **sub-builder** that emits schema nodes, never a `Panel` reference.

---

## Message flow

```text
1. Host opens WebView → pushes event "bootstrap" with UiDocument
   (title, version, colorScheme, sections[], values{})

2. User edits controls in Svelte
   → values live in the web store; optional values.patch from host

3. Dynamic data (e.g. channel rewards / dropdown.refresh)
   → web sends kind:"request" { id, method, params }
   → host replies kind:"response" { id, result | error }

4. Save
   → web request method "save" with full values blob
   → host persists via SettingsManager / CPH

5. Buttons / extension update modal / filepath browse
   → RPC to host; host may push progress / dialog.* / schema.patch / update.available

6. Progress (e.g. long OnClick work)
   → host pushes event "progress" { id, percent|current/total, message, done? }
```

Either side may send `kind:"request"` (e.g. host → web `dialog.confirm`). Correlation is always the numeric `id`.

---

## Envelope

| kind | Fields |
|------|--------|
| `request` | `id`, `method`, `params?` |
| `response` | `id`, `result?` **or** `error: { code, message }` |
| `event` | `event`, `payload?` |

### Push events

| event | Payload |
|-------|---------|
| `bootstrap` | `UiDocument` |
| `progress` | `{ id, title?, message?, percent?, current?, total?, done? }` |
| `values.patch` | `{ paths? }` and/or `{ values? }` |
| `update.available` | `{ noticeId?, currentVersion, latestVersion, releaseNotes?, downloadUrl?, repo?, mode: "notify", releasePageUrl?, updateGuideUrl? }` |
| `schema.patch` | `{ sectionId?, nodeId?, node }` |

### Visibility

Every `SchemaNode` may include:

```json
"visibility": { "saveKey": "optional_limit_enabled", "equals": true, "inverted": false }
```

`ShowWhen(key)` → `{ saveKey: key, equals: true }`.  
`ShowWhen(key, "random")` → `{ saveKey: key, equals: "random" }` (`equals` may be boolean, string, or number).  
`ShowWhenNot(key, "free")` → `{ saveKey: key, equals: "free", inverted: true }`.  
`WithVisibility(key, inverted: true, …)` → a `group` node with `{ saveKey: key, equals: true, inverted: true }`.  
`WithVisibility(key, "random", …)` → a `group` with `{ saveKey: key, equals: "random" }` and `"indented": false` (flat chrome). Toggle groups omit `indented` (left rail).

#### Comparator operators (additive)

When `operator` is set (`gte` | `lte` | `gt` | `lt`), the legacy `equals` path is skipped. Compare the live numeric value at `saveKey` against either a literal `value` or another live key (`compareKey`):

```json
"visibility": { "saveKey": "max_count", "operator": "gte", "value": 4 }
```

```json
"visibility": { "saveKey": "a", "operator": "gte", "compareKey": "b" }
```

`inverted` still negates the result. Missing/NaN values hide the node (conservative default). When `operator` is absent, the equals path matches by type: boolean (`===`, no `"true"` coercion), number (`toNumber(actual) === expected`), string (`String(actual) === expected`, no trim), null (missing/null actual). Missing live values fall back to the driver control's schema default (dropdown `defaultByValue` / first option, toggle `defaultValue`).

`ShowWhen(key, Comparator.GreaterOrEqual, 4)` and `WithVisibility(key, Comparator.GreaterOrEqual, 4, …)` emit this shape.

#### RepeatFor (sugar — no new wire type)

`RepeatFor(driverKey, (row, i) => …)` materializes real controls for each index up to the driver's declared `.Range` max (or an explicit `max`). Indices at/below the driver's min are appended ungated; higher indices wrap in a `group` with `{ operator: "gte", value: i }`. Values are preserved when the driver shrinks — visibility only toggles rendering. No host round-trip.

### Layout (Grid / Row)

A `group` may carry a `grid` spec (CSS grid or flex row). Any `SchemaNode` may carry a `layout` hint. Host fluent API uses **1:1 Tailwind class names** in compound specs (`Grid("grid-cols-2 gap-3 items-center", …)`, `.Size("w-fit min-w-20 col-span-2")`); the wire carries **host-resolved CSS values** — never literal Tailwind class strings.

```json
{
  "type": "group",
  "id": "grid_2",
  "grid": { "mode": "grid", "columns": 2, "gap": 3, "align": "center" },
  "children": [
    { "type": "toggle", "saveKey": "a", "label": "A" },
    {
      "type": "textbox",
      "saveKey": "name",
      "label": "Name",
      "layout": { "span": 2, "width": "50%", "minWidth": "80px", "maxWidth": "384px" }
    },
    {
      "type": "textbox",
      "saveKey": "path",
      "label": "Path",
      "layout": { "grow": true }
    }
  ]
}
```

- `mode`: `"grid"` (default) or `"row"` (flex-wrap)
- `columns`: equal CSS grid columns when mode is `grid`
- `gap`: Tailwind spacing scale (`gap * 4` px); default `3`
- `align`: CSS `align-items` (`start`|`center`|`end`|`stretch`|`baseline`); omit for browser default
- per-node `layout.span`: `grid-column: span N`
- per-node `layout.width` / `minWidth` / `maxWidth`: resolved CSS strings (e.g. `fit-content`, `50%`, `80px`)
- per-node `layout.grow` / `shrink`: booleans → `flex-grow` / `flex-shrink` `1`/`0` (Row children only)

A `group` may set `indented`: omitted/`true` = left-border rail (toggle `WithVisibility`); `false` = flat children (value-equals, comparator, RepeatFor). `grid` still wins over chrome. Legacy RepeatFor documents without `indented` stay flat via `id` prefix `repeat_`.

---

## Example payloads

### 1. Toggle (plain) + ShowWhen on a slider

```json
{
  "type": "toggle",
  "id": "optional_limit_enabled",
  "label": "Enable optional limit",
  "saveKey": "optional_limit_enabled",
  "hint": "Enable and set a limit (0–100).",
  "defaultValue": false
}
```

```json
{
  "type": "slider",
  "id": "optional_limit",
  "label": "Optional limit",
  "saveKey": "optional_limit",
  "min": 0,
  "max": 100,
  "defaultValue": 50,
  "visibility": { "saveKey": "optional_limit_enabled", "equals": true }
}
```

### 2. Dropdown (pair-value + refreshable)

```json
{
  "type": "dropdown",
  "id": "device_display",
  "label": "Device (pair value)",
  "saveKey": "device_display",
  "valueSaveKey": "device_id",
  "refreshable": true,
  "defaultByValue": "id2",
  "options": [
    { "value": "id1", "display": "Device A" },
    { "value": "id2", "display": "Device B" },
    { "value": "id3", "display": "Device C" }
  ]
}
```

Optional flags (omitted when false): `searchable`, `allowCustom`, `multiple`. `multiple` stores `string[]` in `saveKey` and cannot be combined with `valueSaveKey`.

### 3. Pill input (nested schema — not a Panel)

```json
{
  "type": "pill-input",
  "id": "test_items",
  "label": "Test items",
  "saveKey": "test_items",
  "itemTemplate": [
    {
      "type": "title",
      "text": "Item: {name}"
    },
    {
      "type": "toggle",
      "label": "Enabled",
      "saveKey": "{name}_enabled",
      "defaultValue": true
    },
    {
      "type": "slider",
      "label": "Value",
      "saveKey": "{name}_value",
      "min": 0,
      "max": 100,
      "defaultValue": 50
    }
  ],
  "items": [
    {
      "name": "Alpha",
      "children": [
        { "type": "title", "text": "Item: Alpha" },
        { "type": "toggle", "label": "Enabled", "saveKey": "Alpha_enabled", "defaultValue": true },
        { "type": "slider", "label": "Value", "saveKey": "Alpha_value", "min": 0, "max": 100, "defaultValue": 50 }
      ]
    }
  ]
}
```

### 4. RPC request / response (dropdown refresh)

Request (web → host):

```json
{
  "kind": "request",
  "id": 42,
  "method": "dropdown.refresh",
  "params": { "saveKey": "dropdown_choice" }
}
```

Response (host → web):

```json
{
  "kind": "response",
  "id": 42,
  "result": {
    "options": [
      { "value": "Option A", "display": "Option A" },
      { "value": "Option B", "display": "Option B" },
      { "value": "Option C", "display": "Option C" },
      { "value": "Option D (refreshed)", "display": "Option D (refreshed)" }
    ]
  }
}
```

### 5. Push event (progress)

```json
{
  "kind": "event",
  "event": "progress",
  "payload": {
    "id": "progress-1",
    "title": "Progress Test",
    "message": "Simulating work...",
    "current": 4,
    "total": 10,
    "percent": 40,
    "done": false
  }
}
```

### 6. Bootstrap (abbreviated)

```json
{
  "kind": "event",
  "event": "bootstrap",
  "payload": {
    "title": "Example Extension",
    "version": "1.0",
    "colorScheme": "dark",
    "sections": [
      {
        "id": "General",
        "title": "General settings",
        "children": [
          { "type": "description", "text": "Configure the extension." },
          {
            "type": "number-input",
            "label": "Rate",
            "saveKey": "rate_value",
            "valueType": "double",
            "min": 0.1,
            "max": 10,
            "step": 0.1,
            "defaultValue": 1,
            "stepper": true
          }
        ]
      }
    ],
    "values": {
      "rate_value": 1,
      "test_items": ["Alpha"]
    }
  }
}
```

---

## Value storage notes

| Control | Typical stored type |
|---------|---------------------|
| toggle (plain) | `boolean` |
| toggle (exclusive) | `number[]` selected indices |
| textbox / filepath / color / duration | `string` |
| slider / integer number-input | `number` (int) |
| number-input double/float | `number` |
| dropdown | `string` (display); with pair-value also value in `valueSaveKey`; `multiple` → `string[]` |
| dynamic-textboxes / pill-input | `string[]` |
| repeatable-rows | array of objects under `saveKey` |
| button / description / title / separator / update-notice | no settings value |

Nested keys follow existing conventions: `"settings.timeout"`, `"rows[0].name"`.

---

## Extension update modal

Update notices are **not** in bootstrap. After the window is ready, the host may push:

```json
{
  "kind": "event",
  "event": "update.available",
  "payload": {
    "noticeId": "extension-update",
    "currentVersion": "1.0.0",
    "latestVersion": "1.1.0",
    "releaseNotes": "Bug fixes.",
    "repo": "example-org/example-extension",
    "mode": "notify",
    "releasePageUrl": "https://example.test/example-org/example-extension/releases/tag/v1.1.0",
    "updateGuideUrl": "https://example.test/docs/updating"
  }
}
```

The web UI shows an in-menu modal (How to update / Later / Don't ask for this version).

| RPC | Params | Host behavior |
|-----|--------|---------------|
| `update.dismiss` | `{ noticeId?, reason?: "later" \| "ignoreVersion", version? }` | `later` closes modal only; `ignoreVersion` persists version in `FluentConfig_UpdatePrefs_{title}` |
| `update.stage` | `{ downloadUrl, noticeId? }` | Test/tooling only — menu no longer stages DLL updates |
| `shell.openUrl` | `{ url }` | Opens How to update / release URL |

FluentConfig.dll install/update is handled by the copy-paste **DllCheck** action, not the menu.

---

## Performance marks (`perf.mark`)

Web → host RPC `perf.mark` with `{ "name": "<mark>" }`. Meaningful only when the host is compiled with `FC_PERF_TRACE`.

| Contract | Detail |
|----------|--------|
| Params | `{ name: string }` — arbitrary mark names are recorded as milestones |
| Summary close | Only `web-ready` ends the run: logs the summary and writes CPH global `FluentConfig_PerfLast` |
| Other marks | Recorded immediately; do not close the summary |

### Standard web mark names (in order)

| name | When |
|------|------|
| `script-start` | Top of the web entry module (before Svelte `mount`) |
| `svelte-mount` | `App` `onMount` (before bridge/RPC bind) |
| `rpc-bound` | After `RpcClient` is bound to the store / perf helper |
| `bootstrap-received` | Host `bootstrap` push applied |
| `web-ready` | After first paint (`requestAnimationFrame`); closes summary |

### `FluentConfig_PerfLast` JSON (CPH global)

Written when `web-ready` closes the summary:

```json
{
  "cold": true,
  "totalMs": 1234,
  "milestones": [
    { "name": "Settings.Load", "ms": 12, "kind": "phase" },
    { "name": "script-start", "ms": 800, "kind": "mark" },
    { "name": "web-ready", "ms": 1100, "kind": "mark" }
  ]
}
```

- `cold` — `true` on the first `Show` in the process; later opens are warm (`false`)
- `kind: "phase"` — host phase **duration** (ms for that phase)
- `kind: "mark"` — **elapsed ms from tracer Start** (absolute waterfall time)

---

## File map

| Side | Path |
|------|------|
| C# DTOs | `Host/Protocol/` |
| TS types | `web/src/protocol/` |
| This doc | `PROTOCOL.md` |
