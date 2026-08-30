# Cutting a FluentConfig.dll release

Cookbook for library maintainers. Extension authors who only consume the DLL should read [docs/guides/UPDATES.md](../../docs/guides/UPDATES.md) and [docs/guides/SECURITY.md](../../docs/guides/SECURITY.md) instead.

## Version fields (keep these aligned)

`Host.csproj` sets three related values. They are not interchangeable.

| Property | Example | Who reads it |
|---------|---------|----------------|
| `FileVersion` / `AssemblyVersion` | `0.1.0.2` | **PE / DllCheck.** Dotted-numeric only. DllCheck compares on-disk file version to the GitHub tag after `GitHubUpdater.Normalize`. |
| `InformationalVersion` (and `<Version>`) | `0.1.0-beta.2` | **Display / footer / `Fc.GetVersion()` / `minFluentConfig`.** May include `-beta.N`. |
| Git tag | `v0.1.0-beta.2` | GitHub `releases/latest` and DllCheck. Leading `v` is stripped before compare. |

Rules:

1. Bump **FileVersion** on every shippable drop (even a beta). `IsNewer` is dotted-numeric after stripping non-digits; pre-release labels are invisible to the compare (`0.1.0-beta.2` and `0.1.0` normalize the same). Publish a **distinct numeric** FileVersion for each shippable release.
2. Keep `InformationalVersion` equal to the tag **without** the leading `v`. `<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>` must stay **false**. If the SDK appends `+githash`, `Normalize` used to overflow and report `0.1.0.0` — that property is the belt; stripping `+` in `Normalize` is the braces.
3. Do not put the version in the menu persistence id. An id bump orphans `{id}_settings`.

## Release assets (names are load-bearing)

DllCheck and `GitHubUpdater.PickAssetUrl` look at **asset file names**, not zipballs.

| Asset name | Required? | Role |
|------------|-----------|------|
| `FluentConfig.dll` | **Yes** | Host + builders + updater API. Without a `.dll`-named asset, DllCheck treats the release as “no update”. |
| `FluentConfig.UpdaterHelper.exe` | Strongly recommended | Detached stage-and-swap. DllCheck downloads it when present; missing helper means a manual swap after exit. |

Do **not** attach:

- `Microsoft.Web.WebView2.*.dll` / `WebView2Loader.dll` — Streamer.bot already loads them. A second copy in `dlls/` throws `FileLoadException`.
- Source zipballs as the install payload — `PickAssetUrl` ignores them on purpose.

Build Release (`-p:SkipWebBundle=false` is the default for Release). Confirm `FluentConfig/web/dist/index.html` is under the **180,000**-byte gate before tagging.

## How to cut a tag

1. Set the three version properties in `Host.csproj` as above.
2. `dotnet build FluentConfig/Host/Host.csproj -c Release`. Spot-check `Fc.GetVersion()` / the UI footer against `InformationalVersion`.
3. Copy `FluentConfig.dll` and `FluentConfig.UpdaterHelper.exe` from `FluentConfig/Host/bin/Release/net481/` (helper is built with the host).
4. Commit, tag `v{InformationalVersion}`, push the tag.
5. Create a GitHub release on that tag. Attach the two binaries **with the exact names above**.
6. DllCheck actions that point `UpdateRepo` at this repository will see the new latest. Extension menus that gate `minFluentConfig` should use the InformationalVersion string (e.g. `"0.1.0-beta.2"`).

`UpdateRepo` in the library (`ddev01/SB-FluentConfig`) is the published origin for DllCheck copies; forks should change the constant in **their** DllCheck copy, not in every consumer menu.

## See also

- [PACKAGING.md](PACKAGING.md) — what to copy into Streamer.bot `dlls/`
- [docs/guides/SECURITY.md](../../docs/guides/SECURITY.md)
- [docs/guides/UPDATES.md](../../docs/guides/UPDATES.md)
