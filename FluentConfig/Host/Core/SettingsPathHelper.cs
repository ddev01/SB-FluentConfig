using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Core
{
    /// <summary>
    /// Helper for reading/writing nested settings paths (e.g. "voice_aliases[0].name", "settings.timeout").
    /// </summary>
    public static class SettingsPathHelper
    {
        /// <summary>
        /// Gets a value at the given path, or null if missing.
        /// </summary>
        public static JToken GetNestedValue(JObject root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;
            if (!path.Contains("[") && !path.Contains("."))
                return root[path];

            var parts = ParseNestedPath(path);
            if (parts.Length == 0) return null;

            JToken current = root;
            foreach (var part in parts)
            {
                if (current == null) return null;
                if (int.TryParse(part, out int index))
                {
                    if (!(current is JArray arr) || index < 0 || index >= arr.Count)
                        return null;
                    current = arr[index];
                }
                else if (current is JObject obj)
                {
                    current = obj[part];
                }
                else
                {
                    return null;
                }
            }
            return current;
        }

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
        /// Removes a value at the given path when possible.
        /// </summary>
        public static void RemoveNestedValue(JObject root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return;
            if (!path.Contains("[") && !path.Contains("."))
            {
                root.Remove(path);
                return;
            }

            var parts = ParseNestedPath(path);
            if (parts.Length == 0) return;

            JToken current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (int.TryParse(part, out int index))
                {
                    if (!(current is JArray arr) || index < 0 || index >= arr.Count)
                        return;
                    current = arr[index];
                }
                else if (current is JObject obj)
                {
                    current = obj[part];
                    if (current == null) return;
                }
                else return;
            }

            var last = parts[parts.Length - 1];
            if (int.TryParse(last, out int lastIdx))
            {
                if (current is JArray arr && lastIdx >= 0 && lastIdx < arr.Count)
                    arr[lastIdx] = null;
            }
            else if (current is JObject obj)
            {
                obj.Remove(last);
            }
        }

        /// <summary>
        /// Deep-merges <paramref name="incoming"/> into <paramref name="target"/> (objects recurse; arrays/scalars replace).
        /// </summary>
        public static void MergeValues(JObject target, JObject incoming)
        {
            if (target == null || incoming == null) return;
            foreach (var prop in incoming.Properties())
            {
                if (prop.Value is JObject incomingObj && target[prop.Name] is JObject targetObj)
                    MergeValues(targetObj, incomingObj);
                else
                    target[prop.Name] = prop.Value?.DeepClone();
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
