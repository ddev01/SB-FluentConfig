// Streamer.bot C# action – third-party extension updater example (FluentConfig).
// Shows how an extension author uses the generic GitHub-releases updater API.
// The web UI never talks to GitHub — only the host does.
//
// Required references: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/REFERENCES.md and FluentConfig/PROTOCOL.md (update-notice / update.stage).
//
// For tests, set FLUENTCONFIG_GITHUB_API_BASE to a local mock (e.g. http://127.0.0.1:PORT).

using FluentConfig;
using FluentConfig.Updater;
using System;
using System.IO;

public class CPHInline
{
    private const string ExtensionTitle = "Example Extension";
    private const string ExtensionVersion = "1.0.0";
    private const string GitHubRepo = "example-org/example-extension"; // owner/name
    private const string DllFileName = "ExampleExtension.dll";

    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened(ExtensionTitle, ExtensionVersion))
            return true;

        string dllsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlls");
        string targetPath = Path.Combine(dllsDir, DllFileName);

        if (!File.Exists(targetPath))
        {
            // First-time install from latest release asset (no-op if missing / network fail).
            GitHubUpdater.EnsureInstalled(targetPath, GitHubRepo);
        }

        var ui = FluentConfigUi.Create(CPH, ExtensionTitle, ExtensionVersion)
            // Notify-only banner for tag-prefix releases (third-party extensions).
            // For FluentConfig.dll self-update use .WithUpdateCheck(repo, version) instead.
            .WithExtensionUpdateNotice(GitHubRepo, "example", ExtensionVersion)
            .Section("General", "General", s => s
                .Intro("Example third-party extension settings. Update checks use FluentConfig's shared updater.")
                .Toggle("Enable feature", "enabled")
                    .Default(true)
                .Textbox("Display name", "display_name")
                    .Default("Example")
                .Button("Check for updates now")
                    .Text("Check updates")
                    .OnClick(ctx =>
                    {
                        var info = GitHubUpdater.CheckForUpdate(GitHubRepo, ExtensionVersion);
                        if (info != null && info.UpdateAvailable)
                        {
                            GitHubUpdater.StageUpdate(info.DownloadUrl, targetPath);
                            UpdateHelperLauncher.LaunchSwapAndRelaunch(targetPath);
                            ctx.Toast("Update staged — restart Streamer.bot to finish.");
                        }
                        else
                        {
                            ctx.Toast("No update available (or check failed).");
                        }
                    })
            );

        ui.Show();
        return true;
    }
}
