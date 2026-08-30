# FluentConfig performance baselines

Archive of **Release + PerfTrace** cold/warm open timings so future runs can be compared against known-good numbers.

Baseline reports are keyed by **git commit** (short SHA in the filename; full SHA and subject in the report), not by date alone.

## How to remeasure

1. **Redeploy** the host with PerfTrace enabled:

   ```powershell
   .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace
   ```

2. **Streamer.bot WebSocket Server** — enable Auto Start (default `127.0.0.1:8080`, endpoint `/`).

3. **Action** — paste from [`FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) (minimal) or [`FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) (complete). Large-menu fixture (unmeasured): [`actions/PERF_BENCHMARK_LARGE_MENU_ACTION.cs.txt`](actions/PERF_BENCHMARK_LARGE_MENU_ACTION.cs.txt). Run on UI thread. Select via `PERF_ACTION_NAME` in the harness.

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

After a run you trust, archive under `baselines/<short-sha>-minimal.md` / `<short-sha>-complete.md` / `<short-sha>-large.md` for the commit you measured (tip of the perf work tree).

**Large-menu cold start is unmeasured** until someone runs the large-menu fixture on a live Streamer.bot (Release + `FluentConfigPerfTrace`, cold start). Do not treat the absence of a large-menu baseline as known-good.

## Phase vs mark

Milestones in `FluentConfig_PerfLast` are tagged `kind: "phase"` or `kind: "mark"`:

| Kind | Meaning |
|------|---------|
| **phase** | Duration of that step (ms spent inside the named operation). |
| **mark** | Elapsed time **from `Show.begin`** to that point — not a step duration. |

Example: `script-start` at 1567 ms (mark) means the web bundle began executing 1567 ms after `Show.begin`, not that script-start took 1567 ms.

## Archived baselines

| Commit | Subject | Variant | Action name | Cold `totalMs` | Warm `totalMs` | Report |
|--------|---------|---------|-------------|----------------|----------------|--------|
| `9472226` | docs: deferred updates and perf measurement guide | Minimal | `FluentConfig Perf Benchmark` | 1615 | 284 | [9472226-minimal.md](baselines/9472226-minimal.md) |
| `9472226` | docs: deferred updates and perf measurement guide | Complete | `FluentConfig Perf Complete` | 1670 | 276 | [9472226-complete.md](baselines/9472226-complete.md) |
| `0c9d64c` | perf(host): lazy webview2, env overlap, and deferred icon | Minimal | `FluentConfig Perf Benchmark` | 1270 | — | [0c9d64c-minimal.md](baselines/0c9d64c-minimal.md) |
| `0c9d64c` | perf(host): lazy webview2, env overlap, and deferred icon | Complete | `FluentConfig Perf Complete` | 1282 | — | [0c9d64c-complete.md](baselines/0c9d64c-complete.md) |

Full SHA: `9472226070f6d6348ae3d0fc45e000aaebd449d2` (tree at end of perf work on 2026-08-05); cold-start pass `0c9d64c`.

Cold-start optimization handoff (completed): see [archive/COLD_START_OPTIMIZATION_HANDOFF.md](archive/COLD_START_OPTIMIZATION_HANDOFF.md) — work shipped; use baselines above for numbers.

**Metadata (both runs):** Streamer.bot 1.0.4, Release + PerfTrace (`FC_PERF_TRACE`), local Windows machine.

- **Cold** — restart Streamer.bot, then first harness open (`cold: true`).
- **Warm** — close the FluentConfig window, do not restart SB, then reopen (`cold: false`).

## Action templates

| Location | Role |
|----------|------|
| [`FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) | **Canonical paste source** (minimal) |
| [`FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) | **Canonical paste source** (complete) |
| [`actions/PERF_BENCHMARK_LARGE_MENU_ACTION.cs.txt`](actions/PERF_BENCHMARK_LARGE_MENU_ACTION.cs.txt) | Large-menu fixture (8 refresh dropdowns, 4 pills, RepeatFor at cap 32, 4 tabs). **Needs a manual Streamer.bot run** — not yet a baseline. |
| [`actions/PERF_BENCHMARK_ACTION.cs.txt`](actions/PERF_BENCHMARK_ACTION.cs.txt) | Archived snapshot matching baselines (may predate the explicit menu-id API) |
| [`actions/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](actions/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) | Archived snapshot matching baselines |

Host copies are the live paste targets for minimal/complete; `docs/performance/actions/` snapshots for those two are frozen alongside each baseline entry. The large-menu file is authored here until a Host copy is added after the first archived run.

## Comparing results

- Compare **same action** (minimal vs complete are not interchangeable).
- Compare against the **same commit** (or re-baseline after intentional perf changes).
- Use **Release + PerfTrace** only. Debug builds and Vite dev-server loads produce invalid timings.
- `totalMs` is wall-clock to `web-ready` (end-to-end open).
- Large regressions in `Schema.Build` or `Window.Create` on warm runs often indicate a host or bundling change, not network.
