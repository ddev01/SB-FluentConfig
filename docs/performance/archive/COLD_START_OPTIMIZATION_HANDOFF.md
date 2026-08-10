# Cold-start optimization handoff

Handoff for an autonomous AI agent continuing **cold open** performance work on FluentConfig. The user intends to walk away from the PC; the agent should iterate safely without manual Enter prompts or closing menus between runs.

**Working directory:** `F:/Dev/SB-FluentConfig`  
**Branch:** `develop`  
**Scope:** Cold `totalMs` only. Warm opens are out of scope for this pass.

---

## Context summary

### Architecture

FluentConfig is a **WPF shell + WebView2 + Svelte 5** settings UI embedded in Streamer.bot extensions:

| Layer | Path | Role |
|-------|------|------|
| Host | `FluentConfig/Host/` | Thin WPF window, DSL → schema, settings, updater, protocol DTOs |
| Web | `FluentConfig/web/` | Svelte renderer, RPC client, theme; **Release** embeds single-file `dist/index.html` (~150–156 KB) via `NavigateToString` |
| Protocol | `PROTOCOL.md`, `Host/Protocol/`, `web/src/protocol/` | camelCase JSON wire contract |
| Tests | `FluentConfig/FluentConfig.Tests/` | Host behavior without Streamer.bot GUI |
| Smoke | `FluentConfig/SmokeHost/` | STA harness opening real WebView2 + embedded HTML |

Runtime flow: plugin action → `FluentConfigUi.ShowOrFocus` → `FluentConfigSession.Show()` → build schema → create/show window → WebView2 ensure + navigate → bootstrap RPC → `web-ready` mark.

See `FluentConfig/ARCHITECTURE.md` for module boundaries.

### What was already shipped (perf work through commit `9472226`)

| Item | Notes |
|------|-------|
| **`FC_PERF_TRACE`** | `PerformanceTracer` compiles in when `-p:FluentConfigPerfTrace=true`; phases + marks logged and summarized |
| **`FluentConfig_PerfLast`** | Persisted CPH global written on `web-ready`; harness polls this |
| **Multi marks** | `script-start`, `svelte-mount`, `rpc-bound`, `bootstrap-received`, `web-ready` (marks = elapsed from `Show.begin`, not step durations) |
| **Shared `CoreWebView2Environment`** | Process-lifetime reuse on UI thread; **no** background PreWarm thread |
| **Mock bridge excluded from Release** | Vite alias `devMock.ts` → `devMock.empty.ts`; embed size gate ~158 KB in `vite.config.ts` |
| **Deferred `NetworkBackground`** | Canvas arms after rAF ×2 + `requestIdleCallback` (`web/src/lib/NetworkBackground.svelte`) |
| **Deferred extension update notice** | HTTP check after show (`WithExtensionUpdateNotice` / `ConfigureExtensionUpdateNotice`); never blocks `Show()` |
| **Header slimmed / removed** | In-app chrome reduced for smaller embed / less first-paint work |
| **Benchmark harness** | `FluentConfig/scripts/perf-benchmark/` — WebSocket `DoAction` + `GetGlobal` poll |
| **Baselines + docs** | `docs/performance/README.md`, `baselines/9472226-*.md` |

Packaging: **do not** ship WebView2 DLLs in `dlls/` (Streamer.bot provides them). See `FluentConfig/Host/PACKAGING.md`.

### Baselines (commit `9472226`)

Full SHA: `9472226070f6d6348ae3d0fc45e000aaebd449d2`  
Subject: `docs: deferred updates and perf measurement guide`  
Host: **Release + PerfTrace**, Streamer.bot 1.0.4, local Windows.

| Variant | Action name | Cold `totalMs` | Warm `totalMs` | Report |
|---------|-------------|----------------|----------------|--------|
| Minimal | `FluentConfig Perf Benchmark` | **1615** | **284** | [9472226-minimal.md](baselines/9472226-minimal.md) |
| Complete | `FluentConfig Perf Complete` | **1670** | **276** | [9472226-complete.md](baselines/9472226-complete.md) |

**Cold bottleneck (complete, from baseline):**

| Phase / mark | Cold ms | Notes |
|--------------|---------|-------|
| `Window.Create` | ~511 | Largest host-side phase |
| `Window.Show` | ~596 | WPF show + bridge start |
| `WebView.EnsureCore` | ~187 | First-process WebView2 init |
| `Schema.Build` | ~69 | Small even on complete UI |
| `script-start` (mark) | ~1609 | Elapsed from `Show.begin` — JS parse/exec |
| `web-ready` (mark) | ~1669 | End-to-end `totalMs` |

