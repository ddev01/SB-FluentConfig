// Streamer.bot C# action – minimal FluentConfig example.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/REFERENCES.md for reference details.
//

using FluentConfig;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Simple Example", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "Simple Example", "1.0")
            .Section("Settings", "Settings", s => s
                .Intro("A minimal example with a few basic controls.")
                .Toggle("Enable feature", "enabled")
                    .Hint("Turn this on to enable the feature.")
                    .Default(true)
                .Textbox("Name", "name")
                    .Hint("Enter a display name.")
                    .Default("")
                .Slider("Volume", "volume")
                    .Hint("Adjust volume (0–100).")
                    .Range(0, 100)
                    .Default(50))
            .Show();

        return true;
    }
}


.Section("Settings", "Settings", "grid-cols-2", s => s
	.Column(, c => c
	.Toggle("Enable feature", "enabled").width("1/2")
	.Textbox("Name", "name").width("fit-content")
	.Column(, c => c
	.Toggle("Enable feature2", "enabled")
	.Textbox("Name2", "name")
	.column("col-span-2"c => c) 
	.Toggle("Enable feature3", "enabled")
	.Textbox("Name3", "name")
	)
	