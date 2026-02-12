using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FluentConfig.Helpers
{
    /// <summary>
    /// Centralized validation and input handling. DRY methods for numeric parsing, display formatting,
    /// input filtering, and validation across sbui elements.
    /// </summary>
    public static class InputValidation
    {
        private static readonly NumberFormatInfo CommaFormat = new NumberFormatInfo { NumberDecimalSeparator = "," };

        public enum InputType
        {
            String,
            Int,
            Double,
            Float
        }

        public static bool TryParseInt(string text, out int result, int min, int max)
        {
            result = 0;
            if (string.IsNullOrEmpty(text)) return false;
            if (!int.TryParse(text.Trim(), out var v)) return false;
            result = Math.Max(min, Math.Min(max, v));
            return true;
        }

        public static bool TryParseDouble(string text, out double result, double min, double max)
        {
            result = 0;
            if (string.IsNullOrEmpty(text)) return false;
            var normalized = (text ?? "").Trim().Replace('.', ',');
            if (!double.TryParse(normalized, NumberStyles.Float, CommaFormat, out var v)) return false;
            result = Math.Max(min, Math.Min(max, v));
            return true;
        }

        public static bool TryParseFloat(string text, out float result, float min, float max)
        {
            result = 0;
            if (string.IsNullOrEmpty(text)) return false;
            var normalized = (text ?? "").Trim().Replace('.', ',');
            if (!float.TryParse(normalized, NumberStyles.Float, CommaFormat, out var v)) return false;
            result = Math.Max(min, Math.Min(max, v));
            return true;
        }

        public static string FormatDouble(double value)
        {
            return value.ToString("G", CommaFormat);
        }

        public static string FormatFloat(float value)
        {
            return value.ToString("G", CommaFormat);
        }

        public static bool IsValidHexColor(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            return Regex.IsMatch(text, @"^#[0-9A-Fa-f]{6}$") || Regex.IsMatch(text, @"^#[0-9A-Fa-f]{8}$");
        }

        public static string GetTagPrefixForType(InputType type)
        {
            switch (type)
            {
                case InputType.Int: return FluentConfigTags.IntegerPrefix;
                case InputType.Double: return FluentConfigTags.DoublePrefix;
                case InputType.Float: return FluentConfigTags.FloatPrefix;
                default: return "";
            }
        }

        public static void CreatePreviewTextInputHandler(TextBox tb, InputType allowedType)
        {
            tb.PreviewTextInput += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Text)) return;
                var proposed = GetProposedText(tb, e.Text);
                if (allowedType == InputType.Int)
                {
                    if (!IsValidIntInput(proposed)) e.Handled = true;
                }
                else if (allowedType == InputType.Double || allowedType == InputType.Float)
                {
                    if (!IsValidDecimalInput(proposed)) e.Handled = true;
                }
            };
        }

        private static string GetProposedText(TextBox tb, string newText)
        {
            var start = tb.SelectionStart;
            var len = tb.SelectionLength;
            var text = tb.Text ?? "";
            return text.Substring(0, Math.Min(start, text.Length)) + newText +
                (start + len < text.Length ? text.Substring(start + len) : "");
        }

        public static void AddDataObjectPastingHandler(TextBox tb, InputType allowedType)
        {
            tb.AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler((s, e) =>
            {
                if (e.DataObject != null && e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    var pasted = e.DataObject.GetData(DataFormats.Text) as string ?? "";
                    var proposed = GetProposedText(tb, pasted);
                    if (allowedType == InputType.Int && !IsValidIntInput(proposed))
                        e.CancelCommand();
                    else if ((allowedType == InputType.Double || allowedType == InputType.Float) && !IsValidDecimalInput(proposed))
                        e.CancelCommand();
                }
            }));
        }

        private static bool IsValidIntInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (text == "-") return true;
            return Regex.IsMatch(text, @"^-?\d*$");
        }

        private static bool IsValidDecimalInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (text == "-" || text == "," || text == ".") return true;
            return Regex.IsMatch(text, @"^-?\d*[.,]?\d*$");
        }

        public static bool HasDuplicateValues(System.Collections.Generic.IList<string> values, int currentIndex, bool caseSensitive = false)
        {
            if (values == null || currentIndex < 0 || currentIndex >= values.Count) return false;
            var current = values[currentIndex]?.Trim() ?? "";
            if (string.IsNullOrEmpty(current)) return false;
            var comp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            for (int i = 0; i < values.Count; i++)
            {
                if (i == currentIndex) continue;
                if (string.Equals((values[i] ?? "").Trim(), current, comp)) return true;
            }
            return false;
        }

        public static InputType ParseInputType(string type)
        {
            if (string.IsNullOrEmpty(type)) return InputType.String;
            var t = type.Trim().ToLowerInvariant();
            switch (t)
            {
                case "int":
                case "integer": return InputType.Int;
                case "double": return InputType.Double;
                case "float": return InputType.Float;
                case "string":
                default: return InputType.String;
            }
        }
    }
}
