# I1 — Combined 0.2 author-facing breaks (and deferred)

Assembled from `docs/internal/refactor-notes/s0.md` … `w6.md`. Stream notes stay in this folder for W7. S0 had no author-facing breaks.

## Author-facing breaks (0.2 wave)

### Settings identity (W3)

- `Fc.Open(cph, title, version, build)` → `Fc.Open(cph, id, title, version, build)`. The same extra `id` argument is on `Create`, `ShowOrFocus`, `LoadSettings`, `Get/SetSetting`, `SaveSettings`, `HasSavedSettings`, `DataKeyFor`, `Load/SaveData`, `Get/SetData`, `SettingsKeyFor`, `KeyFor`.
- Menu id must match `[a-z][a-z0-9_]*`. Titles like `"Foo"` / `"Foo!"` / `"Foo "` are no longer valid identity keys.
- Settings globals are `{id}_settings` / `{id}_data`. The old `FluentConfig_Settings_{title}` key is not read. Owner menus: `FluentConfig/scripts/RenameSettingsKeys.ps1`, then delete the script.
- `Fc.AlreadyOpened` default id is `fluentconfig` (the old `"FluentConfig"` default is not a legal id).

### DSL builders (W2)

- Naming wrapper types (`SectionFluentWrapper`, `PanelFluentWrapper`, `FluentWrapperBase`) no longer compiles. Authors who only wrote `s => s.Toggle()...` are source-compatible; this is a binary break.
- `WithVisibility(key, build, inverted: true)` is gone. Use `WithVisibilityWhenOff(key, build)` or `WithVisibility(When.Off(key), build)`.
- `.DefaultIndex()` on a refresh-only dropdown throws. Use `.DefaultByValue("...")`.
- Duplicate `saveKey` / `valueSaveKey` across a document throws at build.
- `RepeatFor` without a declared driver `.Range` throws (no implicit 0–100). Max above 32 throws.
- `.Hint` / `.Size` / `.ShowWhen*` after a structural call (`Intro`, `Separator`, …) throw instead of no-op.

### Close machine (W1)

- In-app `window.close` with `alreadyConfirmed: false` no longer closes the HWND unconditionally. The host asks the web via `window.closeRequested` first. Send `alreadyConfirmed: true` after a completed discard check (the in-app Exit path already does).

### Updater / versioning (W6)

- Web → host `update.stage` is gone. The menu cannot stage a DLL; FluentConfig.dll updates stay on the copy-paste DllCheck action. `GitHubUpdater.StageUpdate` remains a host API.
- `UpdateAvailablePayload.mode` / update-notice `mode` is notify-only (`'self'` is not a legal value).
- Extension update prefs (`FluentConfig_UpdatePrefs_*`) key on the explicit menu id, not the window title.
- Installed InformationalVersion is `0.1.0-beta.2` without a `+githash` suffix. `minFluentConfig: "0.1.0-beta.2"` gates now see the real version.

### Web (W4 / W5)

None author-facing. `defaultIndex` is no longer a display fallback (tied to W2 throwing on refresh-only `.DefaultIndex()`).

## I1 integration fixes

- `UpdateNoticeControl.svelte` did not reference `update.stage` or `mode: 'self'` (notify-only Later / How to update / Don't ask). Protocol TS, mockBridge, and host RPC switch already agreed: no `UpdateStage`.
- Frozen `Create(cph, id, title, version)` call sites compile. Public `inverted:` is gone from `WithVisibility`; `When` is the author API. `DurationParsing` remains an experimental helper, not the UI/save grammar.
- Window-manager tests now share the `StaWpf` collection so W1 `TakeAllWindows` cannot empty the registry under `ConcurrentIsOpen`.
- W5 `SelectListbox` / `SelectChips` were re-inlined into `SelectMenu.svelte` (pure `selectMenuState.ts` kept) so the Release single-file HTML fits the 180,000-byte gate. List shadow reuses the existing App.svelte arbitrary class.

## Gate (this tree)

- xunit: **323 passed** / 0 failed (`FluentConfig.Tests`, Debug).
- vitest: **64 passed** / 0 failed.
- `bun run check` (svelte-check + tsc): **0 errors, 0 warnings**.
- Release `web/dist/index.html`: **179,770 bytes** (gate **180,000**).

## I2 prerequisites (not implemented here)

All three look met:

1. W2 rejects `.DefaultIndex()` on refresh-only dropdowns (`RefreshOnlyDropdown_DefaultIndex_Throws`).
2. W3 seeds `defaultByValue` in the empty-options collector branch (`valueSaveKey` when pair-valued).
3. W5 `refreshCache.consumeOnce` still returns true on the first call per session (`DropdownControl` / `FilepathControl` mount).

I2 can proceed: remove the eager `refreshFn()` in `BuildDropdown`, invert `RefreshOnlyDropdown_SeedsInitialOptions`.

## Deferred (not I1)

- `$state.raw` on the values blob: left as `$state`. vitest cannot import runes modules; no store-level test covers the flip.
- Eager author `Refresh()` still runs at schema build (I2).
- Docs / examples (`inverted:` comments, `Fc.Open` signatures, PROTOCOL `update.stage` row) — W7.
- Number stepper as a SelectMenu addon; `SliderControl` has no `step`.
- `RepeatableRowsControl.applyDefault` still seeds `defaultIndex` for new rows.
- `applySchemaPatch` still does not walk `itemTemplate` / `repeatable-rows`.
- `openConfirm` still stacks by resolving the previous dialog with `false`.
