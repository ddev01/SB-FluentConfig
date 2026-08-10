using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>
    /// Shared FluentConfig menu/DLL preferences in CPH global <c>fluentconfig_settings</c>.
    /// Per-title window geometry and UI prefs live under <c>windows.{title}</c>.
    /// </summary>
    internal static class GeneralSettingsStore
    {
        internal const string GlobalKey = "fluentconfig_settings";
        internal const string WindowsKey = "windows";

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
        /// Loads the per-title window entry from general settings (<c>windows.{title}</c>).
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

                return null;
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
    }
}
