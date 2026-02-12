# C# Execute Action References

When you create a C# Execute action that uses FluentConfig, add these assembly references so the code compiles on any Windows system.

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

If your environment requires file paths, use the .NET Framework Reference Assemblies (standard on Windows 10/11):

```
C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\PresentationFramework.dll
C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\PresentationCore.dll
C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\WindowsBase.dll
C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll
C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll
```

Use `v4.7.2` or `v4.8.1` if `v4.8` is not present.

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

## Streamer.bot "Find Refs"

Streamer.bot's **Find Refs** button can infer and add missing references from compile errors. Use it if you prefer automatic resolution over manual setup.
