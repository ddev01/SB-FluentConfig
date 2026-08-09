using System;
using System.Globalization;
using System.Text.RegularExpressions;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Parses Tailwind-flavored layout tokens into structured wire specs.
    /// Size tokens resolve to final CSS values on the host (Svelte is a thin pass-through).
    /// </summary>
    public static class LayoutTokens
    {
        private static readonly Regex GridCols = new Regex(@"^grid-cols-(\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex GapToken = new Regex(@"^gap-(\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex ItemsToken = new Regex(@"^items-(start|center|end|stretch|baseline)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex ScaleNumber = new Regex(@"^(\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex LiteralUnit = new Regex(@"^(\d+(?:\.\d+)?)(px|rem)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly string[] KeywordTokens =
        {
            "fit", "full", "1/2", "1/3", "2/3", "1/4", "3/4",
        };

        /// <summary>Parses a Grid container spec such as <c>"grid-cols-2 gap-3 items-center"</c>.</summary>
        public static GridSpec ParseGridSpec(string spec) => ParseContainerSpec(spec, "grid");

        /// <summary>Parses a Row container spec such as <c>"gap-3 items-center"</c>. Empty → defaults.</summary>
        public static GridSpec ParseRowSpec(string spec) => ParseContainerSpec(spec ?? "", "row");

        /// <summary>
        /// Shared Grid/Row parser. Empty spec throws for <c>grid</c>; defaults for <c>row</c>.
        /// </summary>
        public static GridSpec ParseContainerSpec(string spec, string mode)
        {
            if (string.IsNullOrWhiteSpace(spec))
            {
                if (string.Equals(mode, "grid", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Grid spec cannot be empty.");
                return new GridSpec { Mode = "row", Gap = 3 };
            }

            int? columns = null;
            int gap = 3;
            string align = null;
            var parts = spec.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var colsMatch = GridCols.Match(part);
                if (colsMatch.Success)
                {
                    if (string.Equals(mode, "row", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Unrecognized row token '{part}'. grid-cols-N is only valid on Grid(...).");
                    }
                    columns = int.Parse(colsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    continue;
                }

                var gapMatch = GapToken.Match(part);
                if (gapMatch.Success)
                {
                    gap = int.Parse(gapMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    continue;
                }

                var itemsMatch = ItemsToken.Match(part);
                if (itemsMatch.Success)
                {
                    align = itemsMatch.Groups[1].Value;
                    continue;
                }

                throw new InvalidOperationException(
                    $"Unrecognized {(string.Equals(mode, "row", StringComparison.OrdinalIgnoreCase) ? "row" : "grid")} token '{part}'. " +
                    (string.Equals(mode, "row", StringComparison.OrdinalIgnoreCase)
                        ? "Expected gap-N and/or items-start|center|end|stretch|baseline."
                        : "Expected grid-cols-N, gap-N, and/or items-start|center|end|stretch|baseline."));
            }

            return new GridSpec
            {
                Mode = string.Equals(mode, "row", StringComparison.OrdinalIgnoreCase) ? "row" : "grid",
                Columns = columns,
                Gap = gap,
                Align = align,
            };
        }

        /// <summary>
        /// Parses a compound Size spec such as <c>"fit min-w-20 max-w-96 grow"</c> into a
        /// <see cref="LayoutHint"/> with host-resolved CSS values.
        /// </summary>
        public static LayoutHint ParseSize(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
                throw new InvalidOperationException("Size spec cannot be empty.");

            var hint = new LayoutHint();
            var parts = spec.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part == "grow")
                {
                    hint.Grow = true;
                    continue;
                }
                if (part == "grow-0")
                {
                    hint.Grow = false;
                    continue;
                }
                if (part == "shrink")
                {
                    hint.Shrink = true;
                    continue;
                }
                if (part == "shrink-0")
                {
                    hint.Shrink = false;
                    continue;
                }

                if (part.StartsWith("min-w-", StringComparison.Ordinal))
                {
                    hint.MinWidth = ResolveSizeToken(part.Substring("min-w-".Length), allowScale: true, part);
                    continue;
                }
                if (part.StartsWith("max-w-", StringComparison.Ordinal))
                {
                    hint.MaxWidth = ResolveSizeToken(part.Substring("max-w-".Length), allowScale: true, part);
                    continue;
                }

                // Bare width token (keywords, fractions, px/rem literals). Scale numbers alone are not widths.
                hint.Width = ResolveSizeToken(part, allowScale: false, part);
            }

            if (hint.Width == null && hint.MinWidth == null && hint.MaxWidth == null &&
                !hint.Grow.HasValue && !hint.Shrink.HasValue)
            {
                throw new InvalidOperationException(
                    $"Size spec '{spec}' did not produce any layout properties.");
            }

            return hint;
        }

        /// <summary>
        /// Resolves a size suffix to a CSS value.
        /// Scale (<c>N</c> → <c>{N*4}px</c>) is only allowed for min-w/max-w.
        /// </summary>
        public static string ResolveSizeToken(string suffix, bool allowScale, string originalToken = null)
        {
            if (string.IsNullOrWhiteSpace(suffix))
                throw new InvalidOperationException(
                    $"Unrecognized size token '{originalToken ?? suffix}'.");

            var t = suffix.Trim();
            var label = originalToken ?? t;

            foreach (var known in KeywordTokens)
            {
                if (string.Equals(known, t, StringComparison.Ordinal))
                    return KeywordToCss(known);
            }

            var literal = LiteralUnit.Match(t);
            if (literal.Success)
                return literal.Groups[1].Value + literal.Groups[2].Value;

            if (allowScale)
            {
                var scale = ScaleNumber.Match(t);
                if (scale.Success)
                {
                    int n = int.Parse(scale.Groups[1].Value, CultureInfo.InvariantCulture);
                    return (n * 4).ToString(CultureInfo.InvariantCulture) + "px";
                }
            }

            throw new InvalidOperationException(
                $"Unrecognized size token '{label}'. Expected fit, full, 1/2, 1/3, 2/3, 1/4, 3/4, " +
                "a px/rem literal (e.g. 120px, 5rem)" +
                (allowScale ? ", or a Tailwind scale number (e.g. 20 → 80px)." : "."));
        }

        private static string KeywordToCss(string keyword)
        {
            switch (keyword)
            {
                case "fit": return "fit-content";
                case "full": return "100%";
                case "1/2": return "50%";
                case "1/3": return "33.3333%";
                case "2/3": return "66.6667%";
                case "1/4": return "25%";
                case "3/4": return "75%";
                default: return keyword;
            }
        }

        internal static string OperatorString(Comparator op)
        {
            switch (op)
            {
                case Comparator.GreaterOrEqual: return "gte";
                case Comparator.LessOrEqual: return "lte";
                case Comparator.GreaterThan: return "gt";
                case Comparator.LessThan: return "lt";
                default:
                    throw new InvalidOperationException($"Unknown Comparator '{op}'.");
            }
        }
    }
}
