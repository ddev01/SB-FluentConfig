using FluentConfig.Helpers;
using Xunit;

namespace FluentConfig.Tests
{
    public class InputValidationTests
    {
        // --- TryParseInt ---

        [Fact]
        public void TryParseInt_ValidInput_ParsesAndClamps()
        {
            Assert.True(InputValidation.TryParseInt("5", out var result, 0, 10));
            Assert.Equal(5, result);
        }

        [Fact]
        public void TryParseInt_AboveMax_ClampsToMax()
        {
            Assert.True(InputValidation.TryParseInt("20", out var result, 0, 10));
            Assert.Equal(10, result);
        }

        [Fact]
        public void TryParseInt_BelowMin_ClampsToMin()
        {
            Assert.True(InputValidation.TryParseInt("-5", out var result, 0, 10));
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryParseInt_EmptyString_ReturnsFalse()
        {
            Assert.False(InputValidation.TryParseInt("", out var result, 0, 10));
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryParseInt_NullString_ReturnsFalse()
        {
            Assert.False(InputValidation.TryParseInt(null, out var result, 0, 10));
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryParseInt_NonNumeric_ReturnsFalse()
        {
            Assert.False(InputValidation.TryParseInt("abc", out var result, 0, 10));
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryParseInt_NegativeRange_Works()
        {
            Assert.True(InputValidation.TryParseInt("-3", out var result, -10, -1));
            Assert.Equal(-3, result);
        }

        // --- TryParseDouble ---

        [Fact]
        public void TryParseDouble_ValidInput_Parses()
        {
            Assert.True(InputValidation.TryParseDouble("3,5", out var result, 0, 10));
            Assert.Equal(3.5, result, 5);
        }

        [Fact]
        public void TryParseDouble_DotSeparator_NormalizedToComma()
        {
            Assert.True(InputValidation.TryParseDouble("2.5", out var result, 0, 10));
            Assert.Equal(2.5, result, 5);
        }

        [Fact]
        public void TryParseDouble_AboveMax_Clamps()
        {
            Assert.True(InputValidation.TryParseDouble("15", out var result, 0, 10));
            Assert.Equal(10, result, 5);
        }

        [Fact]
        public void TryParseDouble_BelowMin_Clamps()
        {
            Assert.True(InputValidation.TryParseDouble("-5", out var result, 0, 10));
            Assert.Equal(0, result, 5);
        }

        [Fact]
        public void TryParseDouble_Empty_ReturnsFalse()
        {
            Assert.False(InputValidation.TryParseDouble("", out _, 0, 10));
        }

        [Fact]
        public void TryParseDouble_Null_ReturnsFalse()
        {
            Assert.False(InputValidation.TryParseDouble(null, out _, 0, 10));
        }

        // --- TryParseFloat ---

        [Fact]
        public void TryParseFloat_ValidInput_Parses()
        {
            Assert.True(InputValidation.TryParseFloat("1,5", out var result, 0, 10));
            Assert.Equal(1.5f, result, 4);
        }

        [Fact]
        public void TryParseFloat_DotSeparator_Works()
        {
            Assert.True(InputValidation.TryParseFloat("7.25", out var result, 0, 10));
            Assert.Equal(7.25f, result, 4);
        }

        [Fact]
        public void TryParseFloat_Clamps()
        {
            Assert.True(InputValidation.TryParseFloat("50", out var result, 0, 10));
            Assert.Equal(10f, result, 4);
        }

        // --- FormatDouble ---

        [Fact]
        public void FormatDouble_WholeNumber_NoTrailingZeros()
        {
            var formatted = InputValidation.FormatDouble(5.0);
            Assert.Equal("5", formatted);
        }

        [Fact]
        public void FormatDouble_Decimal_UsesComma()
        {
            var formatted = InputValidation.FormatDouble(3.5);
            Assert.Equal("3,5", formatted);
        }

        // --- FormatFloat ---

        [Fact]
        public void FormatFloat_WholeNumber_NoTrailingZeros()
        {
            var formatted = InputValidation.FormatFloat(5.0f);
            Assert.Equal("5", formatted);
        }

        [Fact]
        public void FormatFloat_Decimal_UsesComma()
        {
            var formatted = InputValidation.FormatFloat(2.5f);
            Assert.Equal("2,5", formatted);
        }

        // --- IsValidHexColor ---

        [Fact]
        public void IsValidHexColor_Valid6Digit_ReturnsTrue()
        {
            Assert.True(InputValidation.IsValidHexColor("#FF0000"));
        }

        [Fact]
        public void IsValidHexColor_Valid8Digit_ReturnsTrue()
        {
            Assert.True(InputValidation.IsValidHexColor("#80FF0000"));
        }

        [Fact]
        public void IsValidHexColor_Invalid_ReturnsFalse()
        {
            Assert.False(InputValidation.IsValidHexColor("red"));
        }

        [Fact]
        public void IsValidHexColor_Empty_ReturnsFalse()
        {
            Assert.False(InputValidation.IsValidHexColor(""));
        }

        [Fact]
        public void IsValidHexColor_Null_ReturnsFalse()
        {
            Assert.False(InputValidation.IsValidHexColor(null));
        }

        [Fact]
        public void IsValidHexColor_TooShort_ReturnsFalse()
        {
            Assert.False(InputValidation.IsValidHexColor("#FFF"));
        }

        // --- HasDuplicateValues ---

        [Fact]
        public void HasDuplicateValues_NoDuplicates_ReturnsFalse()
        {
            var values = new[] { "a", "b", "c" };
            Assert.False(InputValidation.HasDuplicateValues(values, 0));
        }

        [Fact]
        public void HasDuplicateValues_WithDuplicate_ReturnsTrue()
        {
            var values = new[] { "a", "b", "a" };
            Assert.True(InputValidation.HasDuplicateValues(values, 0));
        }

        [Fact]
        public void HasDuplicateValues_CaseInsensitive_ReturnsTrue()
        {
            var values = new[] { "Hello", "hello" };
            Assert.True(InputValidation.HasDuplicateValues(values, 0, caseSensitive: false));
        }

        [Fact]
        public void HasDuplicateValues_CaseSensitive_ReturnsFalse()
        {
            var values = new[] { "Hello", "hello" };
            Assert.False(InputValidation.HasDuplicateValues(values, 0, caseSensitive: true));
        }

        [Fact]
        public void HasDuplicateValues_EmptyValue_ReturnsFalse()
        {
            var values = new[] { "", "" };
            Assert.False(InputValidation.HasDuplicateValues(values, 0));
        }

        [Fact]
        public void HasDuplicateValues_NullList_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(null, 0));
        }

        [Fact]
        public void HasDuplicateValues_OutOfBoundsIndex_ReturnsFalse()
        {
            var values = new[] { "a" };
            Assert.False(InputValidation.HasDuplicateValues(values, 5));
        }

        // --- ParseInputType ---

        [Fact]
        public void ParseInputType_Int_ReturnsInt()
        {
            Assert.Equal(InputValidation.InputType.Int, InputValidation.ParseInputType("int"));
        }

        [Fact]
        public void ParseInputType_Integer_ReturnsInt()
        {
            Assert.Equal(InputValidation.InputType.Int, InputValidation.ParseInputType("integer"));
        }

        [Fact]
        public void ParseInputType_Double_ReturnsDouble()
        {
            Assert.Equal(InputValidation.InputType.Double, InputValidation.ParseInputType("double"));
        }

        [Fact]
        public void ParseInputType_Float_ReturnsFloat()
        {
            Assert.Equal(InputValidation.InputType.Float, InputValidation.ParseInputType("float"));
        }

        [Fact]
        public void ParseInputType_String_ReturnsString()
        {
            Assert.Equal(InputValidation.InputType.String, InputValidation.ParseInputType("string"));
        }

        [Fact]
        public void ParseInputType_Null_ReturnsString()
        {
            Assert.Equal(InputValidation.InputType.String, InputValidation.ParseInputType(null));
        }

        [Fact]
        public void ParseInputType_Unknown_ReturnsString()
        {
            Assert.Equal(InputValidation.InputType.String, InputValidation.ParseInputType("unknown"));
        }
    }
}
