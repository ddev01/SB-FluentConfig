using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Core
{
    /// <summary>
    /// Receives one JSON values blob from the web UI, merges into persisted settings, and saves.
    /// Replaces the old ControlRegistry / SettingsSynchronizer WPF tree-walking approach.
    /// </summary>
    public static class SettingsSync
    {
        /// <summary>
        /// Deep-merges <paramref name="valuesBlob"/> into the manager's in-memory settings and persists.
        /// </summary>
        public static JObject ApplyAndSave(SettingsManager manager, JObject valuesBlob)
        {
            if (manager == null) return new JObject();

            var current = manager.GetSettings() ?? new JObject();
            if (valuesBlob != null)
                SettingsPathHelper.MergeValues(current, valuesBlob);

            manager.ReplaceSettings(current);
            manager.Save(current);
            return current;
        }

        /// <summary>
        /// Writes default values only where the corresponding path is missing (null / absent).
        /// Never overwrites user-changed (or previously saved) values. Optionally persists.
        /// </summary>
        /// <returns>Number of paths newly written.</returns>
        public static int SeedMissingDefaults(
            SettingsManager manager,
            IDictionary<string, JToken> defaults,
            bool persist = true)
        {
            if (manager == null || defaults == null || defaults.Count == 0)
                return 0;

            var current = manager.GetSettings() ?? new JObject();
            int added = 0;
            foreach (var pair in defaults)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null || pair.Value.Type == JTokenType.Null)
                    continue;
                if (!IsMissing(current, pair.Key))
                    continue;
                SettingsPathHelper.SetNestedValue(current, pair.Key, pair.Value.DeepClone());
                added++;
            }

            if (added > 0)
            {
                manager.ReplaceSettings(current);
                if (persist)
                    manager.Save(current);
            }

            return added;
        }

        /// <summary>True when the path is absent or explicitly null.</summary>
        public static bool IsMissing(JObject root, string path)
        {
            var token = SettingsPathHelper.GetNestedValue(root, path);
            return token == null || token.Type == JTokenType.Null;
        }
    }
}
