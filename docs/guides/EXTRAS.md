# Extras

Small topics that don't need their own long guide.

## Known bots (chat filters)

Curated Twitch bot / automation logins for ignoring spam bots in chat triggers. No CPH, no UI — safe on hot paths:

```csharp
using FluentConfig;

if (KnownBots.IsKnownBot(userName))
    return true; // skip

IReadOnlyList<string> all = KnownBots.GetKnownBots(); // sorted copy
```

`IsKnownBot` is case-insensitive, trims whitespace, and strips a leading `@`. Broadcaster / channel-bot exclusion stays in your action (`CPH.TwitchGetBroadcaster` / `TwitchGetBot`) — this helper only covers the shared third-party list.

## Window chrome

FluentConfig uses native WPF chrome with a dark title bar (DWM immersive mode) matching the web UI. A default FluentConfig icon is embedded; override with `.Icon(@"C:\path\to\icon.ico")` on the root builder.

Window size and position are saved automatically on close to the CPH global `FluentConfig_General_Settings` (`windows.{title}`) and restored on the next open (clamped to visible screens). Optional callbacks:

```csharp
FluentConfig.FluentConfig.SetWindowClosedCallback((width, height) => { /* legacy size-only */ });
FluentConfig.FluentConfig.SetWindowClosedCallback((left, top, width, height) => { /* full geometry */ });
```

## Performance measurement

For cold/warm open timing with a Release host built with `FC_PERF_TRACE`:

1. Redeploy: `FluentConfig/scripts/Redeploy.ps1 -Configuration Release -PerfTrace`
2. Paste an action template (Run on UI thread): minimal [`PERF_BENCHMARK_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) or worst-case [`PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) — select via `PERF_ACTION_NAME` in the harness.
3. Run the Node harness in [`FluentConfig/scripts/perf-benchmark/`](../../FluentConfig/scripts/perf-benchmark/) — it drives Streamer.bot via Client WebSocket `DoAction` and reads the CPH global `FluentConfig_PerfLast` written on `web-ready`.

See that folder’s README for cold (SB restart) vs warm (close window, no restart) procedure.

Archived Release+PerfTrace baselines: [../performance/](../performance/).

## See also

- [README.md](README.md) — guide index
- [../setup/REFERENCES.md](../setup/REFERENCES.md) — assembly refs
