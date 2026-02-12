using FluentConfig.Helpers;
using Xunit;

namespace FluentConfig.Tests
{
    public class ControlExtractionHelperTests
    {
        // --- ParseDuration ---

        [Fact]
        public void ParseDuration_Permanent_ReturnsUnitIndex5()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("permanent");
            Assert.Equal(0, num);
            Assert.Equal(5, unitIndex);
        }

        [Fact]
        public void ParseDuration_Null_ReturnsPermanent()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration(null);
            Assert.Equal(0, num);
            Assert.Equal(5, unitIndex);
        }

        [Fact]
        public void ParseDuration_Empty_ReturnsPermanent()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("");
            Assert.Equal(0, num);
            Assert.Equal(5, unitIndex);
        }

        [Fact]
        public void ParseDuration_FullUnitSeconds_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("30seconds");
            Assert.Equal(30, num);
            Assert.Equal(0, unitIndex);
        }

        [Fact]
        public void ParseDuration_FullUnitMinutes_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("5minutes");
            Assert.Equal(5, num);
            Assert.Equal(1, unitIndex);
        }

        [Fact]
        public void ParseDuration_FullUnitHours_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("2hours");
            Assert.Equal(2, num);
            Assert.Equal(2, unitIndex);
        }

        [Fact]
        public void ParseDuration_FullUnitDays_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("7days");
            Assert.Equal(7, num);
            Assert.Equal(3, unitIndex);
        }

        [Fact]
        public void ParseDuration_FullUnitWeeks_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("1weeks");
            Assert.Equal(1, num);
            Assert.Equal(4, unitIndex);
        }

        [Fact]
        public void ParseDuration_ShortUnitS_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("45s");
            Assert.Equal(45, num);
            Assert.Equal(0, unitIndex);
        }

        [Fact]
        public void ParseDuration_ShortUnitM_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("10m");
            Assert.Equal(10, num);
            Assert.Equal(1, unitIndex);
        }

        [Fact]
        public void ParseDuration_ShortUnitH_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("3h");
            Assert.Equal(3, num);
            Assert.Equal(2, unitIndex);
        }

        [Fact]
        public void ParseDuration_ShortUnitD_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("14d");
            Assert.Equal(14, num);
            Assert.Equal(3, unitIndex);
        }

        [Fact]
        public void ParseDuration_ShortUnitW_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("2w");
            Assert.Equal(2, num);
            Assert.Equal(4, unitIndex);
        }

        [Fact]
        public void ParseDuration_NumberOnly_DefaultsToMinutes()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("60");
            Assert.Equal(60, num);
            Assert.Equal(1, unitIndex);
        }

        [Fact]
        public void ParseDuration_CaseInsensitive_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("10SECONDS");
            Assert.Equal(10, num);
            Assert.Equal(0, unitIndex);
        }

        [Fact]
        public void ParseDuration_WithSpaces_Parses()
        {
            var (num, unitIndex) = ControlExtractionHelper.ParseDuration("  30seconds  ");
            Assert.Equal(30, num);
            Assert.Equal(0, unitIndex);
        }
    }
}
