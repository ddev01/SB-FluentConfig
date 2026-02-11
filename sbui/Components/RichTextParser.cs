using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Sbui.Components
{
    /// <summary>
    /// Parses markdown-like syntax into WPF Inlines; no UI state.
    /// </summary>
    public static class RichTextParser
    {
        private static readonly Regex RichTextRegex = new Regex(
            @"(\*\*(?<bold>.+?)\*\*)|_(?<italic>.+?)_|\{(?<col>#[0-9a-fA-F]{6,8})\|(?<colTxt>.+?)\}(?!\})|\{size:(?<size>\d+)\|(?<sizeTxt>.+?)\}(?!\})|\+\+(?<glow>.+?)\+\+|\[link:(?<linkText>.+?)\|(?<linkUrl>.+?)\]",
            RegexOptions.Singleline);

        /// <summary>
        /// Parses raw text with **bold**, _italic_, {#hex|colored}, {size:N|text}, ++glow++, [link:text|url] into WPF Inlines.
        /// </summary>
        public static IEnumerable<Inline> Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                yield return new Run("");
                yield break;
            }
            var matches = RichTextRegex.Matches(raw);
            int start = 0;
            foreach (Match m in matches)
            {
                if (m.Index > start)
                {
                    foreach (var i in AddPlain(raw.Substring(start, m.Index - start)))
                        yield return i;
                }
                if (m.Groups["bold"].Success)
                {
                    var span = new Span { FontWeight = FontWeights.Bold };
                    foreach (var i in Parse(m.Groups["bold"].Value))
                        span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["italic"].Success)
                {
                    var span = new Span { FontStyle = FontStyles.Italic };
                    foreach (var i in Parse(m.Groups["italic"].Value))
                        span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["col"].Success)
                {
                    Color parsedColor;
                    try { parsedColor = (Color)ColorConverter.ConvertFromString(m.Groups["col"].Value); }
                    catch { parsedColor = Colors.White; }
                    var span = new Span
                    {
                        Foreground = new SolidColorBrush(parsedColor)
                    };
                    foreach (var i in Parse(m.Groups["colTxt"].Value))
                        span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["size"].Success)
                {
                    double fontSize;
                    if (!double.TryParse(m.Groups["size"].Value, out fontSize) || fontSize <= 0)
                        fontSize = 14;
                    var span = new Span { FontSize = fontSize };
                    foreach (var i in Parse(m.Groups["sizeTxt"].Value))
                        span.Inlines.Add(i);
                    yield return span;
                }
                else if (m.Groups["glow"].Success)
                {
                    yield return new Run(m.Groups["glow"].Value) { FontWeight = FontWeights.SemiBold };
                }
                else if (m.Groups["linkText"].Success && m.Groups["linkUrl"].Success)
                {
                    var linkText = m.Groups["linkText"].Value;
                    var linkUrl = m.Groups["linkUrl"].Value;
                    Uri parsedUri = null;
                    try { parsedUri = new Uri(linkUrl, UriKind.RelativeOrAbsolute); }
                    catch { /* invalid URI — will fall back to plain text below */ }
                    if (parsedUri == null)
                    {
                        yield return new Run(linkText);
                        start = m.Index + m.Length;
                        continue;
                    }
                    var hyperlink = new Hyperlink(new Run(linkText))
                    {
                        NavigateUri = parsedUri
                    };
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        e.Handled = true;
                        try
                        {
                            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Sbui] RichTextParser: failed to open link: {ex.Message}");
                        }
                    };
                    yield return hyperlink;
                }
                start = m.Index + m.Length;
            }
            if (start < raw.Length)
            {
                foreach (var i in AddPlain(raw.Substring(start)))
                    yield return i;
            }
        }

        private static IEnumerable<Inline> AddPlain(string segment)
        {
            var lines = segment.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                yield return new Run(lines[i]);
                if (i < lines.Length - 1)
                    yield return new LineBreak();
            }
        }
    }
}
