using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>Per-title extension update preferences (CPH global FluentConfig_UpdatePrefs_{title}).</summary>
    internal sealed class UpdatePrefsData
    {
        public DateTime? LastCheckUtc { get; set; }
        public string IgnoredVersion { get; set; }
    }

    /// <summary>
    /// Loads/saves extension update throttle + ignored-version prefs via
    /// CPH global <c>FluentConfig_UpdatePrefs_{title}</c>.
    /// </summary>
    internal static class UpdatePrefsStore
    {
        internal static string KeyForTitle(string title) => "FluentConfig_UpdatePrefs_" + (title ?? "Settings");

        internal static UpdatePrefsData Load(IInlineInvokeProxy cph, string title)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return null;

            try
            {
                string json = cph.GetGlobalVar<string>(KeyForTitle(title), true);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                // Keep ISO timestamps as strings — default DateParseHandling would turn
                // them into Date tokens and Value<string> would culture-format them.
                JObject obj;
                using (var reader = new JsonTextReader(new StringReader(json))
                {
                    DateParseHandling = DateParseHandling.None,
                })
                {
                    obj = JObject.Load(reader);
                }

                DateTime? lastCheck = null;
                var lastRaw = obj.Value<string>("lastCheckUtc");
                if (!string.IsNullOrWhiteSpace(lastRaw)
                    && DateTime.TryParse(
                        lastRaw,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var parsed))
                {
                    lastCheck = parsed.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                        : parsed.ToUniversalTime();
                }

                return new UpdatePrefsData
                {
                    LastCheckUtc = lastCheck,
                    IgnoredVersion = obj.Value<string>("ignoredVersion"),
                };
            }
            catch
            {
                return null;
            }
        }

        internal static void Save(IInlineInvokeProxy cph, string title, UpdatePrefsData data)
        {
            if (cph == null || string.IsNullOrEmpty(title) || data == null)
                return;

            try
            {
                var obj = new JObject();
                if (data.LastCheckUtc.HasValue)
                    obj["lastCheckUtc"] = data.LastCheckUtc.Value.ToUniversalTime()
                        .ToString("o", CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(data.IgnoredVersion))
                    obj["ignoredVersion"] = data.IgnoredVersion.Trim();

                cph.SetGlobalVar(KeyForTitle(title), obj.ToString(), true);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }

        /// <summary>True when a check ran within the last 24 hours.</summary>
        internal static bool IsWithinDailyThrottle(UpdatePrefsData prefs, DateTime? utcNow = null)
        {
            if (prefs?.LastCheckUtc == null)
                return false;
            var now = utcNow ?? DateTime.UtcNow;
            return now - prefs.LastCheckUtc.Value.ToUniversalTime() < TimeSpan.FromHours(24);
        }

        internal static bool IsVersionIgnored(UpdatePrefsData prefs, string latestVersion)
        {
            if (prefs == null || string.IsNullOrWhiteSpace(prefs.IgnoredVersion) || string.IsNullOrWhiteSpace(latestVersion))
                return false;
            return string.Equals(
                StripV(prefs.IgnoredVersion),
                StripV(latestVersion),
                StringComparison.OrdinalIgnoreCase);
        }

        internal static void MarkChecked(IInlineInvokeProxy cph, string title, DateTime? utcNow = null)
        {
            var prefs = Load(cph, title) ?? new UpdatePrefsData();
            prefs.LastCheckUtc = utcNow ?? DateTime.UtcNow;
            Save(cph, title, prefs);
        }

        internal static void IgnoreVersion(IInlineInvokeProxy cph, string title, string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return;
            var prefs = Load(cph, title) ?? new UpdatePrefsData();
            prefs.IgnoredVersion = StripV(version);
            Save(cph, title, prefs);
        }

        private static string StripV(string version)
        {
            if (string.IsNullOrEmpty(version)) return "";
            version = version.Trim();
            if (version.Length > 0 && (version[0] == 'v' || version[0] == 'V'))
                version = version.Substring(1);
            return version;
        }
    }
}
