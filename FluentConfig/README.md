# FluentConfig (library tree)

WebView2 + Svelte implementation of FluentConfig. The shipping assembly is built from [Host/](Host/) with `AssemblyName=FluentConfig` → `FluentConfig.dll`.

| Area | Path |
|------|------|
| C# host (`FluentConfig.dll`) | [Host/](Host/) |
| Packaging notes | [Host/PACKAGING.md](Host/PACKAGING.md) |
| Updater helper exe | [UpdaterHelper/](UpdaterHelper/) |
| Web UI | [web/](web/) |
| Wire protocol | [PROTOCOL.md](PROTOCOL.md) |
| Architecture | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Tests | [FluentConfig.Tests/](FluentConfig.Tests/) |
| Redeploy script | [scripts/Redeploy.ps1](scripts/Redeploy.ps1) |

Plugin author docs and copy-paste examples live at the repo root: [../docs/](../docs/), [../examples/](../examples/).

**Debug:** host navigates to `http://localhost:5173`. **Release:** `NavigateToString(EmbeddedHtml.Content)` with the Vite single-file bundle embedded at build time (`bun run build` → `EmbeddedHtml.Bundle.g.cs`).
