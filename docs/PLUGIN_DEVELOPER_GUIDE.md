# Plugin developer guide (FluentConfig)

Minimal guide for extension authors targeting the WebView2 host. Full control catalog and protocol details live in [../FluentConfig/PROTOCOL.md](../FluentConfig/PROTOCOL.md).

## Quick start

```csharp
using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("My Extension", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "My Extension", "1.0")
            .Section("Settings", "Settings", s => s
                .Intro("Configure the extension.")
                .Toggle("Enabled", "enabled").Default(true)
                .Textbox("Name", "name"))
            .Show();

        return true;
    }
}
```

## PillInput (schema nesting)

```csharp
.PillInput("Items", "items")
    .ItemTemplate(pb => pb
        .Title("Item: {name}")
        .Toggle("Enabled", "{name}_enabled").Default(true)
        .Slider("Value", "{name}_value").Range(0, 100).Default(50))
    .OnPillRemoved((item, ctx) =>
    {
        ctx.RemoveSettingsKeys(item + "_enabled", item + "_value");
    })
```

Do not use WPF `Panel` types in callbacks — they are not part of this API.

## Visibility

```csharp
.Toggle("Show extras", "show_extra")
.WithVisibility("show_extra", inner => inner
    .Textbox("Extra", "extra_value"))

.Toggle("Premium", "premium_mode")
.WithVisibilityWhenOff("premium_mode", inner => inner
    .Textbox("Free tier", "free_tier_setting"))
// equivalent: .WithVisibility("premium_mode", inner => ..., inverted: true)
```

## Dynamic field count (RepeatFor)

When an integer setting drives how many related fields appear, use `RepeatFor` (live show/hide, no reopen). Full behavior, defaults, and recipes: **[LAYOUT.md](LAYOUT.md)**.

```csharp
.RepeatFor("max_count", (row, i) => row
    .IntegerInput($"Points for place #{i}", $"points_{i}")
        .Range(0, 100000)
        .Default(i == 1 ? 100 : 25)
        .Size("fit")
)
```

Comparator gates: `.ShowWhen("max_count", Comparator.GreaterOrEqual, 5)`. Example: [`DynamicCountLayoutExample.cs`](../examples/menu/DynamicCountLayoutExample.cs).

## Layout: Grid, Row, Size, Span

Side-by-side controls and per-control sizing. Compound token specs (same style as Grid), default gap (`3` = 12px), Size vs Span: **[LAYOUT.md](LAYOUT.md)**.

```csharp
.Grid("grid-cols-2 gap-3 items-center", g => g
    .Toggle("Announce", "announce").Default(true)
    .Toggle("Reset daily", "reset_daily").Default(false)
    .Textbox("Prefix", "announce_prefix").Span(2)
)

.IntegerInput("Count", "max_count").Range(1, 10).Size("fit min-w-20")
.Row("gap-2 items-center", r => r
    .Textbox("Name", "name").Size("grow")
    .Button("Go").Text("Run").Size("shrink-0")
)
```

## Known bots (chat filters)

Curated Twitch bot / automation logins for ignoring spam bots in chat triggers. No CPH, no UI — safe on hot paths:

```csharp
using FluentConfig;

if (KnownBots.IsKnownBot(userName))
    return true; // skip

IReadOnlyList<string> all = KnownBots.GetKnownBots(); // sorted copy
```

`IsKnownBot` is case-insensitive, trims whitespace, and strips a leading `@`. Broadcaster / channel-bot exclusion stays in your action (`CPH.TwitchGetBroadcaster` / `TwitchGetBot`) — this helper only covers the shared third-party list.

## Updates (DLL + extension)

**FluentConfig.dll** is installed/updated by a copy-paste **DllCheck** action (no FluentConfig reference) — see [`examples/updater/DllCheckExample.cs`](../examples/updater/DllCheckExample.cs).

**Extension** updates use an in-menu notify modal:

```csharp
FluentConfigUi.Create(CPH, "My Extension", "1.0")
    .WithExtensionUpdateNotice("example-org/example-extension", "1.0.0")
    .Section(...)
    .Show();

// Low-level (no UI):
// GitHubUpdater.CheckForLatestRelease("example-org/example-extension", "1.0.0");
// GitHubUpdater.CheckForTaggedRelease("example-org/monorepo", "spotify", "1.0.0");
// GitHubUpdater.CheckForUpdate("example-org/SB-FluentConfig", "0.1.0"); // DLL assets
// GitHubUpdater.EnsureInstalled(path, "example-org/SB-FluentConfig");
// GitHubUpdater.StageUpdate(url, path);
```

Staging writes `path + ".update"`. `FluentConfig.UpdaterHelper.exe` waits for Streamer.bot to exit, swaps the file, and relaunches. Full model: [EXTENSION_UPDATES.md](EXTENSION_UPDATES.md).

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
2. Paste an action template (Run on UI thread): minimal [`PERF_BENCHMARK_ACTION.cs.txt`](../FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt) or worst-case [`PERF_BENCHMARK_COMPLETE_ACTION.cs.txt`](../FluentConfig/Host/PERF_BENCHMARK_COMPLETE_ACTION.cs.txt) — select via `PERF_ACTION_NAME` in the harness.
3. Run the Node harness in [`FluentConfig/scripts/perf-benchmark/`](../FluentConfig/scripts/perf-benchmark/) — it drives Streamer.bot via Client WebSocket `DoAction` and reads the CPH global `FluentConfig_PerfLast` written on `web-ready`.

See that folder’s README for cold (SB restart) vs warm (close window, no restart) procedure.

Archived Release+PerfTrace baselines: [docs/performance/](performance/).

## References

See [REFERENCES.md](REFERENCES.md) for assembly refs and Run on UI thread.
