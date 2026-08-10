# FluentConfig — Code Review & Refactor Plan (Production Readiness)

**Date:** 2026-08-05 · **Branch reviewed:** `develop` @ `6f8dd6d`

## Method

Five parallel deep-dive reviews were run against non-overlapping slices of the codebase, each with the relevant architecture docs (`ARCHITECTURE.md`, `PROTOCOL.md`, `PACKAGING.md`, `EXTENSION_UPDATES.md`) as context:

| Slice | Scope |
|-------|-------|
| A — Host lifecycle & core | `Host/App/`, `Host/Core/`, `FluentConfigUi.cs` |
| B — Builders & protocol | `Host/Builders/`, `Host/Protocol/` |
| C — Host window, native, updater | `Host/Host/`, `Host/Native/`, `Host/Updater/`, `Host/Helpers/`, `UpdaterHelper/` |
| D — Web UI | `web/src/**` (Svelte 5 + TS) |
| E — Tests, scripts, docs | `FluentConfig.Tests/`, `scripts/`, `examples/`, cross-doc consistency, privacy scan |

Findings below are merged, deduplicated, and re-prioritized across the whole codebase rather than per-slice. Every item cites `File:Line`. This document supersedes the individual slice reports for planning purposes.

**Overall assessment:** the codebase is in noticeably good shape for a project that hasn't shipped v1 yet — architecture boundaries (no WPF leaking into the wire protocol, camelCase JSON, schema-driven web) are respected almost everywhere, Svelte 5 idioms are used consistently, and there's already a real (if uneven) test suite. It is **not** a rewrite candidate. The work below is real cleanup, not damage control: a handful of security/privacy items must be fixed before any public release, a set of correctness bugs should be fixed before relying on this in production plugins, and the rest is maintainability debt that can be paid down incrementally.

---

## P0 — Must fix before any public release (security & privacy)

These are the only items that should block shipping. All are small, contained fixes.

| # | File:Line | Issue | Why it matters | Action |
|---|-----------|-------|-----------------|--------|
| 1 | `Host/App/FluentConfigSession.cs:25` | Real GitHub org/repo hardcoded as `RepoUrl` const, injected into every UI footer | Violates the repo's own privacy convention (real identifiers must not be committed defaults); also means every consumer's UI links to the framework author's repo regardless of context | Move to a build-time property (`-p:FluentConfigRepoUrl=...`) with a generic fallback (e.g. `https://example.test/fluentconfig`), or make it settable via `FluentConfigUi` |
| 2 | `Host/Host.csproj:26`, `FluentConfig.Tests/FluentConfig.Tests.csproj:8-10`, `SmokeHost/SmokeHost.csproj:10-11`, `SmokeHost/Program.cs:141`, `scripts/Redeploy.ps1:52`, `Host/PACKAGING.md:29` | Developer-specific Streamer.bot install path hardcoded as a build/deploy default in 6 places | Machine-specific path baked into shared, committed build logic; breaks on every other machine and is exactly what the privacy rule calls out | Keep only generic fallbacks already present (Desktop portable, WinGet `%LOCALAPPDATA%`) plus `-p:StreamerBotPath` / an env var; delete the hardcoded drive path everywhere, including the doc |
| 3 | `Host/App/FluentConfigSession.cs` `HandleUpdateStage` (~737-750) + `Host/Updater/GitHubUpdater.cs:295-298` | `update.stage` RPC downloads whatever `downloadUrl` the **web side** sends, with no validation against the pending update record, no host allowlist, no HTTPS enforcement | A compromised/buggy web bundle (or a future XSS in rendered rich text — see P1 richText item) could stage arbitrary bytes as `FluentConfig.dll.update`, which the swap helper then installs as the framework DLL | Validate the incoming URL equals `_pendingUpdate.DownloadUrl` (server-known value) before downloading; additionally enforce `https://` + an allowlist of GitHub download hosts |
| 4 | `Host/Updater/GitHubUpdater.cs:307-324` (`PickAssetUrl`) | Falls back to `zipball_url` (a source archive) when a release has no binary assets, and that URL flows into the same self-update download path | Self-update can stage a `.zip` as if it were `FluentConfig.dll` | Never use `zipball_url` for self-update; require a `.dll`-named asset or treat "no asset" as "no update available" |
| 5 | `Host/Updater/GitHubUpdater.cs:295-298` (`DownloadToFile`) | No size cap or integrity check on downloaded update bytes | OOM risk on a huge/misconfigured asset; no way to detect a corrupted or tampered download before installing it | Stream download with a sane max-size cap; consider a minimum PE-header sanity check before staging |
| 6 | `UpdaterHelper/Program.cs:53-77` | Swap sequence is `move-to-backup → delete target → move-staged-to-target`, not atomic; a failure between steps can leave `FluentConfig.dll` deleted with no automatic restore | Worst case: an interrupted update permanently removes the framework DLL from a user's Streamer.bot install | Rewrite as "write staged file next to target, then a single `File.Replace`/rename" pattern with restore-from-backup on any exception |

