using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using FluentConfig.Core;
using Xunit;
using Xunit.Abstractions;

// Alias needed: class FluentConfig lives in namespace FluentConfig, causing resolution conflicts from FluentConfig.Tests
using FluentConfigApp = global::FluentConfig.FluentConfig;
using FluentConfigUi = global::FluentConfig.FluentConfigUi;
using CallbackContext = global::FluentConfig.CallbackContext;
using SectionBuilder = global::FluentConfig.SectionBuilder;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Startup performance benchmarks that run the full FluentConfig UI creation pipeline
    /// on an STA thread — no Streamer.bot required.
    ///
    /// Pass null for CPH; SettingsManager gracefully returns empty settings.
    /// Each test reports per-phase timings via the PerformanceTracer built into FluentConfig.
    ///
    /// Run with:  dotnet test --filter "FullyQualifiedName~StartupPerformance" -v n
    ///
    /// IMPORTANT: WPF's Application.Current is a static singleton. Only ONE test per
    /// process run can create WPF UI on an STA thread without cross-thread issues.
    /// These tests are designed to run one at a time. The recommended workflow:
    ///   dotnet test --filter "DisplayName~FullUI_StartupTiming" -v n
    /// </summary>
    public class StartupPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public StartupPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Runs a delegate on a fresh STA thread and waits for it to complete.
        /// Required because WPF controls can only be created on STA threads.
        /// </summary>
        private void RunOnSta(Action action)
        {
            Exception caught = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { caught = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (caught != null)
                throw new AggregateException("STA thread failed", caught);
        }

        /// <summary>
        /// Full UI creation benchmark — mirrors examples/CompleteExample.cs but without Streamer.bot.
        /// Tests both with and without image download, and reports per-phase timings.
        ///
        /// This is THE primary performance test. Run it with:
        ///   dotnet test --filter "DisplayName~FullUI_StartupTiming" -v n
        ///
        /// The test output shows exactly where time is spent:
        ///   - Constructor.Init:           field assignment
        ///   - SettingsManager.Create+Load: CPH GetGlobalVar + JSON parse (null CPH = 0)
        ///   - WPF Application.Create:     new Application() if needed
        ///   - WindowBuilder.Build:        FluentWindow + grid + sidebar + tabs
        ///   - ThemeManager.Apply (global): resource dictionary loading
        ///   - ThemeManager.Apply (window): per-window theme
        ///   - AddHeader (image download): synchronous BitmapImage download
        ///   - Section: {name}:            per-section control creation
        ///   - Window.Show+Activate:       WPF layout/render (only if Show is called)
        /// </summary>
        [Fact]
        public void FullUI_StartupTiming()
        {
            var lines = new List<string>();
            void Log(string msg)
            {
                lines.Add(msg);
                _output.WriteLine(msg);
            }

            RunOnSta(() =>
            {
                FluentConfigApp.SetLogCallback(Log);
                FluentConfigWindowManager.SetOpened(false);

                var options = new[] { "Option A", "Option B", "Option C" };
                var pillPanels = new Dictionary<string, Panel>();

                void AddPillSection(Panel container, string itemName, CallbackContext ctx)
                {
                    ctx.WithPanel(container, "Pills", pb => pb
                        .Title($"Item: {itemName}")
                        .Toggle("Enabled", itemName + "_enabled")
                            .Hint("Enable this item.")
                            .Default(true)
                        .Slider("Value", itemName + "_value")
                            .Range(0, 100)
                            .Default(50)
                    );
                }

                // ── Run 1: Without image ──
                _output.WriteLine("========== Run 1: Full UI WITHOUT image ==========");
                var sw1 = Stopwatch.StartNew();

                FluentConfigUi.Create(null, "PerfTest_NoImage", "1.0")
                    .Section("General Settings", "General", g => g
                        .Intro("Test all FluentConfig controls.")
                        .Toggle("Enable Requirement", "custom_requirement_enabled")
                            .Hint("Turn this on to require a custom condition.")
                        .Textbox("Requirement Name", "custom_requirement_name")
                            .Hint("The name of your requirement.")
                        .Textbox("Secret Key", "secret_key")
                            .Hint("Optional secret (password field).")
                            .Password()
                        .Slider("Required Value", "custom_required_value")
                            .Hint("Minimum value needed (1–1000000).")
                            .Range(1, 1000000)
                            .Default(250)
                        .Separator()
                        .Toggle("Enable Optional Limit", "optional_limit_enabled")
                            .Hint("Enable and set a limit (0–100).")
                            .Default(false)
                        .Slider("Optional Limit", "optional_limit")
                            .Hint("Limit value when enabled.")
                            .Range(0, 100)
                            .Default(50)
                            .ShowWhen("optional_limit_enabled")
                        .Filepath("Export Filepath", "export_filepath")
                            .Hint("Path for export file.")
                        .Textbox("Response Template", "response_template")
                            .Multiline()
                            .Hint("Multiline template for responses.")
                            .Default("Hello {user}!")
                        .NumberInput("Rate", "rate_value")
                            .Hint("Rate value (0.1–10, step 0.1).")
                            .WithStepper()
                            .Range(0.1, 10.0)
                            .Step(0.1)
                            .Default(1.0)
                        .ColorPicker("Accent Color", "accent_color")
                            .Hint("Hex color.")
                            .Default("#714bfd")
                        .IntegerInput("Max Retries", "max_retries")
                            .Hint("Number of retry attempts (0–100).")
                            .Range(0, 100)
                            .Default(3)
                        .DurationInput("Timeout", "timeout")
                            .Hint("Connection timeout.")
                            .WithPermanentOption(false)
                            .Default("30seconds")
                        .Toggle("Min points required", "minpointsrequired")
                            .Hint("When on, the min points slider is visible.")
                        .Slider("Min points", "minpoints")
                            .Hint("Only visible when toggle is on.")
                            .Range(0, 100)
                            .Default(10)
                            .ShowWhen("minpointsrequired")
                    )
                    .Section("Buttons & Dialogs", "Buttons", b => b
                        .Intro("Button test section.")
                        .Button("Test Popup")
                            .Hint("Shows a popup.")
                            .Text("Show values")
                            .Color("#714bfd")
                            .OnClick(ctx => ctx.Popup("Test", "Hello"))
                        .Button("Test Confirm")
                            .Hint("Shows confirm dialog.")
                            .Text("Confirm")
                            .Color("#31a8ff")
                            .OnClick(ctx => { })
                        .Button("Test Toast")
                            .Hint("Shows a toast notification.")
                            .Text("Toast")
                            .Color("#f09930")
                            .OnClick(ctx => ctx.Toast("Toast!"))
                    )
                    .Section("Refreshable Dropdown", "Dropdowns", d => d
                        .Intro("Dropdown tests.")
                        .Dropdown("Choice", "dropdown_choice")
                            .Hint("Select an option.")
                            .Options(options)
                            .Refresh(() => new[] { "Option A", "Option B", "Option C", "Option D (refreshed)" })
                            .DefaultIndex(0)
                        .Dropdown("Device (pair value)", "device_display")
                            .Hint("Pair value dropdown.")
                            .WithPairValue("device_id")
                            .Options(new[] { ("id1", "Device A"), ("id2", "Device B"), ("id3", "Device C") })
                            .DefaultByValue("id2")
                    )
                    .Section("Advanced Controls", "Advanced", a => a
                        .Intro("Advanced control tests.")
                        .Toggle("Mode", "mode_index")
                            .Hint("Single-select mode.")
                            .WithExclusive(new[] { "Mode 1", "Mode 2", "Mode 3" })
                            .DefaultIndex(0)
                        .Toggle("Features (multi-select)", "features")
                            .Hint("Multi-select features.")
                            .WithExclusive(new[] { "Feature A", "Feature B", "Feature C" })
                            .MaxSelected(2)
                            .DefaultIndices(new[] { 0, 1 })
                        .DynamicTextboxes("Custom List", "custom_list")
                            .Hint("Dynamic textbox list.")
                            .Preset(new[] { "Item 1", "Item 2" })
                        .Toggle("Show extra options", "show_extra_options")
                            .Hint("Visibility toggle.")
                        .WithVisibility("show_extra_options", inner => inner
                            .Textbox("Extra option A", "extra_option_a")
                                .Hint("Visible when toggle is on.")
                            .Slider("Extra option B", "extra_option_b")
                                .Hint("Visible when toggle is on.")
                                .Range(0, 50)
                                .Default(25)
                            .WithRepeatableRows("extra_rows", row => row
                                .Textbox("Row Label", "label")
                                    .Hint("Label for this row.")
                                .IntegerInput("Amount", "amount")
                                    .Range(0, 999)
                                    .Default(10)
                            )
                        )
                        .Toggle("Premium mode", "premium_mode")
                            .Hint("Inverted visibility test.")
                        .WithVisibility("premium_mode", true, inner => inner
                            .Intro("Visible only when Premium mode is OFF.")
                            .Textbox("Free tier setting", "free_tier_setting")
                                .Hint("Shows when premium_mode is OFF.")
                                .Default("default for free")
                        )
                    )
                    .Section("PillInput & Callbacks", "Pills", p => p
                        .Intro("PillInput tests.")
                        .PillInput("Test Items", "test_items")
                            .Hint("Add items to test pill input.")
                            .WithSectionsPanel((sections, tab, ctx) =>
                            {
                                tab.Children.Add(sections);
                            })
                            .OnPillAdded((item, sections, ctx) =>
                            {
                                var wrapper = new StackPanel();
                                sections.Children.Add(wrapper);
                                pillPanels[item] = wrapper;
                                AddPillSection(wrapper, item, ctx);
                            })
                            .OnPillRemoved((item, sections, ctx) =>
                            {
                                if (pillPanels.TryGetValue(item, out var wrapper))
                                {
                                    sections.Children.Remove(wrapper);
                                    pillPanels.Remove(item);
                                    ctx.RemoveSettingsKeys(item + "_enabled", item + "_value");
                                }
                            })
                    )
                    .BuildAllDeferred();  // Force build all lazy-deferred sections for timing

                sw1.Stop();
                _output.WriteLine($">>> Run 1 total (cold, no image): {sw1.ElapsedMilliseconds}ms");

                FluentConfigWindowManager.SetOpened(false);

                // ── Run 2: Warm run (same STA thread, Application already exists) ──
                _output.WriteLine("");
                _output.WriteLine("========== Run 2: Warm run (same thread, App exists) ==========");
                var sw2 = Stopwatch.StartNew();

                FluentConfigUi.Create(null, "PerfTest_Warm", "1.0")
                    .Section("General", "General", g => g
                        .Toggle("Enable", "enabled").Default(true)
                        .Textbox("Name", "name").Default("test")
                        .Slider("Value", "value").Range(0, 100).Default(50)
                        .ColorPicker("Color", "color").Default("#714bfd")
                        .IntegerInput("Count", "count").Range(0, 999).Default(5)
                        .DurationInput("Timeout", "timeout").Default("30seconds")
                    )
                    .Section("Advanced", "Advanced", a => a
                        .Toggle("Mode", "mode")
                            .WithExclusive(new[] { "A", "B", "C" })
                            .DefaultIndex(0)
                        .Dropdown("Choice", "choice")
                            .Options(new[] { "X", "Y", "Z" })
                            .DefaultIndex(0)
                        .DynamicTextboxes("List", "list")
                            .Preset(new[] { "Item 1", "Item 2" })
                    )
                    .BuildAllDeferred();  // Force build all lazy-deferred sections for timing

                sw2.Stop();
                _output.WriteLine($">>> Run 2 total (warm): {sw2.ElapsedMilliseconds}ms");

                FluentConfigWindowManager.SetOpened(false);

                // ── Run 3: With image download ──
                _output.WriteLine("");
                _output.WriteLine("========== Run 3: With header image download ==========");
                var sw3 = Stopwatch.StartNew();

                FluentConfigUi.Create(null, "PerfTest_Image", "1.0")
                    .Header("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTYxAXPiJKEN56xb3g_LJzPaNVuOu2nW9a4VQ&s")
                    .Section("General", "General", g => g
                        .Toggle("Test toggle", "test_toggle").Default(true)
                        .Textbox("Test input", "test_input").Default("hello")
                    )
                    .BuildAllDeferred();  // Force build all lazy-deferred sections for timing

                sw3.Stop();
                _output.WriteLine($">>> Run 3 total (with image): {sw3.ElapsedMilliseconds}ms");

                FluentConfigWindowManager.SetOpened(false);
                FluentConfigApp.SetLogCallback(null);
            });

            // Verify we got performance trace output
            Assert.Contains(lines, l => l.Contains("[PerfTrace]"));

            // Print summary
            _output.WriteLine("");
            _output.WriteLine("=== ALL TRACE LINES ===");
            foreach (var line in lines.Where(l => l.Contains("[PerfTrace]") || l.Contains(">>>")))
            {
                _output.WriteLine(line);
            }
        }
    }
}
