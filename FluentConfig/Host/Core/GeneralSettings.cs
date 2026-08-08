using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>
    /// Shared FluentConfig menu/DLL preferences in CPH global <c>FluentConfig_General_Settings</c>.
    /// Per-title window geometry and UI prefs live under <c>windows.{title}</c>.
    /// </summary>
    internal static class GeneralSettingsStore
    {
        internal const string GlobalKey = "FluentConfig_General_Settings";
        internal const string WindowsKey = "windows";
        internal const string DllCheckLastUtcKey = "dllCheckLastUtc";

        internal static string LegacyWindowKey(string title) => "FluentConfig_Window_" + (title ?? "Settings");
        internal static string LegacyPrefsKey(string title) => "FluentConfig_Prefs_" + (title ?? "Settings");
        internal const string LegacyDllCheckLastUtcKey = "FluentConfig_DllCheck_LastUtc";

        internal static JObject LoadRoot(IInlineInvokeProxy cph)
        {
            if (cph == null)
                return new JObject();

            try
            {
                string json = cph.GetGlobalVar<string>(GlobalKey, true);
                if (string.IsNullOrWhiteSpace(json))
                    return new JObject();

                return ParseObject(json);
            }
            catch
            {
                return new JObject();
            }
        }

        internal static void SaveRoot(IInlineInvokeProxy cph, JObject root)
        {
            if (cph == null || root == null)
                return;

            try
            {
                cph.SetGlobalVar(GlobalKey, root.ToString(), true);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }

        internal static JObject GetOrCreateWindowEntry(JObject root, string title)
        {
            if (root == null)
                root = new JObject();

            var windows = root[WindowsKey] as JObject ?? new JObject();
            if (root[WindowsKey] == null)
                root[WindowsKey] = windows;

            var key = title ?? "Settings";
            var entry = windows[key] as JObject ?? new JObject();
            if (windows[key] == null)
                windows[key] = entry;

            return entry;
        }

        /// <summary>
        /// Loads the per-title window entry from general settings, or merges legacy per-key globals.
        /// </summary>
        internal static JObject LoadWindowEntry(IInlineInvokeProxy cph, string title)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return null;

            try
            {
                var root = LoadRoot(cph);
                var windows = root[WindowsKey] as JObject;
                if (windows != null && windows[title] is JObject entry && entry.HasValues)
                    return (JObject)entry.DeepClone();

                var merged = new JObject();
                try
                {
                    string legacyGeom = cph.GetGlobalVar<string>(LegacyWindowKey(title), true);
                    if (!string.IsNullOrWhiteSpace(legacyGeom))
                    {
                        var geom = ParseObject(legacyGeom);
                        foreach (var prop in geom.Properties())
                            merged[prop.Name] = prop.Value;
                    }
                }
                catch
                {
                    // Ignore legacy read failures.
                }

                try
                {
                    string legacyPrefs = cph.GetGlobalVar<string>(LegacyPrefsKey(title), true);
                    if (!string.IsNullOrWhiteSpace(legacyPrefs))
                    {
                        var prefs = ParseObject(legacyPrefs);
                        if (prefs["dontRemindDiscard"] != null)
                            merged["dontRemindDiscard"] = prefs["dontRemindDiscard"];
                    }
                }
                catch
                {
                    // Ignore legacy read failures.
                }

                return merged.HasValues ? merged : null;
            }
            catch
            {
                return null;
            }
        }

        internal static void UpdateWindowEntry(IInlineInvokeProxy cph, string title, Action<JObject> update)
        {
            if (cph == null || string.IsNullOrEmpty(title) || update == null)
                return;

            try
            {
                var root = LoadRoot(cph);
                var entry = GetOrCreateWindowEntry(root, title);
                update(entry);
                SaveRoot(cph, root);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }

        internal static DateTime? LoadDllCheckLastUtc(IInlineInvokeProxy cph)
        {
            if (cph == null)
                return null;

            try
            {
                var root = LoadRoot(cph);
                var fromGeneral = ParseUtc(root.Value<string>(DllCheckLastUtcKey));
                if (fromGeneral.HasValue)
                    return fromGeneral;

                string legacy = cph.GetGlobalVar<string>(LegacyDllCheckLastUtcKey, true);
                return ParseUtc(legacy);
            }
            catch
            {
                return null;
            }
        }

        internal static void SetDllCheckLastUtc(IInlineInvokeProxy cph, DateTime utc)
        {
            if (cph == null)
                return;

            try
            {
                var root = LoadRoot(cph);
                root[DllCheckLastUtcKey] = utc.ToUniversalTime()
                    .ToString("o", CultureInfo.InvariantCulture);
                SaveRoot(cph, root);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }

        internal static bool IsWithinDailyThrottle(DateTime? lastCheckUtc, DateTime? utcNow = null)
        {
            if (!lastCheckUtc.HasValue)
                return false;

            var now = utcNow ?? DateTime.UtcNow;
            return now - lastCheckUtc.Value.ToUniversalTime() < TimeSpan.FromHours(24);
        }

        private static JObject ParseObject(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json))
            {
                DateParseHandling = DateParseHandling.None,
            })
            {
                return JObject.Load(reader);
            }
        }

        private static DateTime? ParseUtc(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (!DateTime.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
                return null;

            if (parsed.Kind == DateTimeKind.Unspecified)
                parsed = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

            return parsed.ToUniversalTime();
        }
    }
}
