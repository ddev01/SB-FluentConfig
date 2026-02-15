// Streamer.bot C# action - Layout test (Grid, Flex, Div).
// Tests all layout primitives: Grid, Flex, Div, ColSpan.
// Copy into a new C# action. Requires: PresentationFramework, PresentationCore, WindowsBase, FluentConfig.dll.
// See docs/REFERENCES.md for reference details.

using FluentConfig;
using System;
using System.Windows.Controls;

public class CPHInline
{
    public bool Execute()
    {
        if (FluentConfig.FluentConfig.AlreadyOpened("Layout Test", "1.0"))
            return true;

        FluentConfigUi.Create(CPH, "Layout Test", "1.0")
            .Section("Layout examples", "Layout", s => s
                .Intro("Grid, Flex, and Div layout demos. Verify controls render side-by-side or grouped as expected.")
                .Title("1. Grid(3) with ColSpan(2)")
                .Grid(3, g => g
                    .Title("Voice pricing")
                    .IntegerInput("Price", "layout_price")
                        .Hint("Points cost.")
                        .Range(0, 10000)
                        .Default(100)
                    .DurationInput("Duration", "layout_duration")
                        .Hint("How long.")
                        .WithPermanentOption(true)
                        .Default("permanent")
                    .Toggle("Enabled", "layout_enabled")
                        .ColSpan(2)
                    .Button("Add tier")
                        .Text("Add")
                        .OnClick(ui => ui.Toast("Add clicked"))
                )
                .Title("2. Flex (horizontal, auto-width, 8px gap)")
                .Flex(f => f
                    .IntegerInput("Min", "layout_min")
                        .Range(0, 100)
                        .Default(0)
                    .IntegerInput("Max", "layout_max")
                        .Range(0, 100)
                        .Default(100)
                    .DurationInput("Timeout", "layout_timeout")
                        .WithPermanentOption(false)
                        .Default("30seconds")
                    .DurationInput("Timeout", "layout_timeout")
                        .WithPermanentOption(false)
                        .Default("30seconds")
                    .DurationInput("Timeout", "layout_timeout")
                        .WithPermanentOption(false)
                        .Default("30seconds")
                )
                .Title("3. Grid(2) with Div (grouped cells)")
                .Grid(2, g => g
                    .Div(d => d
                        .Title("Left column")
                        .IntegerInput("Price", "div_left_price")
                            .Default(50)
                        .DurationInput("Duration", "div_left_duration")
                            .WithPermanentOption(true)
                            .Default("permanent")
                    )
                    .Div(d => d
                        .Title("Right column")
                        .Toggle("Enabled", "div_right_enabled")
                            .Default(true)
                        .Button("Action")
                            .Text("Do thing")
                            .OnClick(ui => ui.Popup("Done", "Action executed"))
                    )
                )
                .Title("4. Grid(4) - four equal columns")
                .Grid(4, g => g
                    .Toggle("A", "grid4_a")
                    .Toggle("B", "grid4_b")
                    .Toggle("C", "grid4_c")
                    .Toggle("D", "grid4_d")
                )
                .Title("5. Flex with gap=16")
                .Flex(f => f
                    .Textbox("First", "flex_first")
                        .Default("one")
                    .Textbox("Second", "flex_second")
                        .Default("two")
                , gap: 16)
                .Title("6. Nested: Div containing Grid")
                .Div(d => d
                    .Title("Nested grid inside div")
                    .Grid(2, g => g
                        .IntegerInput("X", "nested_x")
                            .Default(10)
                        .IntegerInput("Y", "nested_y")
                            .Default(20)
                    )
                )
                .Title("7. Grid(3) three inputs then full-width toggle")
                .Grid(3, g => g
                    .IntegerInput("Val 1", "g3_v1")
                    .IntegerInput("Val 2", "g3_v2")
                    .IntegerInput("Val 3", "g3_v3")
                    .Input("Full width", "g3_full")
                        .Type("string")
                        .Width("full")
                        .ColSpan(3)
                    .Input("Fixed 100px", "g3_fixed")
                        .Type("string")
                        .Width(100)
                        .ColSpan(3)
                )
                .Title("8. Grid(4) with gap, justify, padding")
                .Grid(4, g => g
                    .IntegerInput("A", "g8_a")
                    .IntegerInput("B", "g8_b").Justify("right")
                    .IntegerInput("C", "g8_c").Justify("center")
                    .IntegerInput("D", "g8_d")
                , gap: 8, padding: 4)
                .Title("9. Grid custom columns (auto, 1*, 2*, auto)")
                .Grid(new[] { "auto", "1*", "2*", "auto" }, g => g
                    .Toggle("Auto", "gc_a")
                    .Input("Star 1", "gc_b").Width("full")
                    .Input("Star 2", "gc_c").Width("full")
                    .Toggle("Auto", "gc_d")
                )
                .Title("10. Flex with wrap and align center")
                .Flex(f => f
                    .IntegerInput("X", "flex_x")
                    .IntegerInput("Y", "flex_y")
                    .IntegerInput("Z", "flex_z")
                , gap: 16, wrap: true, align: "center")
                .Title("11. Grid with align center and nested div")
                .Grid(4, g => g
                    .IntegerInput("A", "g11_a")
                    .IntegerInput("B", "g11_b").Align("center")
                    .Div(d => d
                        .IntegerInput("C", "g11_c")
                        .IntegerInput("D", "g11_d")
                        .IntegerInput("E", "g11_e")
                        .IntegerInput("F", "g11_f")
                    )
                , gap: 8, padding: 4)
            )
            .Show();

        return true;
    }
}
