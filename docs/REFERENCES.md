# C# Execute Action References

When you create a C# Execute action that uses FluentConfig, add these assembly references so the code compiles on any Windows system. For usage and controls, see [PLUGIN_DEVELOPER_GUIDE.md](PLUGIN_DEVELOPER_GUIDE.md) and [ELEMENTS.md](ELEMENTS.md).

---

## Recommended: Import the Quick Start Action

The easiest way to get started is to **import the FluentConfig Quick Start action**. It comes preconfigured with:

1. **Execute C# Method subaction with Run on UI thread enabled** — FluentConfig uses WPF, which must run on the main UI thread. Only the **Execute C# Method** subaction lets you enable **Run on UI thread**; Execute C# Code does not, and will fail. This is critical.
2. **Correct GAC assembly references** — Framework assemblies use GAC paths that work across .NET 4.7.2, 4.8, and 4.8.1. Hardcoded `v4.8` paths fail on systems with only 4.8.1.
3. **DLL and version checks** — Skips opening if the UI is already open and verifies FluentConfig.dll before showing the window.
4. **Minimal working script** — Edit the C# code to add your sections and controls.

---

## Reference by name (recommended)

If your environment resolves framework assemblies by name, add:

| Assembly             | Purpose                                      |
|----------------------|----------------------------------------------|
| **PresentationFramework** | WPF: Application, Panel, StackPanel, MessageBoxResult |
| **PresentationCore**       | WPF core types                              |
| **WindowsBase**            | DispatcherObject, base WPF types            |
| **System**                 | Core .NET (usually implicit)                |
| **System.Core**            | LINQ, etc. (usually implicit)               |

**Custom DLL (path required):**
- **FluentConfig** — path to `FluentConfig.dll` in your Streamer.bot `dlls/` folder

These framework assemblies exist on any Windows system that can run Streamer.bot. They resolve automatically when referenced by name.

---

## Reference by path (fallback)

If your environment requires file paths, use **GAC paths** so references work across all .NET Framework 4.x versions (4.7.2, 4.8, 4.8.1). Hardcoded `v4.8` Reference Assemblies paths fail on systems that only have 4.8.1 installed.

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
C:\Windows\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll
```

**FluentConfig.dll:** point to your Streamer.bot installation, e.g.:

```
C:\path\to\Streamer.bot\dlls\FluentConfig.dll
```

---

## Minimal set for simple FluentConfig usage

If your action only uses basic FluentConfig (no PillInput with `WithSectionsPanel`, no `ShowConfirmDialog`, no `Application.Current`), you might only need:

- **FluentConfig** (path to FluentConfig.dll)
- **System** (implicit in most setups)

When you see errors like `'Panel' could not be found` or `'MessageBoxResult' is defined in an assembly that is not referenced`, add **PresentationFramework**, **PresentationCore**, and **WindowsBase** as shown above.

---

## Execute C# Method vs Execute C# Code

Use **Execute C# Method**, not Execute C# Code. Only Execute C# Method has the **Run on UI thread** option, which is required for WPF windows. Without it, the FluentConfig UI will not display correctly.

---

## Streamer.bot "Find Refs"

Streamer.bot's **Find Refs** button can infer and add missing references from compile errors. Use it if you prefer automatic resolution over manual setup.
