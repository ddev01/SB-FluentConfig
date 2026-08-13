// Deployment 02 — Extension update notice (notify-only in-menu modal).
// Pair with 01_DllCheck.cs in the same action chain (DllCheck → this menu).
//
// Setup: Execute C# Method + Run on UI thread.
// Refs: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/guides/UPDATES.md and docs/setup/REFERENCES.md.

using FluentConfig;

public class CPHInline
{
    private static class ExtensionInfo
    {
        public const string Title = "Example Extension";
        public const string Version = "1.0.0";
        public const string Repo = "example-org/example-extension"; // owner/name
        // Optional monorepo tag prefix, e.g. "spotify" for tags spotify-v1.2.3
        // public const string TagPrefix = "spotify";
        public const string MinStreamerBot = "";   // e.g. "1.0.0" — empty skips gate
        public const string MinFluentConfig = "";  // e.g. "0.1.0" — empty skips gate
        public const string UpdateGuideUrl = "";   // empty → GitHub release page
    }

    public bool Execute()
    {
        FluentConfig.FluentConfig.SetLogCallback(msg => CPH.LogInfo("[FC] " + msg));
        CPH.LogInfo(
            "[ExtensionUpdate] opening "
            + ExtensionInfo.Title
            + " v"
            + ExtensionInfo.Version
            + " (repo="
            + ExtensionInfo.Repo
            + ")");

        Fc.Open(CPH, ExtensionInfo.Title, ExtensionInfo.Version, ui => ui
            // Default: GitHub releases/latest (one extension per repo).
            .WithExtensionUpdateNotice(
                ExtensionInfo.Repo,
                ExtensionInfo.Version,
                minStreamerBot: ExtensionInfo.MinStreamerBot,
                minFluentConfig: ExtensionInfo.MinFluentConfig,
                updateGuideUrl: ExtensionInfo.UpdateGuideUrl)

            // Monorepo / multi-extension repo — uncomment and set TagPrefix:
            // .WithExtensionUpdateNotice(
            //     ExtensionInfo.Repo,
            //     ExtensionInfo.Version,
            //     tagPrefix: "spotify",
            //     minStreamerBot: ExtensionInfo.MinStreamerBot,
            //     minFluentConfig: ExtensionInfo.MinFluentConfig,
            //     updateGuideUrl: ExtensionInfo.UpdateGuideUrl)

            .Section("About", "About", s => s
                .Intro(
                    "This menu checks for a newer extension release once per day.\n\n"
                    + "When an update is available, a modal offers **How to update**, **Later**, "
                    + "or **Don't ask for this version**.\n\n"
                    + "FluentConfig.dll itself is installed/updated by the preceding **DllCheck** action — "
                    + "not from this menu.")));

        return true;
    }
}
