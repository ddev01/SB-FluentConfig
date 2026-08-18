using System.Text;

namespace FluentConfig.Core
{
    /// <summary>
    /// Derives snake_case Streamer.bot global-variable keys from a human-readable menu title.
    /// </summary>
    public static class SettingsKeyHelper
    {
        /// <summary>
        /// Lowercase the title, collapse any run of non-[a-z0-9] characters into a single
        /// underscore, and trim leading/trailing underscores.
        /// </summary>
        public static string Slugify(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "";
            var sb = new StringBuilder(title.Length);
            var lastWasUnderscore = false;
            foreach (var ch in title.ToLowerInvariant())
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    sb.Append(ch);
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                    lastWasUnderscore = true;
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '_')
                sb.Length--;
            return sb.ToString();
        }

        /// <summary>
        /// Builds <c>{slug}_{suffix}</c>. Empty slug or suffix returns the other side alone.
        /// </summary>
        public static string KeyFor(string title, string suffix)
        {
            var slug = Slugify(title);
            var suf = suffix ?? "";
            if (string.IsNullOrEmpty(slug))
                return suf;
            if (string.IsNullOrEmpty(suf))
                return slug;
            return slug + "_" + suf;
        }

        /// <summary>
        /// Settings-blob key for a menu title (e.g. "First Chatters" → "first_chatters_settings").
        /// </summary>
        public static string SettingsKeyFor(string title) => KeyFor(title, "settings");
        /// <summary>
        /// Pre-slug global used by older FluentConfig builds: <c>FluentConfig_Settings_{title}</c>.
        /// </summary>
        public static string LegacySettingsKeyFor(string title) => "FluentConfig_Settings_" + (title ?? "");
    }
}