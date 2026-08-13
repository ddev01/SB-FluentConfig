# FluentConfig

A library for building plugin settings UIs inside Streamer.bot. Tabbed panels, toggles, sliders, dropdowns, and more — defined with a fluent C# API and rendered in WebView2 (Svelte UI).

## Why FluentConfig?

<img src="assets/preview.png" alt="FluentConfig settings UI" height="350px" width="auto" />

Configuring a Streamer.bot plugin shouldn't require editing code. Chains of "Set Argument" subactions are tedious and error-prone. Asking end users to open raw C# is unrealistic for most of them. FluentConfig bridges that gap: end users get an easy-to-use GUI, and you define it with a simple, readable API.

**It really is this simple:**

```csharp
Fc.Open(CPH, "My Extension", "1.0", ui => ui
    .Section("General", "general", s => s
        .Toggle("Enable feature", "enabled")
        .Textbox("Username", "username")
        .Slider("Volume", "volume").Range(0, 100).Default(50)));
```

Each method call adds a control. Options chain onto it. That's the whole model. Settings persist automatically as JSON in Streamer.bot globals (`{slug}_settings`, e.g. `"First Chatters"` → `first_chatters_settings`).

Ready for more? Work through [examples/tutorial/01_BasicControls.cs](examples/tutorial/01_BasicControls.cs) → 11 step by step.

**Reading values at runtime** depends on context:

| Context | API |
|---------|-----|
| After the user opens/saves the UI | `Fc.LoadSettings<T>(CPH, title)` (or `Fc.GetSetting<T>` / `Fc.SettingsKeyFor(title)`) |
| Write a setting from an action | `Fc.SetSetting` / `Fc.SaveSettings` / `Fc.HasSavedSettings` |
| Runtime state (not the menu) | `Fc.LoadData<T>` / `Fc.SaveData` → `{slug}_data` |
| Event args + chat templates | `Fc.CaptureEvent` / `Fc.ApplyTemplate` / `Fc.Logger` |
| Button `OnClick` handlers | `UiContext.Pending<T>(saveKey)` |
| Pill `OnPillAdded` / `OnPillRemoved` | `CallbackContext.GetValue<T>(saveKey)` |

## Quick Start

The fastest path is to import an example from [examples/tutorial/](examples/tutorial/). It comes preconfigured with everything that's easy to get wrong:

**Execute C# Method subaction with `Run on UI thread` enabled.** This is the most important part, and it requires a specific two-subaction setup. Your plugin logic lives inside a disabled **Execute C# Code** subaction so it doesn't auto-run when a trigger fires. Then a separate **Execute C# Method** subaction calls the main function from that file, with **Run on UI thread** turned on. That toggle only exists on Execute C# Method, not on Execute C# Code, which is why both subactions are needed. Without `Run on UI thread`, the host window won't display correctly or will throw errors.

**GAC-based assembly references.** These work across .NET 4.7.2, 4.8, and 4.8.1. References tied to a specific version folder (e.g. `v4.8`) break on machines that only have 4.8.1 installed.

**DLL and version check.** Verifies FluentConfig.dll is present before opening, and prevents duplicate windows from stacking.

### Manual setup

1. Copy `FluentConfig.dll` (and `Newtonsoft.Json.dll` if needed) into your Streamer.bot `dlls/` folder. Do **not** copy WebView2 DLLs — Streamer.bot already loads them.
2. Create an **Execute C# Code** subaction with your plugin code and **disable** it so it doesn't run on trigger.
3. Create an **Execute C# Method** subaction, point it at the code file from step 2, select your main method, and enable **Run on UI thread**.
4. Add assembly references as documented in [docs/setup/REFERENCES.md](docs/setup/REFERENCES.md). Use GAC paths, not version-specific Reference Assembly folders.
5. Write your UI using the fluent API shown above.

## Building

