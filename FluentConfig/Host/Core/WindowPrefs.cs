using System;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>Per-title UI preferences (CPH global FluentConfig_Prefs_{title}).</summary>
    internal sealed class WindowPrefsData
    {
        public bool DontRemindDiscard { get; set; }
    }

    /// <summary>
    /// Loads/saves per-title UI preferences via CPH global <c>FluentConfig_Prefs_{title}</c>
    /// (same storage pattern as <see cref="WindowGeometryStore"/>).
    /// </summary>
    internal static class WindowPrefsStore
    {
        internal static string KeyForTitle(string title) => "FluentConfig_Prefs_" + (title ?? "Settings");

        internal static WindowPrefsData Load(IInlineInvokeProxy cph, string title)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return null;

            try
            {
                string json = cph.GetGlobalVar<string>(KeyForTitle(title), true);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var obj = JObject.Parse(json);
                return new WindowPrefsData
                {
                    DontRemindDiscard = obj.Value<bool?>("dontRemindDiscard") == true,
                };
            }
            catch
            {
                return null;
            }
        }

        internal static void SetDontRemindDiscard(IInlineInvokeProxy cph, string title, bool dontRemind)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return;

            try
            {
                JObject obj;
                try
                {
                    string json = cph.GetGlobalVar<string>(KeyForTitle(title), true);
                    obj = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                }
                catch
                {
                    obj = new JObject();
                }

                obj["dontRemindDiscard"] = dontRemind;
                cph.SetGlobalVar(KeyForTitle(title), obj.ToString(), true);
            }
            catch
            {
                // Persistence is best-effort.
            }
        }
    }
}
