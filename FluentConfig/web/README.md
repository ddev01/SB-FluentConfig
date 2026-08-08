# FluentConfig web UI

Svelte 5 + Vite single-file bundle embedded into `FluentConfig.dll` for the WebView2 host.

## Scripts

| Command | Purpose |
| --- | --- |
| `bun run dev` | Vite/HMR at `http://localhost:5173` (Debug host navigates here) |
| `bun run build` | Production single-file `dist/index.html` (Release embed) |
| `bun run check` | `svelte-check` + TypeScript |
| `bun run verify:mock` | Mock-bridge coverage / smoke (DEV mock only) |

## Bridge

- **Host (WebView2):** `window.chrome.webview` — production path.
- **Browser preview:** DEV mock bridge (`bridge/mockBridge.ts`) serves a fake bootstrap document. Production builds alias the mock out of the graph so Release HTML does not include it.

## Bundle size

Release `dist/index.html` should stay near **~150–158 KB** (soft gate **159 KB** in `vite.config.ts`, plus a mock-string check). Do not chase extra terser passes — minify is already on.

## Related

- Protocol: `FluentConfig/PROTOCOL.md`
- Host embed: Release build copies `web/dist/index.html` into the DLL
