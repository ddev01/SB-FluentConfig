# Baseline: Minimal — cold-start optimization (uncommitted)

| Field | Value |
|-------|-------|
| **Parent commit** | `92a3caa` (working tree; not yet committed) |
| **Change** | Lazy WebView2, early env kickoff, deferred icon, slim loading overlay |
| Measured | 2026-08-05 |
| Action | `FluentConfig Perf Benchmark` |
| Host | Release + PerfTrace (`FC_PERF_TRACE`) |
| Streamer.bot | 1.0.4 |
| Machine | Local Windows |
| Cold procedure | Redeploy (SB restart), then harness `PERF_RUNS=1` |

## Summary

| Run | `totalMs` | `cold` |
|-----|-----------|--------|
| Cold | **1270** | `true` |

Prior baseline (`9472226`): cold **1615** → **1270** (−345 ms, −21%).

## Cold run

```
totalMs: 1270
```

| Milestone | ms | kind |
|-----------|-----|------|
| Show.begin | 136 | phase |
| Settings.Load | 7 | phase |
| Schema.Build | 13 | phase |
| Window.Create | 424 | phase |
| Window.Show | 321 | phase |
| WebView.EnsureCore | 168 | phase |
| script-start | 1233 | mark |
| svelte-mount | 1251 | mark |
| rpc-bound | 1251 | mark |
| WebView.NavigateCall | 179 | phase |
| WebView.NavigationCompleted | 0 | phase |
| bootstrap-received | 1267 | mark |
| web-ready | 1269 | mark |
| Bridge.BootstrapSent | 17 | phase |
