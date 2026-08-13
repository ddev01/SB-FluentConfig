using System;
using System.Collections.Generic;
using System.Text;

namespace FluentConfig.Runtime
{
    /// <summary>
    /// Fill <c>%user%</c> / <c>{user}</c>-style placeholders in chat/response templates.
    /// </summary>
    public static class MessageTemplates
    {
        private static readonly HashSet<string> MentionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "user",
            "username",
            "userName",
            "displayname",
            "displayName",
        };

        /// <summary>
        /// Replace tokens from <paramref name="vars"/>. Supports both <c>%key%</c> and <c>{key}</c>.
        /// Unknown tokens are left unchanged. User-name values have a leading <c>@</c> stripped.
        /// </summary>
        public static string Apply(string template, IReadOnlyDictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(template) || vars == null || vars.Count == 0)
                return template ?? "";

            var result = template;
            foreach (var pair in vars)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;

                var value = pair.Value ?? "";
                if (MentionKeys.Contains(pair.Key))
                    value = SanitizeMention(value);

                result = ReplaceToken(result, pair.Key, value);
            }
            return result;
        }

        /// <summary>Apply common fields from an <see cref="EventContext"/>.</summary>
        public static string Apply(string template, EventContext ev)
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = ev.User,
                ["userName"] = ev.UserName,
                ["username"] = ev.UserName,
                ["userId"] = ev.UserId,
                ["userType"] = ev.UserType,
                ["rawInput"] = ev.RawInput,
                ["message"] = ev.Message,
                ["actionName"] = ev.ActionName,
                ["triggerName"] = ev.TriggerName,
                ["command"] = ev.Command,
                ["msgId"] = ev.MessageId,
                ["messageId"] = ev.MessageId,
            };
            return Apply(template, vars);
        }

        /// <summary>Strip a leading <c>@</c> and trim whitespace from a mention/login.</summary>
        public static string SanitizeMention(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return "";
            var s = userName.Trim();
            if (s.Length > 0 && s[0] == '@')
                s = s.Substring(1).TrimStart();
            return s;
        }

        private static string ReplaceToken(string template, string key, string value)
        {
            // Case-sensitive first for exact key, then rebuild with ordinal ignore for %/% and {}/
            var percent = "%" + key + "%";
            var brace = "{" + key + "}";
            if (template.IndexOf(percent, StringComparison.OrdinalIgnoreCase) < 0
                && template.IndexOf(brace, StringComparison.OrdinalIgnoreCase) < 0)
                return template;

            var sb = new StringBuilder(template.Length + value.Length);
            int i = 0;
            while (i < template.Length)
            {
                if (TryMatch(template, i, percent, out int len) || TryMatch(template, i, brace, out len))
                {
                    sb.Append(value);
                    i += len;
                    continue;
                }
                sb.Append(template[i]);
                i++;
            }
            return sb.ToString();
        }

        private static bool TryMatch(string template, int index, string token, out int length)
        {
            length = 0;
            if (index + token.Length > template.Length)
                return false;
            if (string.Compare(template, index, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            length = token.Length;
            return true;
        }
    }
}
