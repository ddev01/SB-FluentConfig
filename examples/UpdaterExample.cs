// Streamer.bot C# action – extension update *notify* example (FluentConfig).
// Third-party extensions are action bundles, not a swappable DLL — use notify-only
// tag-prefix checks + a release-page link. Do NOT call StageUpdate / UpdateHelperLauncher
// for extension updates (that path is FluentConfig.dll self-update only).
//
// Required references: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/EXTENSION_UPDATES.md and docs/REFERENCES.md.
//
// For tests, set FLUENTCONFIG_GITHUB_API_BASE to a local mock (e.g. http://127.0.0.1:PORT).

using FluentConfig;

public class CPHInline
{
    private static class ExtensionInfo
    {
        public const string Title = "Example Extension";
        public const string Version = "1.0.0";
        public const string GitHubRepo = "example-org/example-extension"; // owner/name
        public const string TagPrefix = "example";
    }

    public bool Execute()
    {
        FluentConfigUi.ShowOrFocus(CPH, ExtensionInfo.Title, ExtensionInfo.Version, ui => ui
            .WithExtensionUpdateNotice(ExtensionInfo.GitHubRepo, ExtensionInfo.TagPrefix, ExtensionInfo.Version)
            .Section("General", "General", s => s
                .Intro("Settings for an example extension. When a newer tag-prefix release exists, a notify banner links to the GitHub release page.")
                .Toggle("Enable feature", "enabled")
                    .Default(true)
                .Textbox("Display name", "display_name")
                    .Default("Example")
            ));
        return true;
    }
}
