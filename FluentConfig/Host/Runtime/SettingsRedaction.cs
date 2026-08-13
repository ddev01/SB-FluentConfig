using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Runtime
{
    /// <summary>
    /// Masks sensitive fields in settings/data JSON before logging.
    /// </summary>
    public static class SettingsRedaction
    {
        private static readonly string[] DefaultSensitiveSubstrings =
        {
            "password",
            "secret",
            "token",
            "jwt",
            "api_key",
            "apikey",
            "client_secret",
            "refresh",
        };

        /// <summary>
        /// Deep-clones <paramref name="source"/> and replaces sensitive string/number values with <c>***</c>.
        /// Matching is case-insensitive substring against property names (plus optional extras).
        /// </summary>
        public static JToken Redact(JToken source, IEnumerable<string> extraSensitiveKeys = null)
        {
            if (source == null)
                return null;

            var clone = source.DeepClone();
            var matchers = BuildMatchers(extraSensitiveKeys);
            RedactToken(clone, matchers);
            return clone;
        }

        /// <summary>Convenience: redact a settings <see cref="JObject"/> and return a <see cref="JObject"/>.</summary>
        public static JObject RedactObject(JObject source, IEnumerable<string> extraSensitiveKeys = null)
        {
            var result = Redact(source, extraSensitiveKeys);
            return result as JObject ?? new JObject();
        }

        internal static bool IsSensitiveKey(string key, IList<string> matchers)
        {
            if (string.IsNullOrEmpty(key) || matchers == null || matchers.Count == 0)
                return false;

            var lower = key.ToLowerInvariant();
            for (int i = 0; i < matchers.Count; i++)
            {
                if (lower.Contains(matchers[i]))
                    return true;
            }
            return false;
        }

        private static List<string> BuildMatchers(IEnumerable<string> extraSensitiveKeys)
        {
            var list = new List<string>(DefaultSensitiveSubstrings.Length + 4);
            for (int i = 0; i < DefaultSensitiveSubstrings.Length; i++)
                list.Add(DefaultSensitiveSubstrings[i]);

            if (extraSensitiveKeys != null)
            {
                foreach (var extra in extraSensitiveKeys)
                {
                    if (string.IsNullOrWhiteSpace(extra))
                        continue;
                    var lower = extra.Trim().ToLowerInvariant();
                    if (!list.Contains(lower))
                        list.Add(lower);
                }
            }
            return list;
        }

        private static void RedactToken(JToken token, IList<string> matchers)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (IsSensitiveKey(prop.Name, matchers) && IsScalar(prop.Value))
                    {
                        prop.Value = "***";
                        continue;
                    }
                    RedactToken(prop.Value, matchers);
                }
            }
            else if (token is JArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                    RedactToken(arr[i], matchers);
            }
        }

        private static bool IsScalar(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return false;
            return value.Type != JTokenType.Object && value.Type != JTokenType.Array;
        }
    }
}
