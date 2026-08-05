# Baseline: Complete — 9472226

| Field | Value |
|-------|-------|
| **Commit** | `9472226070f6d6348ae3d0fc45e000aaebd449d2` |
| **Subject** | docs: deferred updates and perf measurement guide |
| Measured | 2026-08-05 |
| Action | `FluentConfig Perf Complete` |
| Template | [PERF_BENCHMARK_COMPLETE_ACTION.cs.txt](../actions/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) |
| Host | Release + PerfTrace (`FC_PERF_TRACE`) |
| Streamer.bot | 1.0.4 |
| Machine | Local Windows |
| Cold procedure | Restart Streamer.bot, then first harness open |
| Warm procedure | Close FluentConfig window, reopen without restarting SB |

## Summary

| Run | `totalMs` | `cold` |
|-----|-----------|--------|
| Cold | **1670** | `true` |
| Warm | **276** | `false` |

## Cold run (run 1/2)

```
totalMs: 1670
```

| Milestone | ms | kind |
|-----------|-----|------|
| Show.begin | 136 | phase |
| Settings.Load | 1 | phase |
| Schema.Build | 69 | phase |
| Window.Create | 511 | phase |
| Window.Show | 596 | phase |
| WebView.EnsureCore | 187 | phase |
| script-start | 1609 | mark |
| svelte-mount | 1619 | mark |
| rpc-bound | 1619 | mark |
| WebView.NavigateCall | 119 | phase |
| WebView.NavigationCompleted | 0 | phase |
| bootstrap-received | 1663 | mark |
| web-ready | 1669 | mark |
| Bridge.BootstrapSent | 46 | phase |

## Warm run (run 2/2)

```
totalMs: 276
```

| Milestone | ms | kind |
|-----------|-----|------|
| Show.begin | 0 | phase |
| Settings.Load | 0 | phase |
| Schema.Build | 0 | phase |
| Window.Create | 0 | phase |
| Window.Show | 17 | phase |
| WebView.EnsureCore | 139 | phase |
| script-start | 207 | mark |
| svelte-mount | 208 | mark |
| rpc-bound | 208 | mark |
| WebView.NavigateCall | 53 | phase |
| WebView.NavigationCompleted | 0 | phase |
| bootstrap-received | 214 | mark |
| web-ready | 276 | mark |
| Bridge.BootstrapSent | 63 | phase |
