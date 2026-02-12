using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Core
{
    /// <summary>
    /// Helper for reading/writing nested settings paths (e.g. "voice_aliases[0].name").
    /// </summary>
    public static class SettingsPathHelper
    {
        /// <summary>
        /// Sets a value at the given path in the root JObject. Creates nested objects/arrays as needed.
        /// </summary>
        public static void SetNestedValue(JObject root, string path, JToken value)
        {
            if (root == null || string.IsNullOrEmpty(path)) return;
            if (!path.Contains("[") && !path.Contains("."))
            {
                root[path] = value;
                return;
            }
            var parts = ParseNestedPath(path);
            if (parts.Length == 0) return;
            JToken current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                var nextIsIndex = i + 1 < parts.Length && int.TryParse(parts[i + 1], out _);
                if (int.TryParse(part, out int index))
                {
                    if (!(current is JArray arr)) return;
                    while (arr.Count <= index) arr.Add(new JObject());
                    current = arr[index];
                }
                else if (current is JObject obj)
                {
                    if (obj[part] == null)
                        obj[part] = nextIsIndex ? (JToken)new JArray() : new JObject();
                    current = obj[part];
                }
                else return;
            }
            var last = parts[parts.Length - 1];
            if (int.TryParse(last, out int lastIdx))
            {
                if (!(current is JArray arr)) return;
                while (arr.Count <= lastIdx) arr.Add(null);
                arr[lastIdx] = value;
            }
            else if (current is JObject obj)
            {
                obj[last] = value;
            }
        }

        /// <summary>
        /// Parses a path like "key[0].sub" into parts ["key", "0", "sub"].
        /// </summary>
        public static string[] ParseNestedPath(string path)
        {
            var parts = new List<string>();
            var current = "";
            bool inBracket = false;
            foreach (var ch in path)
            {
                if (ch == '[')
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                    inBracket = true;
                }
                else if (ch == ']')
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                    inBracket = false;
                }
                else if (ch == '.' && !inBracket)
                {
                    if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
                }
                else
                    current += ch;
            }
            if (!string.IsNullOrEmpty(current)) parts.Add(current);
            return parts.ToArray();
        }
    }
}