The solution is `SB-FluentConfig.slnx` (net481). The shipping assembly is `FluentConfig/Host` with `AssemblyName=FluentConfig`.

```powershell
# Debug (WebView2 loads http://localhost:5173 — run `bun run dev` in FluentConfig/web)
dotnet build FluentConfig/Host/Host.csproj -c Debug

# Release (embeds the Vite single-file bundle; requires Bun + bun install in FluentConfig/web)
dotnet build FluentConfig/Host/Host.csproj -c Release
```

Outputs land in `FluentConfig/Host/bin/{Debug|Release}/net481/FluentConfig.dll`. Or use [FluentConfig/scripts/Redeploy.ps1](FluentConfig/scripts/Redeploy.ps1) to rebuild and copy into a local Streamer.bot `dlls/` folder.

## Layout

| Path | Role |
|------|------|
| [FluentConfig/Host/](FluentConfig/Host/) | C# host → `FluentConfig.dll` (WPF shell + WebView2 + DSL) |
| [FluentConfig/web/](FluentConfig/web/) | Svelte 5 settings UI |
| [FluentConfig/PROTOCOL.md](FluentConfig/PROTOCOL.md) | Host ↔ web wire contract |
| [FluentConfig/UpdaterHelper/](FluentConfig/UpdaterHelper/) | Optional stage-and-swap helper exe |
| [docs/](docs/) | Plugin author docs |
| [examples/](examples/) | Guided tutorial + reference / deployment examples |
| [decompile/](decompile/) | Local reference dumps (gitignored) |

## Documentation

See the index at [docs/README.md](docs/README.md). Highlights:

| Document | Contents |
|----------|----------|
| [docs/guides/README.md](docs/guides/README.md) | Author landing page + quick start |
| [docs/guides/CONTROLS.md](docs/guides/CONTROLS.md) | Control catalog |
| [docs/guides/LAYOUT.md](docs/guides/LAYOUT.md) | Grid / Row / Size / RepeatFor |
| [docs/guides/UPDATES.md](docs/guides/UPDATES.md) | Self-update vs notify-only extension update paths |
| [docs/setup/REFERENCES.md](docs/setup/REFERENCES.md) | Assembly reference setup for Streamer.bot C# actions |
| [docs/performance/README.md](docs/performance/README.md) | Cold/warm open baselines and how to remeasure |
| [FluentConfig/PROTOCOL.md](FluentConfig/PROTOCOL.md) | Host ↔ web message contract |
| [FluentConfig/Host/PACKAGING.md](FluentConfig/Host/PACKAGING.md) | Deploy footprint (FluentConfig.dll + Newtonsoft; WebView2 from Streamer.bot) |
| [FluentConfig/ARCHITECTURE.md](FluentConfig/ARCHITECTURE.md) | Module boundaries and design notes |
| [examples/README.md](examples/README.md) | Guided example index |

## Examples

See [examples/README.md](examples/README.md) for the full guided path. Highlights:

| Example | What it shows |
|---------|--------------|
| `tutorial/01_BasicControls.cs` | Minimal setup: toggle, textbox, slider |
| `tutorial/02_Pages.cs` | Multiple Section tabs |
| `tutorial/03_DropdownAndButtons.cs` | Dropdown, ShowWhen, Button + Popup |
| `tutorial/05_LayoutAndRepeatFor.cs` | Grid / Row / Size + RepeatFor |
| `tutorial/09_PillsAndNestedItems.cs` | PillInput + nested ItemTemplate |
| `tutorial/10_ReadingSavedSettings.cs` | Read saved settings from a runtime action |
| `tutorial/11_RuntimeHelpers.cs` | Logger, templates, SetSetting, `{slug}_data` |
| `reference/FullControlShowcase.cs` | Everything in one file (lookup) |
| `deployment/01_DllCheck.cs` | Install + daily FluentConfig.dll check |
| `deployment/02_ExtensionUpdateNotice.cs` | In-menu extension update modal |
