// Tutorial 06 — Dialogs and feedback
// Popup, Toast, ConfirmDialog, ProgressWindow, and Log from Button OnClick handlers.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/setup/REFERENCES.md and docs/guides/DIALOGS_AND_RUNTIME_VALUES.md.
//
// Previous: 05_LayoutAndRepeatFor.cs · Next: 07_DropdownRefreshAndPairs.cs

using FluentConfig;
using System.Threading;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        Fc.Open(CPH, "Tutorial 06 Dialogs And Feedback", "1.0", ui => ui
            .Section("Dialogs", "Dialogs", b => b
                .Intro("Click buttons to exercise Popup, Confirm, Progress, Toast, and Log.")
                .Textbox("Sample name", "sample_name")
                    .Hint("Read by the Popup button via UiContext.Pending.")
                    .Default("Demo")
                .Slider("Sample level", "sample_level")
                    .Hint("Also read by the Popup button.")
                    .Range(0, 100)
                    .Default(50)
                .Button("Show pending values")
                    .Hint("Reads Pending values and shows them in a popup.")
                    .Text("Popup")
                    .Color("#714bfd")
                    .OnClick(ui =>
                    {
                        string name = ui.Pending<string>("sample_name");
                        int level = ui.Pending<int>("sample_level");
                        ui.Popup("Pending values", $"Name: '{name}'\nLevel: {level}");
                    })
                .Button("Confirm dialog")
                    .Hint("Shows Yes/No dialog and displays the result.")
                    .Text("Confirm")
                    .Color("#31a8ff")
                    .OnClick(ui =>
                    {
                        var confirmed = ui.ShowConfirmDialog("Confirm Test", "Do you want to continue?", "Yes", "No");
                        ui.Popup("Confirm Result", $"Confirmed: {confirmed}");
                    })
                .Button("Progress window")
                    .Hint("Shows progress, simulates 10 steps, then closes.")
                    .Text("Progress")
                    .Color("#1ba489")
                    .OnClick(ui =>
                    {
                        const int total = 10;
                        var progress = ui.ShowProgressWindow("Progress Test", "Simulating work...", "Progress", total);
                        if (progress != null)
                        {
                            Task.Run(() =>
                            {
                                for (int i = 0; i <= total; i++)
                                {
                                    Thread.Sleep(200);
                                    progress.Report(i);
                                }
                                progress.Close();
                                ui.Popup("Progress Done", "Progress window completed.");
                            });
                        }
                    })
                .Button("Toast")
                    .Hint("Shows a short-lived toast notification.")
                    .Text("Toast")
                    .Color("#f09930")
                    .OnClick(ui => ui.Toast("Test toast – auto-dismiss in 3 seconds"))
                .Button("Log")
                    .Hint("Writes a line to the log callback (visible in Streamer.bot logs if wired).")
                    .Text("Log")
                    .Color("#636b9a")
                    .OnClick(ui => ui.Log("Test log message from button click"))));

        return true;
    }
}
