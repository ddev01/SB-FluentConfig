# FluentConfig perf benchmark

Repeatable **cold / warm** open timing via Streamer.bot’s Client WebSocket (`DoAction` → poll `FluentConfig_PerfLast`).

Baselines are only valid with a **Release + PerfTrace** host DLL. Debug / Vite-dev numbers are not comparable.

## Prerequisites

1. **Redeploy** with PerfTrace:

   ```powershell
   .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace
   ```

2. **WebSocket Server** enabled in Streamer.bot:  
   Servers/Clients → WebSocket Server → Auto Start (default `127.0.0.1:8080`, endpoint `/`).

3. **Action** — create one (or both) Streamer.bot actions from the templates below. The harness selects which to run via `PERF_ACTION_NAME`.

   | Variant | Template | Action name |
   |---------|----------|-------------|
   | **Minimal** (default) | [`../../Host/PERF_BENCHMARK_ACTION.cs.txt`](../../Host/PERF_BENCHMARK_ACTION.cs.txt) | **`FluentConfig Perf Benchmark`** |
   | **Complete / worst-case** | [`../../Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) | **`FluentConfig Perf Complete`** |

   For each action:
   - Execute C# Code (disabled) + Execute C# Method → `CPHInline.Execute`
   - **Run on UI thread** enabled on the Method sub-action
   - Refs: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll
   - After Test/Test Trigger: SB Log must show `[MENU] trigger <action title>`

   **Harness note:** `DoAction accepted` only means Streamer.bot found and queued the
   action by name. It does **not** mean `Execute()` ran or the window opened. If the
   harness polls forever, check the SB checklist above (Method disabled / no UI thread /
   compile error are the usual causes).

   Use **minimal** for baseline open timing; use **complete** to stress the full control surface (same density as `examples/reference/FullControlShowcase.cs`).

4. Node 18+ and deps:

   ```bash
   cd FluentConfig/scripts/perf-benchmark
   npm install
   ```

## Cold vs warm

| Mode | Procedure |
|------|-----------|
| **Cold** | Fully **restart Streamer.bot**, then run the harness. First open measures cold WebView2 + navigate. Expect `cold: true` in `FluentConfig_PerfLast`. |
| **Warm** | **Do not** restart SB. **Close** the FluentConfig window, then continue (Enter / pause). Second open reuses process state. Expect `cold: false`. |

`ShowOrFocus` only re-instruments a full Show path. Leaving the window open yields focus-only and no new `FluentConfig_PerfLast`.

## Run

```bash
npm run bench
# or
node benchmark.mjs
```

Default: 2 runs (cold then warm). Between runs, close the menu and press Enter.

### Environment

| Variable | Default | Meaning |
|----------|---------|---------|
| `STREAMERBOT_WS_URL` | `ws://127.0.0.1:8080/` | Client WebSocket URL (host:port/endpoint from SB settings) |
| `STREAMERBOT_WS_PASSWORD` | _(empty)_ | Password when authentication / Enforce is enabled |
| `PERF_ACTION_NAME` | `FluentConfig Perf Benchmark` | Action name for `DoAction` (`FluentConfig Perf Complete` for worst-case) |
| `PERF_RUNS` | `2` | Number of open cycles |
| `PERF_POLL_MS` | `250` | Poll interval for `GetGlobal` |
| `PERF_TIMEOUT_MS` | `60000` | Max wait for `FluentConfig_PerfLast` update |
| `PERF_PAUSE_MS` | `0` | If &gt; 0, auto-wait between runs instead of prompting Enter |

PowerShell examples:

```powershell
# Minimal (default)
$env:STREAMERBOT_WS_URL = 'ws://127.0.0.1:8080/'
$env:PERF_RUNS = '3'
node benchmark.mjs

# Worst-case / complete UI surface
$env:PERF_ACTION_NAME = 'FluentConfig Perf Complete'
npm run bench
```

## Output

Each run prints `totalMs` and milestones from the CPH global written on `web-ready`:

```json
{
  "cold": true,
  "totalMs": 1234,
  "milestones": [
    { "name": "Settings.Load", "ms": 12, "kind": "phase" },
    { "name": "script-start", "ms": 800, "kind": "mark" },
    { "name": "web-ready", "ms": 1100, "kind": "mark" }
  ]
}
```

## WebSocket messages used

See comments in `benchmark.mjs`. Summary:

1. Connect → wait for server **`Hello`** (optional **`Authenticate`** if challenged).
2. **`DoAction`** by action name.
3. Poll **`GetGlobal`** (`variable: FluentConfig_PerfLast`, `persisted: true`) until `lastWrite`/value changes.
   Streamer.bot **1.0.4** returns the value under `variables.<name>`; some docs/clients use singular `variable`. The harness accepts both.
