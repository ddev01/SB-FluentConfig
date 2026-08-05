# FluentConfig performance baselines

Archive of **Release + PerfTrace** cold/warm open timings so future runs can be compared against known-good numbers.

## How to remeasure

1. **Redeploy** the host with PerfTrace enabled:

   ```powershell
   .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace
   ```

2. **Streamer.bot WebSocket Server** — enable Auto Start (default `127.0.0.1:8080`, endpoint `/`).

3. **Action** — paste from [`FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) (minimal) or [`FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) (complete). Run on UI thread. Select via `PERF_ACTION_NAME` in the harness.

4. **Harness**:

   ```bash
   cd FluentConfig/scripts/perf-benchmark
   npm install
   npm run bench
   ```

   For complete/worst-case:

   ```powershell
   $env:PERF_ACTION_NAME = 'FluentConfig Perf Complete'
   npm run bench
   ```

See [`FluentConfig/scripts/perf-benchmark/README.md`](../../FluentConfig/scripts/perf-benchmark/README.md) for cold vs warm procedure and environment variables.

## Phase vs mark

Milestones in `FluentConfig_PerfLast` are tagged `kind: "phase"` or `kind: "mark"`:

| Kind | Meaning |
|------|---------|
| **phase** | Duration of that step (ms spent inside the named operation). |
| **mark** | Elapsed time **from `Show.begin`** to that point — not a step duration. |

Example: `script-start` at 1567 ms (mark) means the web bundle began executing 1567 ms after `Show.begin`, not that script-start took 1567 ms.

## Archived baselines

| Date | Variant | Action name | Cold `totalMs` | Warm `totalMs` | Report |
|------|---------|-------------|----------------|----------------|--------|
| 2026-08-05 | Minimal | `FluentConfig Perf Benchmark` | 1615 | 284 | [2026-08-05-minimal.md](baselines/2026-08-05-minimal.md) |
| 2026-08-05 | Complete | `FluentConfig Perf Complete` | 1670 | 276 | [2026-08-05-complete.md](baselines/2026-08-05-complete.md) |

**Metadata (both runs):** Streamer.bot 1.0.4, Release + PerfTrace (`FC_PERF_TRACE`), local Windows machine.

- **Cold** — restart Streamer.bot, then first harness open (`cold: true`).
- **Warm** — close the FluentConfig window, do not restart SB, then reopen (`cold: false`).

## Action templates

| Location | Role |
|----------|------|
| [`FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) | **Canonical paste source** (minimal) |
| [`FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) | **Canonical paste source** (complete) |
| [`actions/PERF_BENCHMARK_ACTION.cs.txt`](actions/PERF_BENCHMARK_ACTION.cs.txt) | Archived snapshot matching baselines |
| [`actions/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](actions/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) | Archived snapshot matching baselines |

Host copies are the live paste targets; `docs/performance/actions/` snapshots are frozen alongside each baseline entry.

## Comparing results

- Compare **same action** (minimal vs complete are not interchangeable).
- Use **Release + PerfTrace** only. Debug builds and Vite dev-server loads produce invalid timings.
- `totalMs` is wall-clock to `web-ready` (end-to-end open).
- Large regressions in `Schema.Build` or `Window.Create` on warm runs often indicate a host or bundling change, not network.
