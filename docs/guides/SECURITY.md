# Security model (one page)

FluentConfig splits **who can install bytes** from **who can tell the user that something is new**.

## We auto-update only our own DLL

`FluentConfig.dll` (and `FluentConfig.UpdaterHelper.exe` when present) is installed and staged by the copy-paste **DllCheck** action. That action has **zero** FluentConfig references: Streamer.bot must compile it even when the DLL is missing.

DllCheck talks to GitHub `releases/latest`, picks a `.dll`-named asset, and stages next to the install. The detached helper swaps after Streamer.bot exits. This path never asks the settings UI which URL to download.

## Extensions are notify-only

Streamer.bot extensions are action bundles, not a swappable DLL. `.WithExtensionUpdateNotice(...)` may push `update.available` after a **deferred, host-side** GitHub check. The in-menu modal offers How to update / Later / Don't ask. The user re-imports the bundle themselves. FluentConfig **does not** download or install extension files.

## The web cannot choose a download URL

The Svelte UI never talks to GitHub. It cannot pick, rewrite, or pass a download URL to the host.

- There is no `update.stage` RPC. A compromised or buggy web bundle cannot stage arbitrary bytes as `FluentConfig.dll`.
- `GitHubUpdater.StageUpdate` is a **host** API (DllCheck / tests). It allowlists GitHub download hosts, requires HTTPS, rejects zipballs, and sanity-checks a PE header.
- Extension notices keep `DownloadUrl` null. How to update opens `updateGuideUrl` or the release page via `shell.openUrl`.

## What this is not

- Not a general plugin marketplace. Each extension’s DllCheck constants (`UpdateRepo`, min versions) are the author’s.
- Not an integrity-pinned supply chain: GitHub release assets are trusted as published. There is no signed-catalog or hash pin in DllCheck today.
- WebView2 is Streamer.bot’s copy (`Private=False`). FluentConfig must not ship or NuGet-reference WebView2; a second copy in `dlls/` causes `FileLoadException`.

## See also

- [UPDATES.md](UPDATES.md) — author-facing update paths
- [../../FluentConfig/Host/RELEASE.md](../../FluentConfig/Host/RELEASE.md) — how to cut a FluentConfig.dll release
- [../../FluentConfig/Host/PACKAGING.md](../../FluentConfig/Host/PACKAGING.md) — deploy footprint