**Effort for all of P0 together: S–M, roughly 1 focused day.** None of these require architectural change.

---

## P1 — Correctness bugs (fix before relying on this for real plugins)

These won't necessarily crash anything today, but each is a real, reachable bug.

### Host / DSL

| File:Line | Issue | Action |
|-----------|-------|--------|
| `Host/Builders/PendingControl.cs:359-360` (vs `:174-183`) | `.Range(double, double)` writes fields that `BuildNode()` never reads for `Kind.Slider` (only int range is read) — a slider built with a double range silently gets the default 0–100 | Wire double ranges into slider output, or throw when the double overload is used on a slider |
| `Host/Builders/PendingControl.cs` (all option setters + `BuildNode` switch) | Every fluent option (`.Password()`, `.WithExclusive()`, etc.) is stored on shared fields regardless of the control kind currently being built; irrelevant options are silently dropped at flush with no error | Add kind-guards that throw a clear `InvalidOperationException` when an option is applied to an incompatible control kind |
| `Host/Builders/PendingControl.cs:92-96` + all `BuildNode()` paths | Null/empty `saveKey` is accepted everywhere; breaks persistence, RPC registration, and web element ids downstream with no signal at the call site | Validate saveKey (non-null, non-empty, unique within its section) at `Begin*`/`Flush()` and throw immediately |
| `Host/Builders/PendingControl.cs:268-281` | Exclusive toggle silently ignores a plain `.Default(bool)` (only `DefaultIndex`/`DefaultIndices` are honored) | Throw when `.Default(bool)` is combined with `.WithExclusive()`, or map it intelligently |
| `Host/App/FluentConfigSession.cs:171-216` (`Show`) | No guard against calling `Show()` twice on the same session — orphans the previous window and overwrites the window-manager registry entry | Add an idempotency/re-entrancy guard; close-and-replace or no-op on repeat `Show()` |
| `Host/Core/FluentConfigWindowManager.cs:45-57` | `AlreadyOpened()` returns `true` even when focusing a stale window throws (caught and ignored) — caller thinks a window is open and skips creating a new one, so the user sees nothing | On focus failure, remove the stale registry entry and return `false` so a fresh window gets created |
| `Host/App/FluentConfigUi.cs:101-115` (`ShowOrFocus`) | Check-then-register is not atomic (two separate lock acquisitions) | Hold one lock across check + register, or register a placeholder before `Show()` begins |
| `Host/Core/SettingsPathHelper.cs:127-128` (`RemoveNestedValue`) | Nulls array slots instead of splicing them out — removing a pill row can leave `null` holes in the array | Splice/remove the element instead of nulling it; add a regression test |
| `Host/Core/SettingsPathHelper.cs:64-76` (`SetNestedValue`) | Silently no-ops on a type mismatch (e.g. writing a nested path into a scalar) instead of erroring | Surface an error (exception in debug, logged warning at minimum) instead of a silent no-op |
| `Host/App/FluentConfigSession.cs` (`HandlePillChanged`, ~702-735) | Author-supplied `OnAdded`/`OnRemoved` pill callbacks run synchronously on the UI thread inside `WebMessageReceived`, before the RPC response is sent — a slow callback blocks the whole web UI | Defer through `Dispatcher.BeginInvoke`, matching the existing pattern used for button click handlers |
| `Host/Updater/UpdateHelperLauncher.cs:35-46` | `Process.Start`'s return isn't checked; the caller reports `{ staged: true }` to the web UI even when the helper process never actually launched | Check the return value / catch launch failures and propagate a real error to the `update.stage` RPC response |
| `Host/Updater/GitHubUpdater.cs:339-354` (`Normalize`/`IsNewer`) | Semver comparison strips all non-digit characters, so `1.0.0-rc1` compares equal to `1.0.0` — pre-release tags are invisible to the update check | Document the limitation explicitly in `EXTENSION_UPDATES.md`, or implement basic pre-release-aware comparison |
| `Host/Host/FluentConfigHostWindow.cs` (`GetSharedEnvironmentAsync`, ~396-404) | If the one-time `CoreWebView2Environment.CreateAsync()` faults, every later window open re-awaits the same faulted task — permanently broken until the whole process restarts | On fault, clear the cached task under lock so a later attempt can retry |

