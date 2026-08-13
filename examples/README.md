# FluentConfig examples

Copy-paste Streamer.bot C# Execute actions targeting the WebView2 host (`FluentConfig/Host`).
Use **Execute C# Method** with **Run on UI thread** enabled. Assembly refs: see [docs/setup/REFERENCES.md](../docs/setup/REFERENCES.md).

**Start here:** work through `tutorial/` in order (01 → 11). Each step builds on the last.

Update paths (DllCheck vs extension modal): [docs/guides/UPDATES.md](../docs/guides/UPDATES.md).

## tutorial/ — guided path

| Step | File | What you'll learn |
|------|------|-------------------|
| 01 | [01_BasicControls.cs](tutorial/01_BasicControls.cs) | Toggle, Textbox, Slider — the minimal menu |
| 02 | [02_Pages.cs](tutorial/02_Pages.cs) | Multiple `Section` tabs (pages) |
| 03 | [03_DropdownAndButtons.cs](tutorial/03_DropdownAndButtons.cs) | Dropdown, `ShowWhen`, Button + `OnClick` + Popup |
| 04 | [04_ConditionalVisibility.cs](tutorial/04_ConditionalVisibility.cs) | `WithVisibility` / `WithVisibilityWhenOff` blocks |
| 05 | [05_LayoutAndRepeatFor.cs](tutorial/05_LayoutAndRepeatFor.cs) | `Grid` / `Row` / `Size` + `RepeatFor` dynamic field count |
| 06 | [06_DialogsAndFeedback.cs](tutorial/06_DialogsAndFeedback.cs) | Popup, Toast, Confirm, Progress, Log |
| 07 | [07_DropdownRefreshAndPairs.cs](tutorial/07_DropdownRefreshAndPairs.cs) | `Refresh()` and `WithPairValue` dropdowns |
| 08 | [08_AdvancedControls.cs](tutorial/08_AdvancedControls.cs) | Exclusive toggles, DynamicTextboxes, repeatable rows |
| 09 | [09_PillsAndNestedItems.cs](tutorial/09_PillsAndNestedItems.cs) | PillInput + nested `ItemTemplate` |
| 10 | [10_ReadingSavedSettings.cs](tutorial/10_ReadingSavedSettings.cs) | Runtime action reading Tutorial 03's saved JSON |
| 11 | [11_RuntimeHelpers.cs](tutorial/11_RuntimeHelpers.cs) | Logger, templates, SetSetting, `{slug}_data` blob |

## reference/

| File | Description |
|------|-------------|
| [FullControlShowcase.cs](reference/FullControlShowcase.cs) | Everything in one file — lookup after the tutorial |

## deployment/

Action chain order: **DllCheck → Menu**.

| Step | File | Description |
|------|------|-------------|
| 01 | [01_DllCheck.cs](deployment/01_DllCheck.cs) | No FluentConfig ref — install + daily auto-stage of `FluentConfig.dll` |
| 02 | [02_ExtensionUpdateNotice.cs](deployment/02_ExtensionUpdateNotice.cs) | Menu + `.WithExtensionUpdateNotice` (notify-only extension modal) |

## dev/

| File | Description |
|------|-------------|
| [DevPreviewExample.cs](dev/DevPreviewExample.cs) | Contributor tool — mirrors `localhost:5173` mock for browser vs WebView2 comparison. Not an authoring tutorial. |
