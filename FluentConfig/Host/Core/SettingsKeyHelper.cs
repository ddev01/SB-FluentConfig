using System;
using System.Text;

namespace FluentConfig.Core
{
    /// <summary>
    /// Menu persistence keys. Identity is an explicit id <c>[a-z][a-z0-9_]*</c>;
    /// <see cref="Slugify"/> remains a helper for authors picking an id from a title.
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
        /// Throws unless <paramref name="id"/> matches <c>[a-z][a-z0-9_]*</c>.
        /// </summary>
        public static string ValidateMenuId(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (!IsValidMenuId(id))
            {
                throw new ArgumentException(
                    "Menu id must match [a-z][a-z0-9_]* (start with a lowercase letter; then letters, digits, or underscores).",
                    nameof(id));
            }

            return id;
        }

        public static bool IsValidMenuId(string id)
        {
            if (string.IsNullOrEmpty(id) || id[0] < 'a' || id[0] > 'z')
                return false;
            for (int i = 1; i < id.Length; i++)
            {
                var c = id[i];
                if (c >= 'a' && c <= 'z') continue;
                if (c >= '0' && c <= '9') continue;
                if (c == '_') continue;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Builds <c>{id}_{suffix}</c>. Empty suffix returns the id alone.
        /// </summary>
        public static string KeyFor(string id, string suffix)
        {
            id = ValidateMenuId(id);
            var suf = suffix ?? "";
            if (string.IsNullOrEmpty(suf))
                return id;
            return id + "_" + suf;
        }

        /// <summary>Settings-blob key for a menu id (e.g. <c>alerts</c> → <c>alerts_settings</c>).</summary>
        public static string SettingsKeyFor(string id) => KeyFor(id, "settings");
    }
}