Warm path is already fast (~276 ms complete); **do not optimize warm** in this pass.

### Already rejected / out of scope

Do **not** re-propose without new evidence:

- **Costura** / single-file managed packers (`PACKAGING.md`)
- **Shipping WebView2** in extension `dlls/`
- **Background PreWarm** thread for `CoreWebView2Environment` (removed; UI-thread shared env only)
- **Motion One** (`motion` package) — embed size
- **Extra terser passes** — minify already on; do not chase bundle bytes for &lt;1% gains
- **WPF-UI `FluentWindow`** — architecture is thin native chrome + WebView2

Also **do not** switch `NavigateToString` → virtual host mapping unless carefully validated (CSP, relative URLs, regression on warm path).

### What's left

Improve **cold** `totalMs` on the **Complete** action by **≥5–10%** or **≥100 ms absolute** vs baseline **1670**, without breaking STA/WebView2/packaging/focus path.

High-confidence areas still on the table (investigate, benchmark, keep or revert):

1. **Defer work inside `Window.Create`** — loading overlay, icon decode, DWM title bar, non-critical HWND hooks until after `Show` or first navigation.
2. **Lazy `WebView2` control construction** — if measurable in `Window.Create` without breaking teardown/focus.
3. **Web first-paint shell** — render tab chrome before heavy sections (web-only; measure `script-start` / `web-ready` marks).
4. **Trim cold JS path** — defer imports, lazy dialogs/overlays already partially done; avoid Motion One.
5. **Host `Application` bootstrap** — only if profiling shows cost in `Show.begin` phase.

Low priority / noisy: micro-optimizations in `Schema.Build`, settings load, or chasing single-digit ms on `EnsureCore` without a structural win.

### Measurement semantics

- **`totalMs`** = wall-clock from tracer start to `web-ready` mark.
- **`kind: "phase"`** = duration inside that named operation.
- **`kind: "mark"`** = elapsed ms **from `Show.begin`** to that point (not a duration).

---

## Unattended cold benchmark harness

### Location

- Script: `FluentConfig/scripts/perf-benchmark/benchmark.mjs`
- README: `FluentConfig/scripts/perf-benchmark/README.md`

### Environment variables

| Variable | Default | Use for cold-only |
|----------|---------|-------------------|
| `PERF_RUNS` | `2` | Set **`1`** — single cold open, no warm run |
| `PERF_ACTION_NAME` | `FluentConfig Perf Benchmark` | Set **`FluentConfig Perf Complete`** (primary) |
| `PERF_PAUSE_MS` | `0` | Unused when `PERF_RUNS=1` |
| `STREAMERBOT_WS_URL` | `ws://127.0.0.1:8080/` | Match SB WebSocket Server settings |
| `STREAMERBOT_WS_PASSWORD` | empty | Required if SB enforces auth |
| `PERF_TIMEOUT_MS` | `60000` | Raise only if UI is very slow |
| `PERF_POLL_MS` | `250` | Poll interval for `FluentConfig_PerfLast` |

### Enter prompt / `PERF_RUNS=1` behavior

The harness prompts for Enter only when **`i > 0`** (second and later runs). With **`PERF_RUNS=1`**, the loop runs once and **exits without any Enter prompt**. No harness fix was required for unattended cold-only runs.

If `PERF_RUNS &gt; 1` and stdin is not a TTY, the harness auto-waits 5 s between runs (still requires closing the window for warm semantics — **avoid** for this task).

### Cold procedure (unattended)

**After each code change:**

1. Redeploy Release + PerfTrace (stops and restarts Streamer.bot → automatic cold process):
   ```powershell
   cd F:\Dev\SB-FluentConfig
   .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace
   ```
2. Wait for Streamer.bot to finish starting and WebSocket Server to accept connections (~10–20 s after relaunch; watch SB UI or retry harness connect).
3. Run **one** cold sample:
   ```powershell
   cd FluentConfig\scripts\perf-benchmark
   $env:PERF_ACTION_NAME = 'FluentConfig Perf Complete'
   $env:PERF_RUNS = '1'
   node benchmark.mjs
   ```
4. Record `totalMs` and key milestones from output. Compare to baseline **1670** (complete cold).

**Multiple cold samples without a code change** (variance check): fully restart Streamer.bot (stop process + start, or run Redeploy again), then repeat step 3. Do **not** rely on closing the menu window — cold requires **process** restart.

**Optional regression check (minimal):** same steps with `PERF_ACTION_NAME = 'FluentConfig Perf Benchmark'`; baseline cold **1615**.

### Prerequisites (one-time)

