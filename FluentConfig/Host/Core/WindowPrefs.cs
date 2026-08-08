using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>Per-title UI preferences nested under general settings <c>windows.{title}</c>.</summary>
    internal sealed class WindowPrefsData
    {
        public bool DontRemindDiscard { get; set; }
    }

    /// <summary>
    /// Loads/saves per-title UI preferences via <see cref="GeneralSettingsStore"/>.
    /// </summary>
    internal static class WindowPrefsStore
    {
        internal static WindowPrefsData Load(IInlineInvokeProxy cph, string title)
        {
            if (cph == null || string.IsNullOrEmpty(title))
                return null;

            try
            {
                var obj = GeneralSettingsStore.LoadWindowEntry(cph, title);
                if (obj == null)
                    return null;

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

            GeneralSettingsStore.UpdateWindowEntry(cph, title, entry =>
            {
                entry["dontRemindDiscard"] = dontRemind;
            });
        }
    }
}