### Web UI

| File:Line | Issue | Action |
|-----------|-------|--------|
| `web/src/rpc/client.ts:67-82` | RPC requests never time out — if the host never replies, any control awaiting the promise (button click, filepath browse) stays busy/disabled forever | Add a configurable timeout that rejects with a typed error; ensure callers clear busy state in `finally` |
| `web/src/lib/controls/PillInputControl.svelte:67-97` | Pill add/remove updates local values/schema **before** the `pill.changed` RPC resolves, with no rollback on failure — UI and host settings can permanently diverge after a failed request | Apply the local mutation only after RPC success, or roll back on error |
| `web/src/store/app.svelte.ts:364-368` (`applyValuesPatch`) | Updates `values` but not `savedValues`, so any host-pushed `values.patch` makes the form appear dirty ("Unsaved changes") even without a user edit. Host doesn't send this yet, but the handler is live | Mirror patches into `savedValues` too (or define patch semantics explicitly) before this is ever exercised in production |
| `web/src/lib/controls/SliderControl.svelte:13-19` | Doesn't clamp to min/max on read or input, unlike `NumberInputControl` | Clamp consistently with the number-input control |
| `web/src/lib/controls/DropdownControl.svelte:18-44` | After `dropdown.refresh`, if the current selection isn't in the new options list, the UI shows a placeholder while the underlying saved value(s) stay stale | Reconcile or explicitly clear the stored selection when it's no longer valid post-refresh |
| `web/src/lib/controls/DynamicTextboxesControl.svelte:49`, `RepeatableRowsControl.svelte:98` | `{#each}` keyed by array index — removing/reordering an item can rebind the wrong DOM node/input state to a different logical row | Key by a stable id or content fingerprint instead of index |
| `web/src/lib/visibility.ts:11-14` | Visibility checks only read `values`, never falling back to `node.defaultValue` — latent bug if a key is ever absent from the values blob (future `values.patch`/`schema.patch` paths) | Resolve an "effective value" (wire value ?? schema default) before evaluating the condition |

**Effort for P1: M, roughly 2-4 days** including the added tests called out in the test plan below.

---

## P2 — Architecture & maintainability cleanup

Not bugs today, but debt that will make future changes riskier and slower. Recommended before calling the codebase "production ready" in the sense of being comfortable handing it to another contributor.