1. Streamer.bot actions pasted from:
   - `FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt` → action name **`FluentConfig Perf Complete`**
   - (optional) `FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt` → **`FluentConfig Perf Benchmark`**
   - Execute C# Method → `CPHInline.Execute`, **Run on UI thread ON**, Code sub-action disabled.
2. WebSocket Server enabled (Auto Start).
3. `npm install` in `FluentConfig/scripts/perf-benchmark` (Node 18+).

`Redeploy.ps1` auto-detects Streamer.bot install path; pass `-StreamerBotPath` if detection fails.

---

## Safety constraints

| Rule | Why |
|------|-----|
| **STA / UI thread** | `FluentConfigSession.Show()` throws off STA; Streamer.bot Method must use Run on UI thread |
| **Do not break `ShowOrFocus` / focus path** | `AlreadyOpened` focuses existing window; must not re-run full Show instrumentation |
| **Keep `Dispatcher` / `BeginInvoke` sync context** | WebView2 callbacks and overlay hide use `Dispatcher.BeginInvoke` |
| **Do not remove `GC.SuppressFinalize(_webView)`** | Prevents fatal teardown on host exit |
| **No WebView2 DLLs in `dlls/`** | Version mismatch → `FileLoadException` |
| **Keep tests green** | Run before/after each experiment |
| **Reversible experiments** | Prefer flags or small diffs; revert if no clear win or any regression |
| **Do not chase noise** | Ignore &lt;5% and &lt;50 ms swings on single samples |

### Tests to run

```powershell
cd F:\Dev\SB-FluentConfig\FluentConfig
dotnet test FluentConfig.Tests\FluentConfig.Tests.csproj -c Release
```

Relevant suites: `Phase2HostSmokeTests`, `StartupPerformanceTests` (schema-only timing), `FluentConfigWindowManagerTests`, `HostBridgeTests`, protocol tests.

Optional manual smoke (real WebView2, not timed for baselines):

```powershell
dotnet run --project SmokeHost\SmokeHost.csproj -c Release
```

---

## Iteration loop (agent workflow)

```
implement safe opt → dotnet test → Redeploy Release -PerfTrace →
wait for SB → PERF_RUNS=1 bench (Complete) → compare vs 1670 →
keep OR revert → repeat until stop criteria
```

### Keep vs revert

| Outcome | Action |
|---------|--------|
| Complete cold **≥100 ms** or **≥5–10%** better, tests green, no functional regression | **Keep**; archive new baseline |
| Any warm-path regression, focus bug, WebView2 teardown issue, or test failure | **Revert** immediately |
| Improvement &lt;50 ms or &lt;5% on one sample | **Revert** or re-sample; treat as noise |
| Structural win but incomplete | Keep only if confident; document follow-up |

### Stop criteria

Stop when:

- No remaining **high-confidence** options you would bet on, **or**
- Last **N ≥ 3** attempts each yielded **&lt;5%** and **&lt;50 ms** on complete cold, **or**
- You would need rejected/out-of-scope approaches (Costura, ship WebView2, PreWarm thread, Motion One, FluentWindow, risky virtual host).

### Archiving new baselines

If a real win ships, add `docs/performance/baselines/<short-sha>-complete.md` (and `-minimal.md` if re-measured), update the table in `docs/performance/README.md`, keyed to commit short SHA.

### Git / commit policy

- **Default: do not commit** unless the user explicitly asked in the session prompt.
- **Never push** without explicit user instruction.
- Leave a clean summary of kept changes vs reverted experiments in the handoff reply.
- Before any commit: scan diff for secrets, real host paths, personal identifiers (privacy rule).

---

## Key file map

| Path | Purpose |
|------|---------|
| `FluentConfig/scripts/Redeploy.ps1` | Build, copy DLL, stop/start Streamer.bot |
| `FluentConfig/Host/App/FluentConfigSession.cs` | `Show()` phases, deferred sections, update check |
| `FluentConfig/Host/Host/FluentConfigHostWindow.cs` | Window, WebView2, shared env, navigate |
| `FluentConfig/Host/Core/PerformanceTracer.cs` | Phases, marks, `FluentConfig_PerfLast` JSON |
| `FluentConfig/Host/Core/FluentConfigWindowManager.cs` | `AlreadyOpened` / focus |
| `FluentConfig/Host/Host/EmbeddedHtml.cs` | Release HTML string |
| `FluentConfig/web/vite.config.ts` | Embed build, mock exclusion, size gate |
| `FluentConfig/web/src/App.svelte` | Root shell, perf marks |
| `FluentConfig/web/src/lib/perf.ts` | Web perf marks → host |
| `docs/performance/README.md` | Baseline index, phase vs mark |
| `FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt` | Complete bench action template |
| `FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt` | Minimal bench action template |

