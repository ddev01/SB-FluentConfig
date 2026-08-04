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
    }
}