| File:Line | Issue | Action | Effort |
|-----------|-------|--------|--------|
| `Host/App/FluentConfigSession.cs` (~900+ lines) | God-class: window lifecycle, ~10 RPC handlers, updater staging, and host-exit teardown all in one file | Split into `SessionLifecycle`, `SessionRpcHandlers`, `DeferredUpdateService` (or similar) sharing session state via a small internal context object | L |
| `Host/Builders/PendingControl.cs` (~377-396 lines) | Single class + one large switch handles all 12 control kinds' state and serialization | Split per control kind (matches the direction `ARCHITECTURE.md` originally envisioned), or at minimum extract one `Build*` method per kind into partial classes/files | L |
| `Host/Builders/SectionBuilder.cs:64-110`, `Host/Builders/PanelBuilder.cs:51-96` | `WithVisibility`/`WithVisibilityWhenOff`/`WithRepeatableRows` are duplicated near-verbatim between section and panel builders (~45 lines) | Extract a shared static helper operating on `SchemaNodeList` + session context | S–M |
| `Host/Builders/SectionFluentWrapper.cs:27-31`, `Host/Builders/PanelFluentWrapper.cs:23-27` | Still expose a positional `bool inverted` overload on `WithVisibility` — the exact DSL smell `ARCHITECTURE.md`/`PROTOCOL.md` call out as required to fix, even though the underlying builders did it correctly | Remove the positional-bool overload from the wrappers; keep `WithVisibilityWhenOff` + named `inverted:` only | S |
| `Host/Builders/SectionFluentWrapper.cs` (no `ConnectionStatus`) | `ConnectionStatus()` only exists on the raw `SectionBuilder`, not the wrapper returned after the first control — `.Toggle(...).ConnectionStatus(...)` doesn't compile, an easy trap for authors | Add `ConnectionStatus` to `SectionFluentWrapper` to mirror `Intro`/`Separator` | S |
| `Host/Builders/FluentWrapperBase.cs:22-25` (`Option()`) | Silently no-ops when called with no pending control (e.g. after a structural call like `Intro`/`Separator`) instead of erroring | Throw a clear exception instead of silently dropping the option | S |
| `Host/Builders/PendingControl.cs:186-196` (button id) | Button ids are a fresh GUID per build, not stable — breaks `button.click` correlation across any future `schema.patch` re-render | Derive a stable id (label-based, like `ConnectionStatus` already does) or accept an optional explicit id | S |
| `Host/Host/HostBridge.cs:131-165` (`SendRequestAndWait`) | On timeout, the pending `TaskCompletionSource` is removed from the dictionary but never completed | Complete it (e.g. `TrySetResult(null)`) before removing, to avoid a lingering unresolved task | S |
| `Host/Helpers/InputValidation.cs`, `Host/Helpers/DurationParsing.cs` | Clean, well-tested pure helpers that are **not called from any production code path** — ported from the old WPF tree but never wired into the new RPC/save flow | Either wire them into the relevant save/validation paths (duration input, number input) or explicitly mark them `internal`/experimental until they are, so "tested but dead" doesn't look like an oversight | M |
| `web/src/lib/controls/SchemaNodeView.svelte:34-71` | Type-dispatch chain has no `{:else}` — an unrecognized/future node `type` renders nothing with zero signal | Add a dev-mode console warning + a minimal visible fallback stub | S |
| `web/src/lib/controls/PillInputControl.svelte:84-88` | Mutates `appStore.values` directly on remove instead of going through `setValue`/`setPath`, unlike the rest of the codebase | Add/us a `deleteValue`-style store method for consistency | S |
| `web/src/lib/ProgressOverlay.svelte:16-49` | Blocking full-screen modal has no focus trap, unlike the dialog components | Match the focus-cycling already implemented in `ConfirmDialog.svelte` | S |
| Protocol: `pill.changed` `rename` action | Defined in `PROTOCOL.md`, `Host/Protocol/RpcMethods.cs`, and `web/src/protocol/rpc.ts`, but implemented on **neither** side | Either implement it end-to-end or remove it from the documented contract to stop it looking half-finished | S |
| Protocol: `log` RPC method | Defined in C#/TS types and mocked, never called from production code on either side | Remove from the contract, or decide it's a deliberate future hook and note that in `PROTOCOL.md` | S |

**Effort for P2: L overall**, but every item is independently shippable — no need to do this in one pass.

---

## Dead code inventory (safe removal candidates)

