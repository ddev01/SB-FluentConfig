# FluentConfig

A library for building plugin settings UIs inside Streamer.bot. Tabbed panels, toggles, sliders, dropdowns, and more, without touching WPF directly.

## Why FluentConfig?

<img src="assets/preview.png" alt="FluentConfig settings UI" height="350px" width="auto" />

Configuring a Streamer.bot plugin shouldn't require editing code. Chains of "Set Argument" subactions are tedious and error-prone. Asking end users to open raw C# is unrealistic for most of them. FluentConfig bridges that gap: end users get an easy-to-use GUI, and you define it with a simple, readable API. Here's what that looks like in practice:

```csharp
FluentConfigUi.Create(CPH, "My Extension", "1.0")
    .Section("General", "general", s => s
        .Toggle("Enable feature", "enabled")
        .Textbox("Username", "username")
        .Slider("Volume", "volume").Range(0, 100).Default(50))
    .Show();
```

Each method call adds a control. Options chain onto it. That's the whole model. Settings persist automatically as JSON in Streamer.bot globals, and you read them back at runtime with `GetValue<T>()`. Use `Grid`, `Flex`, and `Div` for multi-column and horizontal layouts with Tailwind-inspired alignment.

## Quick Start

The fastest path is to import the **FluentConfig Quick Start action** (see [examples/](examples/)). It comes preconfigured with everything that's easy to get wrong:

**Execute C# Method subaction with `Run on UI thread` enabled.** This is the most important part, and it requires a specific two-subaction setup. Your plugin logic lives inside a disabled **Execute C# Code** subaction disabled so it doesn't auto-run when a trigger fires. Then a separate **Execute C# Method** subaction calls the main function from that file, with **Run on UI thread** turned on. That toggle only exists on Execute C# Method, not on Execute C# Code, which is why both subactions are needed. Without `Run on UI thread`, the WPF window won't display correctly or will throw errors.

**GAC-based assembly references.** These work across .NET 4.7.2, 4.8, and 4.8.1. References tied to a specific version folder (e.g. `v4.8`) break on machines that only have 4.8.1 installed.

**DLL and version check.** Verifies FluentConfig.dll is present before opening, and prevents duplicate windows from stacking.

**Minimal working script.** Replace the placeholder sections with your own controls and you're done.

### Manual setup

1. Copy `FluentConfig.dll` into your Streamer.bot `dlls/` folder.
2. Create an **Execute C# Code** subaction with your plugin code and **disable** it so it doesn't run on trigger.
3. Create an **Execute C# Method** subaction, point it at the code file from step 2, select your main method, and enable **Run on UI thread**.
4. Add assembly references as documented in [docs/REFERENCES.md](docs/REFERENCES.md). Use GAC paths, not version-specific Reference Assembly folders.
5. Write your UI using the fluent API shown above.

## Building

The solution is `SB-FluentConfig.slnx`, targeting .NET 4.8.1. Build outputs `FluentConfig.dll` to `FluentConfig/bin/Debug/` or `FluentConfig/bin/Release/`. Copy it to your Streamer.bot `dlls/` folder.

## Documentation

| Document | Contents |
|----------|----------|
| [docs/PLUGIN_DEVELOPER_GUIDE.md](docs/PLUGIN_DEVELOPER_GUIDE.md) | Integration guide: Create, Section, Show, GetValue, dropdowns, DurationInput, layout |
| [docs/ELEMENTS.md](docs/ELEMENTS.md) | Full control reference: all elements, options, and stored types |
| [docs/REFERENCES.md](docs/REFERENCES.md) | Assembly reference setup for Streamer.bot C# actions |

## Examples

The [examples/](examples/) folder contains ready-to-import Streamer.bot actions:

| Example | What it shows |
|---------|--------------|
| `SimpleExample.cs` | Minimal setup: toggle, textbox, slider |
| `MediumExample.cs` | Dropdown, slider, button, `ShowWhen` visibility |
| `CompleteExample.cs` | Every control in one action, for reference |
| `LayoutTestExample.cs` | Grid, Flex, Div, ColSpan, RowSpan, Justify, Align, custom columns, wrap |