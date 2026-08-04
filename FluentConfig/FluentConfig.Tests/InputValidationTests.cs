using FluentConfig.Helpers;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Behavior spec for numeric/color/duplicate validation.
    /// </summary>
    public class InputValidationTests
    {
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

        [Fact]
        public void FormatDouble_WholeNumber_NoTrailingZeros()
        {
            Assert.Equal("5", InputValidation.FormatDouble(5.0));
        }

        [Fact]
        public void FormatDouble_Decimal_UsesComma()
        {
            Assert.Equal("3,5", InputValidation.FormatDouble(3.5));
        }

        [Fact]
        public void FormatFloat_WholeNumber_NoTrailingZeros()
        {
            Assert.Equal("5", InputValidation.FormatFloat(5.0f));
        }

        [Fact]
        public void FormatFloat_Decimal_UsesComma()
        {
            Assert.Equal("2,5", InputValidation.FormatFloat(2.5f));
        }

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

        [Fact]
        public void HasDuplicateValues_NoDuplicates_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(new[] { "a", "b", "c" }, 0));
        }

        [Fact]
        public void HasDuplicateValues_WithDuplicate_ReturnsTrue()
        {
            Assert.True(InputValidation.HasDuplicateValues(new[] { "a", "b", "a" }, 0));
        }

        [Fact]
        public void HasDuplicateValues_CaseInsensitive_ReturnsTrue()
        {
            Assert.True(InputValidation.HasDuplicateValues(new[] { "Hello", "hello" }, 0, caseSensitive: false));
        }

        [Fact]
        public void HasDuplicateValues_CaseSensitive_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(new[] { "Hello", "hello" }, 0, caseSensitive: true));
        }

        [Fact]
        public void HasDuplicateValues_EmptyValue_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(new[] { "", "" }, 0));
        }

        [Fact]
        public void HasDuplicateValues_NullList_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(null, 0));
        }

        [Fact]
        public void HasDuplicateValues_OutOfBoundsIndex_ReturnsFalse()
        {
            Assert.False(InputValidation.HasDuplicateValues(new[] { "a" }, 5));
        }

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
