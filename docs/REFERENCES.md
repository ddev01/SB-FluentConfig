# C# Execute Action References (FluentConfig)

When you create a C# Execute action that uses FluentConfig, add these assembly references. The thin host is still WPF (one window + WebView2), so the framework refs below are required.

---

## Critical: Run on UI thread

Use **Execute C# Method**, not Execute C# Code. Only Execute C# Method has **Run on UI thread**, which is required for WPF + WebView2. Without it, the window will not open correctly.

A common pattern is two sub-actions:

1. **Execute C# Code** (optional / disabled) — compile-only or stub  
2. **Execute C# Method** with **Run on UI thread** enabled — calls your `Execute()` / entry method

---

## Reference by name (recommended)

| Assembly | Purpose |
|----------|---------|
| **PresentationFramework** | WPF Application, Window, MessageBoxResult |
| **PresentationCore** | WPF core types |
| **WindowsBase** | DispatcherObject, base WPF types |
| **System** / **System.Core** | Usually implicit |

**Custom DLL (path required):**

- **FluentConfig** — path to `FluentConfig.dll` in your Streamer.bot `dlls/` folder

You do **not** need to add WebView2 assemblies as action references. Streamer.bot already
loads its own WebView2 into the AppDomain; FluentConfig references those at compile time
and does **not** deploy duplicate `Microsoft.Web.WebView2.*.dll` / `WebView2Loader.dll`
into `dlls/` (doing so causes a version clash — see [../Host/PACKAGING.md](../Host/PACKAGING.md)).

---

## Reference by path (fallback)

Use GAC paths so refs work across .NET Framework 4.7.2 / 4.8 / 4.8.1:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
C:\Windows\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll
```

**FluentConfig.dll** — point at your install, e.g. a local Streamer.bot `dlls` folder (do not hardcode machine-specific paths into shared scripts).

---

## Deploy checklist

Copy into Streamer.bot `dlls/`:

- `FluentConfig.dll`
- `Newtonsoft.Json.dll` (if not already available)
- `FluentConfig.UpdaterHelper.exe` (only if using the updater)

Do **not** copy `Microsoft.Web.WebView2.*.dll` or `WebView2Loader.dll` into `dlls/` —
Streamer.bot’s install root already provides them. The WebView2 **Evergreen runtime**
must be installed on the OS (typical on modern Windows).

---

## DSL changes vs old FluentConfig (authors)

- **PillInput:** use `.ItemTemplate(pb => …)` (or `.WithSectionsPanel(pb => …)` alias). Callbacks no longer receive `Panel` / `StackPanel`. Prefer `{name}` placeholders in nested saveKeys.
- **WithVisibility:** prefer `.WithVisibility("key", build, inverted: true)` or `.WithVisibilityWhenOff("key", build)` instead of a bare positional bool in the middle.
- **Updater:** `FluentConfigUi.Create(...).WithUpdateCheck("owner/repo", "1.0.0")` or call `FluentConfig.Updater.GitHubUpdater` directly from any extension.
