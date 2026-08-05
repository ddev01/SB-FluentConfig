// Pure duration string parser. Old ControlExtractionHelper mixed this with WPF panel walkers.
using System;

namespace FluentConfig.Helpers
{
    /// <summary>
    /// Pure duration string parser ("30seconds", "5m", "permanent").
    /// Not yet wired into the RPC/save path — kept as an experimental utility for authors/tests.
    /// </summary>
    internal static class DurationParsing
    {
        /// <summary>
        /// Parses a duration string into (numeric value, unit index).
        /// Unit indices: 0=seconds, 1=minutes, 2=hours, 3=days, 4=weeks, 5=permanent.
        /// </summary>
        public static (int num, int unitIndex) ParseDuration(string value)
        {
            if (string.IsNullOrEmpty(value)) return (0, 5);
            value = value.Trim().ToLowerInvariant();
            if (value == "permanent") return (0, 5);

            var fullUnits = new[] { "seconds", "minutes", "hours", "days", "weeks" };
            for (int u = 0; u < fullUnits.Length; u++)
            {
                if (value.EndsWith(fullUnits[u]))
                {
                    var numStr = value.Substring(0, value.Length - fullUnits[u].Length).Trim();
                    int.TryParse(numStr, out var num);
                    return (num, u);
                }
            }

            var unitChars = "smhdw";
            for (int i = value.Length - 1; i >= 0; i--)
            {
                var c = value[i];
                if (char.IsDigit(c) || c == ' ') continue;
                if (unitChars.IndexOf(c) >= 0)
                {
                    var numStr = value.Substring(0, i).Trim();
                    int.TryParse(numStr, out var num);
                    int unitIndex;
                    switch (c)
                    {
                        case 's': unitIndex = 0; break;
                        case 'm': unitIndex = 1; break;
                        case 'h': unitIndex = 2; break;
                        case 'd': unitIndex = 3; break;
                        case 'w': unitIndex = 4; break;
                        default: unitIndex = 5; break;
                    }
                    return (num, unitIndex);
                }
                break;
            }
            int.TryParse(value, out var n);
            return (n, 1);
        }
    }
}
