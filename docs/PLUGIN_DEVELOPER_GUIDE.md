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

## Self-update / third-party update

```csharp
FluentConfigUi.Create(CPH, "My Extension", "1.0")
    .WithUpdateCheck("example-org/example-extension", "1.0.0")
    .Section(...)
    .Show();

// Or without UI:
// GitHubUpdater.CheckForUpdate("example-org/example-extension", "1.0.0");
// GitHubUpdater.EnsureInstalled(path, "example-org/example-extension");
// GitHubUpdater.StageUpdate(url, path);
```

Staging writes `path + ".update"`. `FluentConfig.UpdaterHelper.exe` waits for Streamer.bot to exit, swaps the file, and relaunches.

## References

See [REFERENCES.md](REFERENCES.md) for assembly refs and Run on UI thread.
