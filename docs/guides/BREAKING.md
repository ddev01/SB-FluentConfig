# Author-facing breaks (0.2)

All 0.2 authoring breaks land in one wave. This is the list to read when updating an existing menu. Wire shapes (`inverted` on a visibility condition, `dropdown.refresh`, and so on) are unchanged unless noted.

## Settings identity

- `Fc.Open(cph, title, version, build)` → `Fc.Open(cph, id, title, version, build)`. The same extra `id` argument is on `Create`, `ShowOrFocus`, `LoadSettings`, `Get`/`SetSetting`, `SaveSettings`, `HasSavedSettings`, `DataKeyFor`, `Load`/`SaveData`, `Get`/`SetData`, `SettingsKeyFor`, and `KeyFor`.
- Menu id must match `[a-z][a-z0-9_]*` (start with a lowercase letter). The window **title is a label**; it is not persistence or registry identity. Titles like `"Foo"` / `"Foo!"` / `"Foo "` are no longer valid keys.
- Settings globals are `{id}_settings` / `{id}_data`. The old `FluentConfig_Settings_{title}` key is not read. A title rename no longer orphans settings if the id stays the same.
- `Fc.AlreadyOpened` default id is `fluentconfig` (the old `"FluentConfig"` default is not a legal id).
- Owner menus that still live under a slugified title: `FluentConfig/scripts/RenameSettingsKeys.ps1` copies `{old_slug}_settings|_data` → `{new_id}_*` in a globals JSON dump. It is throwaway, not shipped migration — delete the script after use.

```csharp
Fc.Open(CPH, "my_extension", "My Extension", "1.0", ui => ui
    .Section("Settings", "settings", s => s
        .Toggle("Enabled", "enabled")));

var settings = Fc.LoadSettings<MySettings>(CPH, "my_extension");
```

## DSL builders

- Naming wrapper types (`SectionFluentWrapper`, `PanelFluentWrapper`, `FluentWrapperBase`) no longer compiles. Authors who only wrote `s => s.Toggle()...` are source-compatible; this is a binary break.
- `WithVisibility(key, build, inverted: true)` is gone. Use `WithVisibilityWhenOff(key, build)` or `WithVisibility(When.Off(key), build)`. The **wire** still has `inverted` on `VisibilityCondition`; only the public C# trailing bool is gone. See [VISIBILITY.md](VISIBILITY.md).
- `.DefaultIndex()` on a refresh-only dropdown (no static `.Options()`) throws. Use `.DefaultByValue("...")`.
- Duplicate `saveKey` / `valueSaveKey` across a document throws at build.
- `RepeatFor` without a declared driver `.Range` throws (no implicit 0–100). Max above 32 throws unless you need a smaller explicit `max:`.
- `.Hint` / `.Size` / `.ShowWhen*` after a structural call (`Intro`, `Separator`, …) throw instead of no-op.

## Refresh-only dropdowns

- Author `.Refresh(() => …)` (string[] or pair enumerable) **no longer runs during schema build**. Side effects at open (logging, pre-warming CPH) move to the web’s first-mount `dropdown.refresh` (once per saveKey).
- Bootstrap schema for refresh-only dropdowns has empty/omitted `options`. Do not treat `UiDocument` options as populated at build time.
- First fetch is the web’s once-per-saveKey `dropdown.refresh` (`refreshCache`). Tab remounts do not re-fire.
- Residual: `SelectMenu` may show the placeholder until options arrive. If Refresh returns empty (Twitch offline), mount refresh still **clears** an unmatched default.

```csharp
.Dropdown("Reward", "reward_display")
    .WithPairValue("reward_id")
    .Refresh(() => new[] { ("reward-guid", "Channel Points Reward") }) // first dropdown.refresh, not at Build
    .DefaultByValue("reward-guid")
```

## Close machine

- In-app `window.close` with `alreadyConfirmed: false` (or omitted) no longer closes the HWND unconditionally. The host asks the web via `window.closeRequested` first. Send `alreadyConfirmed: true` after a completed discard check (the in-app Exit path already does).

## Updater / versioning

- Web → host `update.stage` is gone. The menu cannot stage a DLL; `FluentConfig.dll` updates stay on the copy-paste DllCheck action. `GitHubUpdater.StageUpdate` remains a host API.
- `UpdateAvailablePayload.mode` / update-notice `mode` is notify-only (`'self'` is not a legal value).
- Extension update prefs (`FluentConfig_UpdatePrefs_{id}`) key on the explicit menu id, not the window title.
- Installed `InformationalVersion` is the csproj value (currently `0.1.0-beta.2`) without a `+githash` suffix. `minFluentConfig: "0.1.0-beta.2"` gates now see the real version.

See [UPDATES.md](UPDATES.md), [SECURITY.md](SECURITY.md), and [../../FluentConfig/Host/RELEASE.md](../../FluentConfig/Host/RELEASE.md).
