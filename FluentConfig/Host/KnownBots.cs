using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>
    /// Curated Twitch bot / automation account logins for chat filters.
    /// Pure in-memory lookups — no CPH, no UI, safe on hot chat paths.
    /// </summary>
    public static class KnownBots
    {
        // OrdinalIgnoreCase: Twitch logins are case-insensitive.
        private static readonly HashSet<string> Set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nightbot",
            "streamelements",
            "streamlabs",
            "sery_bot",
            "moobot",
            "fossabot",
            "wizebot",
            "scorpbot",
            "mixitupbot",
            "phantombot",
            "coebot",
            "stay_hydrated_bot",
            "pretzelrocks",
            "firebot",
            "botrixlive",
            "pokemoncommunitygame",
            "anotherttvviewer",
            "deepbot",
            "streampuppet",
            "vivbot",
            "lurxbots",
            "snazbot",
            "mtgbot",
            "moobotalpha",
            "streambot",
            "soundalerts",
            "kofistreambot",
            "tangiabot",
            "botrixoficial",
            "frostytoolsdotcom",
            "rocketrankbot",
            "wzbot",
            "commanderroot",
        };

        private static readonly string[] Snapshot = CreateSnapshot();

        private static string[] CreateSnapshot()
        {
            var arr = new string[Set.Count];
            Set.CopyTo(arr);
            Array.Sort(arr, StringComparer.OrdinalIgnoreCase);
            return arr;
        }

        /// <summary>
        /// Returns true when <paramref name="user"/> matches a known bot login
        /// (case-insensitive). Null/empty/whitespace → false. Leading <c>@</c> is stripped.
        /// </summary>
        public static bool IsKnownBot(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                return false;

            var login = user.Trim();
            if (login.Length > 0 && login[0] == '@')
                login = login.Substring(1).Trim();

            return login.Length > 0 && Set.Contains(login);
        }

        /// <summary>
        /// Sorted copy of the curated known-bot logins (lowercase as stored).
        /// Safe to mutate — does not affect the internal set.
        /// </summary>
        public static IReadOnlyList<string> GetKnownBots() => (string[])Snapshot.Clone();
    }
}
