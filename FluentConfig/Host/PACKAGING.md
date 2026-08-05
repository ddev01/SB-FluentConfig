# FluentConfig packaging

## Decision: rely on Streamer.bot’s WebView2 — deploy almost nothing else

Streamer.bot already ships and loads WebView2 into its AppDomain at startup
(`Microsoft.Web.WebView2.Core` / `.Wpf` / `.WinForms`, plus native `WebView2Loader.dll`,
currently **1.0.3296.44** on recent builds). Putting a *different* WebView2 version in
`dlls/` causes:

```text
FileLoadException: The located assembly's manifest definition does not match the assembly reference.
```

So FluentConfig compiles against Streamer.bot’s own WebView2 assemblies
(`Private=False` — not copy-local) and **must not** deploy its own copies.

| File | Deploy to `dlls/`? | Role |
|------|-------------------|------|
| `FluentConfig.dll` | **Yes** | Host + builders + updater API |
| `Newtonsoft.Json.dll` | **Yes** (unless already present) | JSON / protocol |
| `FluentConfig.UpdaterHelper.exe` | Optional | Detached stage-and-swap helper |
| `Microsoft.Web.WebView2.*.dll` | **No** | Use Streamer.bot’s copies in the app base directory |
| `WebView2Loader.dll` | **No** | Already in Streamer.bot’s install root |

**Why not Costura / single-file packers:** unnecessary now — the managed dependency
footprint is essentially Newtonsoft only — and risky around native WebView2 loading.

**Build-time path:** `Host.csproj` resolves `$(StreamerBotPath)` via env `STREAMER_BOT_PATH`,
then Desktop portable / WinGet `%LOCALAPPDATA%` fallbacks. Override when needed:

```powershell
dotnet build -p:StreamerBotPath=C:\path\to\Streamer.bot
# or
$env:STREAMER_BOT_PATH = 'C:\path\to\Streamer.bot'
```

**Footer repo URL:** override the generic placeholder with
`-p:FluentConfigRepoUrl=https://example.test/your-fork` or `FluentConfigUi.RepoUrl(...)`.

**Runtime resilience:** `WebView2AssemblyResolve` hooks `AppDomain.AssemblyResolve` so
if Streamer.bot later ships a different WebView2 version than we compiled against, we
reuse the already-loaded / base-directory assembly (and log a warning via the log callback).

**Release HTML:** DEBUG navigates to `http://localhost:5173`. RELEASE embeds the Vite
single-file `web/dist/index.html` at build time (`Host/scripts/EmbedWebHtml.ps1`) and
serves it via `NavigateToString(EmbeddedHtml.Content)`. Requires Bun + `bun install` in
`web/` for Release builds (Debug builds skip the embed step).
