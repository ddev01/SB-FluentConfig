using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>
    /// Resolves button.click ids. Item-template buttons are registered as
    /// <c>btn_label__{name}</c> and arrive expanded (e.g. <c>btn_label__Rickroll</c>).
    /// </summary>
    internal static class ButtonClickRouting
    {
        internal const string NameToken = "{name}";

        /// <summary>
        /// Resolve an exact map key, or a <c>{name}</c> template (longest template wins).
        /// Used for item-template buttons and dropdown.refresh saveKeys.
        /// </summary>
        public static bool TryResolve<T>(
            IDictionary<string, T> map,
            string actual,
            out T value,
            out string itemName)
        {
            value = default;
            itemName = null;
            if (map == null || string.IsNullOrEmpty(actual))
                return false;

            if (map.TryGetValue(actual, out value))
                return true;

            T best = default;
            string bestName = null;
            var bestLen = -1;
            var found = false;
            foreach (var kv in map)
            {
                var tmpl = kv.Key;
                if (string.IsNullOrEmpty(tmpl))
                    continue;
                var idx = tmpl.IndexOf(NameToken, StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var prefix = tmpl.Substring(0, idx);
                var suffix = tmpl.Substring(idx + NameToken.Length);
                if (actual.Length <= prefix.Length + suffix.Length)
                    continue;
                if (!actual.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (!actual.EndsWith(suffix, StringComparison.Ordinal))
                    continue;
                if (tmpl.Length < bestLen)
                    continue;
                bestLen = tmpl.Length;
                best = kv.Value;
                bestName = actual.Substring(prefix.Length, actual.Length - prefix.Length - suffix.Length);
                found = true;
            }

            if (!found)
                return false;
            value = best;
            itemName = bestName;
            return true;
        }
    }
}