All of these have zero call sites found by repo-wide search. Recommended: remove in one small, low-risk PR after a final grep confirms no external consumers (this is a library other DLLs may reference, so treat any **public** API removal as a minor breaking change / mark `[Obsolete]` first if you're worried about downstream plugin code).

**Host (C#):**
- `Host/Core/FluentConfigWindowManager.cs:98-104` — `SnapshotWindows()`
- `Host/App/UiContext.cs:21` — `SetPendingValues(JObject)`
- `Host/App/FluentConfigSession.cs:141-146` — `CheckSelfUpdate` / `CheckExtensionUpdateNotice` (superseded by `Configure*` + `FluentConfigUi.WithExtensionUpdateNotice`)
- `Host/Builders/FluentWrapperBase.cs:53`, `PendingControl.cs:385`, `IControlOptions.cs:35` — `ToggleDefault` (exact duplicate of `Default(bool)`)
- `Host/Builders/ControlHostBuilder.cs:89`, `FluentWrapperBase.cs:73` — `Input()`/`BeginInput` (zero usage anywhere, overlaps `Textbox`)
- `Host/Builders/SchemaNodeList.cs:14` — `Nodes` property (never read)
- `Host/Builders/FluentWrapperBase.cs:67` — `Type()` (no call sites)
- `Host/Protocol/ProtocolJson.cs:34` — `StringEnumConverter` registration (no enums exist under `Host/Protocol/`)
- `Host/Host/EmbeddedHtml.cs:2-3` — unused `using System.IO;` / `using System.Reflection;`
- `Host/Host/FluentConfigHostWindow.cs:29` — `_colorScheme` field (stored, never read after constructor — dead until a `.ColorScheme()` builder option ships)

**Web (TS/Svelte):**
- `web/src/protocol/messages.ts:94-98` — `BootstrapEvent` interface, exported but never imported
- `web/src/lib/richText.ts:21` — `parseInline`, exported but only used internally
- `web/src/lib/paths.ts:8` — `splitPath`, exported but only used internally

**Low-confidence / judgment calls (keep unless you confirm intent):**
- `Host/Native/DwmTitleBar.cs` "light"/"system" branches — currently unreachable because the session always passes `"dark"`, but this is clearly forward-looking for a future theme option, not accidental dead code
- `Host/App/FluentConfigUi.cs:86` — `plainWindow` parameter, documented as an intentionally-ignored compat shim

---

## Documentation cleanup

| File | Problem | Action |
|------|---------|--------|
| `docs/performance/COLD_START_OPTIMIZATION_HANDOFF.md` | 328-line autonomous-agent handoff whose "what's left" section is now stale — the described work shipped in `0c9d64c` (baselines already show the ~1282ms result) | Archive (e.g. move under a `docs/performance/archive/` folder) or delete; leave a two-line "cold-start pass completed, see baselines" pointer in `docs/performance/README.md` |
| `docs/performance/baselines/cold-opt-wip-minimal.md`, `cold-opt-wip-complete.md` | Labeled "uncommitted"/WIP even though the underlying commits are now on `develop` | Rename to the real short-SHA convention used by the other baseline files and update the table in `docs/performance/README.md` |
| `FluentConfig/ARCHITECTURE.md:50-175` (port classification table) | Describes per-control WPF-era builder files (e.g. `Controls/ToggleBuilder.cs`) as destinations; the real host consolidates all of that into `Host/Builders/PendingControl.cs` — misleading to a new reader trying to map the table to the actual tree | Condense the whole historical port table to a short paragraph ("legacy WPF tree removed; DSL now lives in `Host/Builders/`, consolidated per-control logic in `PendingControl.cs`") and drop the row-by-row mapping, or explicitly mark it "historical, as of Phase 1 — see current tree for actual layout" |
| `README.md` doc table (lines ~66-74) | Missing links to `docs/EXTENSION_UPDATES.md`, `docs/performance/README.md`, `FluentConfig/README.md`, `examples/README.md` — all of which exist | Extend the table or just point to `docs/README.md` as the canonical index |
| `README.md` examples table (lines ~76-83) | Missing `DevPreviewExample.cs` (already documented in `examples/README.md`) | Add the row |
| `README.md:20` | Claims settings are read back at runtime with a generic `GetValue<T>()`, but the only public `GetValue<T>()` is on `CallbackContext` (pill callbacks); button handlers use `UiContext.Pending<T>()`; most runtime reads actually go through `FluentConfig.LoadSettings<T>` / `CPH.GetGlobalVar` on the `{slug}_settings` global | Clarify which API applies in which context |
| `Host/Host/PACKAGING.md:29` | Documents the same hardcoded machine path flagged in P0 #2 as an example fallback | Fix alongside P0 #2 |

---

## Test plan

### Fix existing test debt first (before adding new coverage on top of it)

- **`FluentConfig.Tests/SettingsMergeTests.cs:12-56`** — currently tests a hand-rolled local `MergeValuesBlob()` helper, not the production `SettingsSync.ApplyAndSave` → `SettingsPathHelper.MergeValues` path (the file even has a comment admitting this is a stand-in). Rewrite to call `SettingsSync.ApplyAndSave` / `SettingsPathHelper.MergeValues` directly. **This is the single highest-value test fix in the whole review** — it's currently possible for the real save-merge logic to regress with all tests green.
- **`FluentConfig.Tests/Phase2HostSmokeTests.cs:236-263`** (`UpdaterFlowTests`) — spawns `dotnet build` at test run time (120s budget); slow and fragile in CI/sandboxes. Pre-build the helper as a test dependency instead.
- **`FluentConfig.Tests/HostBridgeTests.cs:59-61`** — reaches into `FluentConfigHostWindow`'s private `DisposeWebViewCore` via reflection; brittle to rename. Expose a proper internal test seam.

### New coverage, by priority

| Area | What's missing | Priority |
|------|-----------------|----------|
| `SettingsPathHelper.MergeValues` / `GetNestedValue` / `RemoveNestedValue` | No direct unit tests at all today | High (pairs with the merge-test fix above) |
| `PendingControl` negative cases | No test for null/empty saveKey, `.Range(double)` on a slider, wrong-kind option application, exclusive-toggle default conflict | High (once P1 validation fixes land, lock them in with tests) |
| `ProtocolContractTests.cs` | Only ~5 of 18 `SchemaNode` types have a serialize round-trip test (missing: textbox, slider, number-input, dropdown, color-picker, duration-input, filepath, dynamic-textboxes, repeatable-rows, button, description, title, separator, exclusive toggle) | Medium |
| `GitHubUpdater.PickAssetUrl` | No test for the empty-assets → zipball fallback branch — exactly the dangerous path flagged in P0 #4 | High |
| `HostBridge` | Only one thread-guard test exists; nothing for bootstrap send, timeout, `Detach`, or `CompletePendingResponse` on error | Medium |
| `FluentConfigWindowManager` / `ShowOrFocus` | No end-to-end test for the "already open but focus throws" bug fixed in P1 | High (regression guard for that fix) |
| Web: no unit test runner exists at all | `lib/diff.ts`, `lib/paths.ts`, `lib/visibility.ts`, `rpc/client.ts` are all pure, high-value logic with zero automated tests | High — recommend adding `vitest` as a first step, even before writing many tests |
| Web: `richText.ts` | Renders via `{@html}` in `DescriptionControl.svelte` — no test coverage of escaping/allowlist behavior, which is security-relevant given it renders host-supplied text | Medium-High |

---

## Suggested execution order

1. **P0 security/privacy fixes** (S–M effort, ~1 day) — do this first and in isolation; nothing else depends on it.
2. **Fix `SettingsMergeTests` to exercise the real merge path**, then land the P1 correctness fixes with tests attached as you go (saveKey validation, slider range, exclusive-toggle default, double-`Show()` guard, `AlreadyOpened` stale-window fix, pill callback deferral, RPC client timeout, pill optimistic-update rollback).
3. **P2 architecture cleanup**, largest-value-first: split `FluentConfigSession`, dedupe `SectionBuilder`/`PanelBuilder` visibility helpers, fix the wrapper `WithVisibility` positional-bool smell, add `vitest` to the web package.
4. **Dead code removal** as its own small PR (low risk, high signal-to-noise for reviewers).
5. **Docs cleanup** (archive the perf handoff, fix `ARCHITECTURE.md`'s stale table, fill in `README.md` gaps) — can run in parallel with steps 3-4.

None of this requires pausing feature work indefinitely — P0 is a short, focused pass; everything else can be picked up incrementally per the priority tiers above.
