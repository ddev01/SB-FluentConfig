# Baseline: Complete — cold-start optimization (uncommitted)

| Field | Value |
|-------|-------|
| **Parent commit** | `92a3caa` (working tree; not yet committed) |
| **Change** | Lazy WebView2, early env kickoff, deferred icon, slim loading overlay |
| Measured | 2026-08-05 |
| Action | `FluentConfig Perf Complete` |
| Host | Release + PerfTrace (`FC_PERF_TRACE`) |
| Streamer.bot | 1.0.4 |
| Machine | Local Windows |
| Cold procedure | Redeploy (SB restart), then harness `PERF_RUNS=1` |

## Summary

| Run | `totalMs` | `cold` |
|-----|-----------|--------|
| Cold sample 1 | **1283** | `true` |
| Cold sample 2 | **1281** | `true` |

Prior baseline (`9472226`): cold **1670** → **~1282** avg (−388 ms, −23%).

## Cold run (sample 1/2)

```
totalMs: 1283
```

| Milestone | ms | kind |
|-----------|-----|------|
| Show.begin | 138 | phase |
| Settings.Load | 1 | phase |
| Schema.Build | 76 | phase |
| Window.Create | 400 | phase |
| Window.Show | 333 | phase |
| WebView.EnsureCore | 166 | phase |
| script-start | 1233 | mark |
| svelte-mount | 1239 | mark |
| rpc-bound | 1239 | mark |
| WebView.NavigateCall | 123 | phase |
| WebView.NavigationCompleted | 0 | phase |
| bootstrap-received | 1276 | mark |
| web-ready | 1282 | mark |
| Bridge.BootstrapSent | 42 | phase |

## Cold run (sample 2/2)

```
totalMs: 1281
```

| Milestone | ms | kind |
|-----------|-----|------|
| Show.begin | 138 | phase |
| Settings.Load | 1 | phase |
| Schema.Build | 76 | phase |
| Window.Create | 409 | phase |
| Window.Show | 327 | phase |
| WebView.EnsureCore | 167 | phase |
| script-start | 1234 | mark |
| svelte-mount | 1241 | mark |
| rpc-bound | 1241 | mark |
| WebView.NavigateCall | 122 | phase |
| WebView.NavigationCompleted | 0 | phase |
| bootstrap-received | 1274 | mark |
| web-ready | 1280 | mark |
| Bridge.BootstrapSent | 38 | phase |
