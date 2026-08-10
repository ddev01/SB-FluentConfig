# Plugin author guides

Minimal guides for extension authors targeting the WebView2 host. Full control catalog and protocol details live in [../FluentConfig/PROTOCOL.md](../../FluentConfig/PROTOCOL.md).

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

That's the whole model: each method adds a control, options chain onto it, settings persist automatically.

**Next:** work through [examples/tutorial/](../../examples/tutorial/) (01 → 10), or jump into a topic below.

## Guides (tutorial order)

| Guide | Contents | Tutorial step |
|-------|----------|---------------|
| [CONTROLS.md](CONTROLS.md) | Control catalog and chained options | 01, 03, 07, 08 |
| [VISIBILITY.md](VISIBILITY.md) | `ShowWhen`, `WithVisibility`, `WithVisibilityWhenOff` | 03, 04 |
| [LAYOUT.md](LAYOUT.md) | Grid / Row / Size / RepeatFor | 05 |
| [DIALOGS_AND_RUNTIME_VALUES.md](DIALOGS_AND_RUNTIME_VALUES.md) | Buttons, dialogs, reading values | 06, 10 |
| [PILLS.md](PILLS.md) | PillInput + nested ItemTemplate | 09 |
| [UPDATES.md](UPDATES.md) | FluentConfig.dll vs extension update paths | deployment/ |
| [EXTRAS.md](EXTRAS.md) | Known bots, window chrome, perf pointer | — |

## Setup

| Doc | Contents |
|-----|----------|
| [../setup/REFERENCES.md](../setup/REFERENCES.md) | Assembly references + Run on UI thread |
