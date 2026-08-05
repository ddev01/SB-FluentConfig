# Extension updates (FluentConfig)

FluentConfig distinguishes **two update paths**. They behave differently on purpose.

## FluentConfig self-update (automatic)

FluentConfig's own DLL can update itself when a newer release is published on GitHub.

```csharp
FluentConfigUi.ShowOrFocus(CPH, ExtensionInfo.Title, ExtensionInfo.Version, ui => ui
    .WithUpdateCheck("example-org/fluentconfig", ExtensionInfo.Version)
    .Section("Settings", "Settings", s => s
        .Toggle("Enabled", "enabled"))
);
```

- Uses GitHub `releases/latest` (one cheap fetch **after** the window is shown — never blocks open).
- Targets **only** `FluentConfig.dll` — the path is hardcoded inside the host; callers cannot override it.
- When an update is available, the UI shows an update notice in **`self`** mode (via `update.available`). The user can stage the download; after Streamer.bot exits, `FluentConfig.UpdaterHelper.exe` swaps the staged file into place and relaunches.

This path is for the FluentConfig framework itself, not for third-party extensions.

## Extension updates (notify-only)

Streamer.bot extensions are **action bundles** (imports), not a single swappable DLL. FluentConfig does **not** auto-install extension updates — that would require a fragile, author-specific swap mechanism.

Instead, use **tag-prefix releases** on your GitHub repo and surface a **notify-only** banner that links to the release page (same idea as Tawmae's per-extension version checks).

### 1. Publish tag-prefix releases

Create GitHub releases whose tags follow:

```text
{tagPrefix}-v{semver}
```

Example for a Spotify extension with prefix `spotify`:

| Tag | Meaning |
|-----|---------|
| `spotify-v1.0.0` | Initial release |
| `spotify-v1.2.3` | Newer release |

FluentConfig lists `/repos/{owner}/{repo}/releases`, filters tags starting with `{tagPrefix}-v`, and picks the highest semver. The banner links to that release's `html_url` (real changelog page on GitHub).

### 2. Wire the notify banner in your config action

Recommended: a small static `ExtensionInfo` class with shared constants.

```csharp
static class ExtensionInfo
{
    public const string Title = "My Extension";
    public const string Version = "1.0.0";
    public const string IconPath = null; // optional .ico path
    public const string Repo = "example-org/my-extension";
    public const string TagPrefix = "my-extension";
}

public bool Execute()
{
    FluentConfigUi.ShowOrFocus(CPH, ExtensionInfo.Title, ExtensionInfo.Version, ui => ui
        .WithExtensionUpdateNotice(ExtensionInfo.Repo, ExtensionInfo.TagPrefix, ExtensionInfo.Version)
        .Section("Settings", "Settings", s => s
            .Intro("Configure **My Extension**.")
            .Toggle("Enabled", "enabled"))
    );
    return true;
}
```

- `WithExtensionUpdateNotice` runs a background check when the UI opens.
- If a newer `{tagPrefix}-v*` release exists, an update notice appears in **`notify`** mode.
- The **Update** control opens the release page in the system browser (`shell.openUrl`) — no download, no staging, no DLL swap.

### 3. Manual re-import (author / user flow)

When users click through to GitHub and download a new `.streamBot` (or your packaged import):

1. **Export or note current settings** if your extension stores data outside Streamer.bot globals (optional but kind to users).
2. In Streamer.bot, **import** the new action bundle (same as a fresh install).
3. Replace or disable the old action group so only one copy runs.
4. Re-open the FluentConfig UI — settings stored in CPH globals under `FluentConfig_Settings_{title}` are preserved as long as the **title** string unchanged.

There is no one-click in-app upgrade for extensions by design. Document this flow in your own README or in-action `.Intro()` text if your audience needs hand-holding.

## Quick comparison

| | FluentConfig self-update | Extension notify |
|--|--------------------------|------------------|
| API | `.WithUpdateCheck(repo, version)` | `.WithExtensionUpdateNotice(repo, tagPrefix, version)` |
| GitHub lookup | `releases/latest` | Paginated `releases` + tag prefix |
| UI mode | `self` | `notify` |
| User action | Stage + restart Streamer.bot | Open release page → manual re-import |
| Download target | `FluentConfig.dll` only | None (link-out only) |

## Low-level API (no UI)

```csharp
// Self-update probe (returns download URL when newer)
var self = GitHubUpdater.CheckForUpdate("example-org/fluentconfig", "1.0.0");

// Extension probe (returns releasePageUrl when newer)
var ext = GitHubUpdater.CheckForTaggedRelease("example-org/my-extension", "my-extension", "1.0.0");
```

Both return `null` on network or parse failure — failures are silent by design so a bad connection never blocks opening settings.

## See also

- [PLUGIN_DEVELOPER_GUIDE.md](PLUGIN_DEVELOPER_GUIDE.md) — general authoring
- [REFERENCES.md](REFERENCES.md) — Streamer.bot assembly references
- `examples/CompleteExample.cs` — `ShowOrFocus`, `ConnectionStatus`, rich `Intro()`