---

## Prompt for other AI (self-contained)

Copy everything in the block below into a new agent session.

```text
You are optimizing FluentConfig COLD startup time on Windows. Read the full handoff first:
  F:/Dev/SB-FluentConfig/docs/performance/COLD_START_OPTIMIZATION_HANDOFF.md

GOAL
- Improve cold totalMs on the Complete perf action meaningfully: target ≥5–10% or ≥100 ms vs baseline 1670 ms (commit 9472226, Release+PerfTrace).
- Scope is COLD ONLY. Do not optimize warm opens. Do not ask the user to close menus between runs.

WORKING CONTEXT
- Repo: F:/Dev/SB-FluentConfig, branch develop.
- Architecture: WPF shell + WebView2 + Svelte single-file embed (~156 KB Release).
- Baselines (9472226070f6d6348ae3d0fc45e000aaebd449d2):
    Minimal action "FluentConfig Perf Benchmark": cold 1615, warm 284.
    Complete action "FluentConfig Perf Complete": cold 1670, warm 276.
- Cold bottlenecks: Window.Create (~500 ms), Window.Show (~550–600 ms), WebView.EnsureCore (~170–190 ms); Schema.Build is small even on complete.
- Marks (script-start, web-ready, etc.) are elapsed from Show.begin, not phase durations.

ALREADY DONE (do not redo)
- FC_PERF_TRACE, FluentConfig_PerfLast, shared CoreWebView2Environment (no PreWarm thread), mock excluded from Release embed, deferred NetworkBackground, deferred WithExtensionUpdateNotice, perf harness, baseline docs.

REJECTED (do not pursue)
- Costura, ship WebView2 in dlls/, background PreWarm, Motion One, extra terser, WPF-UI FluentWindow, NavigateToString→virtual host without careful validation.

UNATTENDED BENCHMARK PROCEDURE
1. After each code change:
   .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace
   (This stops and restarts Streamer.bot → cold process.)
2. Wait for Streamer.bot + WebSocket Server (~10–20 s).
3. Cold bench (no Enter prompt):
   cd FluentConfig\scripts\perf-benchmark
   $env:PERF_ACTION_NAME = 'FluentConfig Perf Complete'
   $env:PERF_RUNS = '1'
   node benchmark.mjs
4. Optional regression: same with PERF_ACTION_NAME = 'FluentConfig Perf Benchmark' (baseline cold 1615).
5. For extra cold samples without code changes: restart Streamer.bot fully, then rerun step 3.

ITERATION
- Implement one safe optimization at a time.
- dotnet test FluentConfig\FluentConfig.Tests\FluentConfig.Tests.csproj -c Release
- Redeploy → bench → compare → keep or revert.
- Do NOT chase <5% or <50 ms noise. Do NOT risk breaking STA, WebView2 packaging, ShowOrFocus/focus path, Dispatcher/BeginInvoke sync context, or WebView2 teardown.

STOP WHEN
- No high-confidence options left, OR last 3+ attempts all <5% and <50 ms improvement.

COMMITS
- Do not commit unless improvement is confirmed AND the user explicitly said to commit.
- Never push.
- If archiving a win: add docs/performance/baselines/<short-sha>-complete.md and update docs/performance/README.md.

Deliverables: list of experiments (kept/reverted), before/after cold totalMs on Complete, test status, and recommended next steps if stopped early.
```

---

## Prompt only (minimal paste)

```text
Optimize FluentConfig COLD startup (not warm) on F:/Dev/SB-FluentConfig, branch develop.

Read: docs/performance/COLD_START_OPTIMIZATION_HANDOFF.md

Target: Complete action cold totalMs ≥100 ms or ≥5–10% below baseline 1670 (9472226, Release+PerfTrace).

Loop: safe change → dotnet test → .\FluentConfig\scripts\Redeploy.ps1 -Configuration Release -PerfTrace → wait for SB →
  $env:PERF_ACTION_NAME='FluentConfig Perf Complete'; $env:PERF_RUNS='1'; node FluentConfig\scripts\perf-benchmark\benchmark.mjs
→ compare → keep/revert. PERF_RUNS=1 exits without Enter. Cold = SB process restart (Redeploy does this).

Do not: Costura, ship WebView2, PreWarm thread, Motion One, extra terser, FluentWindow, break ShowOrFocus/focus or STA.
Stop: no high-confidence opts left or 3+ attempts <5%/<50ms. Do not commit unless user asked; never push.
```
