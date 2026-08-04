using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentConfig;
using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Cold/warm timing for schema+settings path (no WebView2 window — comparable host-side cost).
    /// Full WebView2 open timing is captured via PerfTrace in Streamer.bot / SmokeHost harness.
    /// </summary>
    public class StartupPerformanceTests
    {
        [Fact]
        public void SchemaBuild_ColdAndWarm_ReportsTiming()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
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

            // Cold
            var sw = Stopwatch.StartNew();
            var ui1 = FluentConfigUi.Create(cph.Object, "Perf Cold", "1.0");
            build(ui1);
            var coldDoc = ui1.Session.BuildDocumentForTests();
            sw.Stop();
            var coldMs = sw.ElapsedMilliseconds;
            Assert.NotNull(coldDoc);
            Assert.NotEmpty(coldDoc.Sections);

            // Warm (same process, second open)
            sw.Restart();
            var ui2 = FluentConfigUi.Create(cph.Object, "Perf Warm", "1.0");
            build(ui2);
            var warmDoc = ui2.Session.BuildDocumentForTests();
            sw.Stop();
            var warmMs = sw.ElapsedMilliseconds;
            Assert.NotNull(warmDoc);

            // Emit for harness/log collection (xunit captures console in some runners)
            Console.WriteLine($"[PerfTrace] Next.SchemaBuild.Cold: {coldMs}ms");
            Console.WriteLine($"[PerfTrace] Next.SchemaBuild.Warm: {warmMs}ms");

            // Sanity: both should complete quickly (schema-only path)
            Assert.True(coldMs < 5000, "Cold schema build took unexpectedly long: " + coldMs + "ms");
            Assert.True(warmMs < 5000, "Warm schema build took unexpectedly long: " + warmMs + "ms");
        }
    }
}
