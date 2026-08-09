using System;
using System.Linq;
using FluentConfig.Protocol;
using Xunit;

namespace FluentConfig.Tests
{
    public class RepeatForTests
    {
        [Fact]
        public void RepeatFor_EmitsUngatedBelowMin_AndGatedAbove()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "RepeatFor Basic", "1.0");
            ui.Section("G", "g", s => s
                .IntegerInput("Max count", "max_count").Range(1, 4).Default(2)
                .RepeatFor("max_count", (row, i) => row
                    .IntegerInput($"Points #{i}", $"points_{i}").Range(0, 100).Default(0)
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var children = doc.Sections[0].Children.ToList();

            // Driver + points_1 (ungated) + groups for 2..4
            Assert.Equal(5, children.Count);
            Assert.IsType<NumberInputNode>(children[0]);
            Assert.Equal("max_count", ((NumberInputNode)children[0]).SaveKey);

            var points1 = Assert.IsType<NumberInputNode>(children[1]);
            Assert.Equal("points_1", points1.SaveKey);
            Assert.Null(points1.Visibility);

            for (int i = 2; i <= 4; i++)
            {
                var group = Assert.IsType<GroupNode>(children[i]);
                Assert.Equal("gte", group.Visibility.Operator);
                Assert.Equal(i, group.Visibility.Value);
                Assert.Equal("max_count", group.Visibility.SaveKey);
                var field = Assert.IsType<NumberInputNode>(Assert.Single(group.Children));
                Assert.Equal($"points_{i}", field.SaveKey);
            }
        }

        [Fact]
        public void RepeatFor_AutoDetectsMaxFromEarlierSection()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "RepeatFor CrossSection", "1.0");
            ui.Section("Settings", "settings", s => s
                .IntegerInput("Max count", "max_count").Range(2, 3).Default(2)
            );
            ui.Section("Points", "points", s => s
                .RepeatFor("max_count", (row, i) => row
                    .IntegerInput($"Points #{i}", $"points_{i}").Default(0)
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var pointsSection = doc.Sections.Single(sec => sec.Id == "points");
            // min=2 → indices 1,2 ungated; index 3 gated → 3 nodes
            Assert.Equal(3, pointsSection.Children.Count);
            Assert.IsType<NumberInputNode>(pointsSection.Children[0]);
            Assert.IsType<NumberInputNode>(pointsSection.Children[1]);
            var gated = Assert.IsType<GroupNode>(pointsSection.Children[2]);
            Assert.Equal(3, gated.Visibility.Value);
        }

        [Fact]
        public void RepeatFor_ExplicitMaxOverride_IgnoresDeclaredMax()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "RepeatFor MaxOverride", "1.0");
            ui.Section("G", "g", s => s
                .IntegerInput("Max count", "max_count").Range(1, 10).Default(1)
                .RepeatFor("max_count", (row, i) => row
                    .IntegerInput($"P{i}", $"points_{i}"), max: 2)
            );

            var doc = ui.Session.BuildDocumentForTests();
            // driver + points_1 + group for 2
            Assert.Equal(3, doc.Sections[0].Children.Count);
        }

        [Fact]
        public void RepeatFor_MissingRangeAndNoExplicitMax_Throws()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "RepeatFor Missing", "1.0");
            ui.Section("G", "g", s => s
                .RepeatFor("unknown_driver", (row, i) => row
                    .IntegerInput($"P{i}", $"points_{i}")
                )
            );

            var ex = Assert.Throws<InvalidOperationException>(() => ui.Session.BuildDocumentForTests());
            Assert.Contains("unknown_driver", ex.Message);
            Assert.Contains("Range", ex.Message);
        }
    }
}
