# Extension updates (FluentConfig)

FluentConfig splits **framework DLL** updates from **extension** updates.

```text
Open settings action chain
  → DllCheck (no FluentConfig reference)  — install / daily stage FluentConfig.dll
  → Menu action (FluentConfig)            — optional extension update modal
```

## FluentConfig.dll (DllCheck action)

Streamer.bot will not compile a C# action that references `FluentConfig.dll` if the file is missing. First-install and DLL updates therefore live in a **copy-paste DllCheck** action with **zero** FluentConfig references (and **zero** Newtonsoft — only GAC framework refs like `System.Net.Http` / `System.Windows.Forms`, so imports do not need Find Refs for machine-specific paths).

See [`examples/deployment/01_DllCheck.cs`](../../examples/deployment/01_DllCheck.cs).

| Concern | Behavior |
|---------|----------|
| DLL missing | Download `.dll` (+ helper if present) from GitHub `releases/latest` |
| DLL outdated | Once per day: compare on-disk file version → GH latest → **auto-stage** + toast; swap after Streamer.bot exits |
| Min Streamer.bot | Hardcoded constant at top of DllCheck; MessageBox and abort if too old |

Edit the constants at the top of DllCheck when publishing (`UpdateRepo`, `MinStreamerBotVersion`). Matching public constants also exist on the library for docs/forks:

```csharp
FluentConfig.FluentConfig.UpdateRepo
FluentConfig.FluentConfig.MinStreamerBotVersion
FluentConfig.FluentConfig.GetVersion()
```

DllCheck remains the runtime path — it cannot load those constants from the DLL.

Deploy `FluentConfig.UpdaterHelper.exe` next to `FluentConfig.dll` (DllCheck will also try to download it from the same release).

## Extension updates (in-menu modal)

Streamer.bot extensions are **action bundles**, not a swappable DLL. FluentConfig does **not** auto-install extension updates.

Use `.WithExtensionUpdateNotice(...)` on the menu action. After the window opens, FluentConfig checks GitHub **at most once per day**, then shows an in-menu modal:

- **How to update** — opens `updateGuideUrl` (or the release page)
- **Later** — closes for this session; daily throttle may re-ask tomorrow
- **Don't ask for this version** — persists ignored version in `FluentConfig_UpdatePrefs_{title}`

### Discovery modes

**Default (one extension per repo)** — `releases/latest`:

```csharp
.WithExtensionUpdateNotice(ExtensionInfo.Repo, ExtensionInfo.Version)
```

**Monorepo / multi-extension** — tagged releases `{tagPrefix}-v{semver}`:

```csharp
.WithExtensionUpdateNotice(ExtensionInfo.Repo, ExtensionInfo.Version, tagPrefix: "spotify")
```

### Optional gates and guide URL

```csharp
static class ExtensionInfo
{
    public const string Title = "My Extension";
    public const string Version = "1.0.0";
    public const string Repo = "example-org/my-extension";
    public const string MinStreamerBot = "1.0.0";      // optional
    public const string MinFluentConfig = "0.1.0";     // optional
    public const string UpdateGuideUrl = null;           // optional; else release page
}

FluentConfigUi.ShowOrFocus(CPH, ExtensionInfo.Title, ExtensionInfo.Version, ui => ui
    .WithExtensionUpdateNotice(
        ExtensionInfo.Repo,
        ExtensionInfo.Version,
        minStreamerBot: ExtensionInfo.MinStreamerBot,
        minFluentConfig: ExtensionInfo.MinFluentConfig,
        updateGuideUrl: ExtensionInfo.UpdateGuideUrl)
    .Section("Settings", "Settings", s => s
        .Toggle("Enabled", "enabled"))
);
```

If a min gate fails, `Show()` aborts with a MessageBox **before** the window opens.

### Manual re-import

1. Export or note settings if needed.
2. Import the new action bundle in Streamer.bot.
3. Replace/disable the old action group.
4. Re-open the menu — CPH settings under `FluentConfig_Settings_{title}` are preserved when the **title** is unchanged.

## Quick comparison

| | FluentConfig.dll | Extension |
|--|------------------|-----------|
| Where | DllCheck action (no FluentConfig ref) | `.WithExtensionUpdateNotice` on menu |
| GitHub lookup | `releases/latest` + `.dll` asset | `releases/latest` **or** tagged `{prefix}-v*` |
| User action | Auto-stage; restart Streamer.bot | Modal → guide / release → manual re-import |
| Throttle | Daily (`FluentConfig_General_Settings` → `dllCheckLastUtc`) | Daily (`FluentConfig_UpdatePrefs_{title}`) |

## Low-level API (no UI)

```csharp
// DLL-oriented (requires .dll asset for UpdateAvailable)
var dll = GitHubUpdater.CheckForUpdate("example-org/SB-FluentConfig", "0.1.0");

// Extension notify on latest (no .dll required)
var latest = GitHubUpdater.CheckForLatestRelease("example-org/my-extension", "1.0.0");

// Extension notify on tag prefix
var tagged = GitHubUpdater.CheckForTaggedRelease("example-org/monorepo", "spotify", "1.0.0");
```

All return `null` on network/parse failure.

## Version comparison notes

`GitHubUpdater.IsNewer` uses a dotted-numeric compare after stripping non-digit characters (except dots). Pre-release metadata is invisible — publish distinct numeric versions for each shippable release.

## See also

- [README.md](README.md) — guide index
- [../setup/REFERENCES.md](../setup/REFERENCES.md)
- [`examples/deployment/01_DllCheck.cs`](../../examples/deployment/01_DllCheck.cs)
- [`examples/deployment/02_ExtensionUpdateNotice.cs`](../../examples/deployment/02_ExtensionUpdateNotice.cs)
