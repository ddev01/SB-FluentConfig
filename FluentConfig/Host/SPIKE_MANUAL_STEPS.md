# FluentConfig — Manual host validation

Confirm a WPF window hosting WebView2 works when opened via Streamer.bot
**Execute C# Method** with **Run on UI thread** enabled.

Prep:

- Build `FluentConfig.dll` from `FluentConfig/Host` (Debug or Release)
- Copy DLL(s) into Streamer.bot's `dlls\` folder (or run `FluentConfig/scripts/Redeploy.ps1`)
- Do **not** copy WebView2 assemblies into `dlls/`

---

## 1. Open Streamer.bot

Launch your local Streamer.bot install. Skip or dismiss platform login prompts if you only need local C# testing.

## 2. Confirm the DLL is present

Under the Streamer.bot install folder, check:

```
dlls\FluentConfig.dll
```

Do **not** copy `Microsoft.Web.WebView2.*.dll` or `WebView2Loader.dll` into `dlls/` —
Streamer.bot already loads its own copies from the install root. Duplicate versions
in `dlls/` cause `FileLoadException` (manifest mismatch).

## 3. Create a new Action

1. Open the **Actions** panel.
2. Create a new action (any name, e.g. `FluentConfig Host Check`).
3. You do **not** need a trigger for a Test-button run.

## 4. Sub-action A — Execute C# Code (DISABLED)

1. Add sub-action **Core → C# → Execute C# Code**.
2. Paste the contents of:

   `FluentConfig/Host/SPIKE_TEST_ACTION.cs.txt`

   (or a Phase 2 smoke script / example from `examples/`)

3. **Disable** this sub-action so it does not auto-run on trigger. The method body
   must still compile and be visible to Execute C# Method.

## 5. Sub-action B — Execute C# Method (Run on UI thread)

1. Add sub-action **Core → C# → Execute C# Method**.
2. Point it at the C# file / class from step 4.
3. Select method **`CPHInline.Execute`** (or whatever your UI lists for `Execute`).
4. Enable **Run on UI thread**. This is the critical toggle — without it the
   check is invalid.

## 6. Assembly references

On the C# Code sub-action, add references (see `docs/REFERENCES.md`):

| Assembly | How |
|----------|-----|
| PresentationFramework | By name, or GAC path below |
| PresentationCore | By name, or GAC path below |
| WindowsBase | By name, or GAC path below |
| FluentConfig | Path to `dlls\FluentConfig.dll` under the Streamer.bot install |

GAC fallbacks:

```
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
C:\Windows\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll
```

Use Streamer.bot's **Find Refs** if compile errors complain about missing WPF types.

## 7. Test

1. Click **Test** on the action (or run the Execute C# Method sub-action).
2. **Pass:** a FluentConfig host window appears (WebView2 loads the UI — not blank / crashed).
3. **Fail signals:**
   - STA / apartment exception → **Run on UI thread** is off
   - Orange-red error text in the window → WebView2 init failed (read the message)
   - Compile errors about PresentationFramework / FluentConfig → fix references
   - Nothing happens → confirm sub-action B is the one running and A is disabled
