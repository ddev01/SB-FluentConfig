using System;
using FluentConfig;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Schema-build sanity (no WebView2). Timing is logged only; this is not a performance gate.
    /// Full window open timing is captured via PerfTrace in Streamer.bot / SmokeHost.
    /// </summary>
    public class SchemaBuildSanityTests
    {
        [Fact]
        public void SchemaBuild_ProducesSections()
        {
            var cph = TestCph.CreateMockCph();
            Action<FluentConfigUi> build = ui =>
            {
                ui.Section("General", "general", s => s
                    .Intro("Perf sample")
                    .Toggle("Enabled", "perf_enabled").Default(true)
                    .Textbox("Name", "perf_name").Default("x")
                    .Dropdown("Choice", "perf_choice")
                        .Options(new[] { ("a", "A"), ("b", "B"), ("c", "C") })
                        .DefaultByValue("b")
                    .PillInput("Items", "perf_items")
                        .WithItemTemplate(item => item
                            .Toggle("On", "{name}_on").Default(true)
                            .Slider("Val", "{name}_val").Range(0, 100).Default(50)
                        )
                    .WithVisibility("perf_enabled", inner => inner
                        .Textbox("Extra", "perf_extra")
                    )
                );
            };

            var ui1 = FluentConfigUi.Create(cph.Object, "schema_cold", "Schema Cold", "1.0");
            build(ui1);
            var coldDoc = ui1.Session.BuildDocumentForTests();
            Assert.NotNull(coldDoc);
            Assert.NotEmpty(coldDoc.Sections);

            var ui2 = FluentConfigUi.Create(cph.Object, "schema_warm", "Schema Warm", "1.0");
            build(ui2);
            var warmDoc = ui2.Session.BuildDocumentForTests();
            Assert.NotNull(warmDoc);
            Assert.NotEmpty(warmDoc.Sections);
            Assert.Equal(coldDoc.Sections.Count, warmDoc.Sections.Count);
        }
    }
}
