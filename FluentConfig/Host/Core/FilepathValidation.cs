using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Core
{
    internal sealed class FilepathRule
    {
        public string SaveKey;
        public string Label;
        public bool MustExist = true;
        public string[] Accept;
        public string PillSaveKey;
    }

    internal static class FilepathValidation
    {
        public static string[] NormalizeAccept(IEnumerable<string> extensions)
        {
            if (extensions == null)
                return null;
            var list = new List<string>();
            foreach (var raw in extensions)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var ext = raw.Trim();
                if (ext.StartsWith("*", StringComparison.Ordinal))
                    ext = ext.Substring(1);
                if (!ext.StartsWith(".", StringComparison.Ordinal))
                    ext = "." + ext;
                list.Add(ext.ToLowerInvariant());
            }
            return list.Count == 0 ? null : list.Distinct().ToArray();
        }

        public static string Check(string path, bool mustExist, string[] accept)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            path = path.Trim();
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return "Use a local file path, not a URL.";

            string local = path;
            if (path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try { local = new Uri(path).LocalPath; }
                catch { return "That is not a valid file path."; }
            }

            if (mustExist)
            {
                try
                {
                    if (!File.Exists(local))
                        return "File not found.";
                }
                catch
                {
                    return "That is not a valid file path.";
                }
            }

            if (accept == null || accept.Length == 0)
                return null;
            string ext;
            try { ext = Path.GetExtension(local); }
            catch { return "That is not a valid file path."; }
            if (string.IsNullOrEmpty(ext))
                return "Expected " + FormatAccept(accept) + ".";
            foreach (var allowed in accept)
            {
                if (string.Equals(ext, allowed, StringComparison.OrdinalIgnoreCase))
                    return null;
            }
            return "Expected " + FormatAccept(accept) + ".";
        }

        public static IList<string> ValidateAll(IList<FilepathRule> rules, JObject values)
        {
            var errors = new List<string>();
            if (rules == null || rules.Count == 0)
                return errors;
            foreach (var rule in rules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.SaveKey))
                    continue;
                foreach (var key in ExpandKeys(rule, values))
                {
                    var token = SettingsPathHelper.GetNestedValue(values, key) ?? values?[key];
                    string path = token == null || token.Type == JTokenType.Null ? "" : token.ToString();
                    var err = Check(path, rule.MustExist, rule.Accept);
                    if (err == null)
                        continue;
                    var label = string.IsNullOrWhiteSpace(rule.Label) ? key : rule.Label;
                    errors.Add(label + ": " + err);
                }
            }
            return errors;
        }

        public static FilepathRule FindRule(IList<FilepathRule> rules, string saveKey)
        {
            if (rules == null || string.IsNullOrEmpty(saveKey))
                return null;
            foreach (var rule in rules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.SaveKey))
                    continue;
                if (string.Equals(rule.SaveKey, saveKey, StringComparison.Ordinal))
                    return rule;
                var idx = rule.SaveKey.IndexOf("{name}", StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var prefix = rule.SaveKey.Substring(0, idx);
                var suffix = rule.SaveKey.Substring(idx + 6);
                if (saveKey.Length <= prefix.Length + suffix.Length)
                    continue;
                if (saveKey.StartsWith(prefix, StringComparison.Ordinal)
                    && saveKey.EndsWith(suffix, StringComparison.Ordinal))
                    return rule;
            }
            return null;
        }

        public static string DialogFilter(string label, string[] accept)
        {
            if (accept == null || accept.Length == 0)
                return "All files|*.*";
            var globs = string.Join(";", accept.Select(e => "*" + e));
            var title = string.IsNullOrWhiteSpace(label) ? "Files" : label;
            return title + "|" + globs + "|All files|*.*";
        }

        private static IEnumerable<string> ExpandKeys(FilepathRule rule, JObject values)
        {
            if (rule.SaveKey.IndexOf("{name}", StringComparison.Ordinal) < 0)
            {
                yield return rule.SaveKey;
                yield break;
            }
            if (string.IsNullOrEmpty(rule.PillSaveKey) || values == null)
                yield break;
            var names = values[rule.PillSaveKey] as JArray;
            if (names == null)
                yield break;
            foreach (var token in names)
            {
                var name = token?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                yield return rule.SaveKey.Replace("{name}", name.Trim());
            }
        }

        private static string FormatAccept(string[] accept)
        {
            var parts = accept.Select(e => e.TrimStart('.')).ToArray();
            if (parts.Length == 1)
                return "." + parts[0];
            if (parts.Length == 2)
                return "." + parts[0] + " or ." + parts[1];
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                    sb.Append(i == parts.Length - 1 ? ", or " : ", ");
                sb.Append('.').Append(parts[i]);
            }
            return sb.ToString();
        }
    }
}
