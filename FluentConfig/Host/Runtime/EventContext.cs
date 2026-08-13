using System;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Runtime
{
    /// <summary>
    /// Snapshot of common Streamer.bot action arguments for chat/event handlers.
    /// </summary>
    public readonly struct EventContext
    {
        public string User { get; }
        public string UserName { get; }
        public string UserId { get; }
        public string UserType { get; }
        public string RawInput { get; }
        public string Message { get; }
        public string ActionName { get; }
        public string TriggerName { get; }
        public string Command { get; }
        public string MessageId { get; }
        public bool IsModerator { get; }
        public bool IsVip { get; }
        public bool IsSubscribed { get; }
        public bool IsTest { get; }

        public EventContext(
            string user,
            string userName,
            string userId,
            string userType,
            string rawInput,
            string message,
            string actionName,
            string triggerName,
            string command,
            string messageId,
            bool isModerator,
            bool isVip,
            bool isSubscribed,
            bool isTest)
        {
            User = user ?? "";
            UserName = userName ?? "";
            UserId = userId ?? "";
            UserType = userType ?? "";
            RawInput = rawInput ?? "";
            Message = message ?? "";
            ActionName = actionName ?? "";
            TriggerName = triggerName ?? "";
            Command = command ?? "";
            MessageId = messageId ?? "";
            IsModerator = isModerator;
            IsVip = isVip;
            IsSubscribed = isSubscribed;
            IsTest = isTest;
        }

        /// <summary>Read common event args from <paramref name="cph"/> (missing → empty/false).</summary>
        public static EventContext Capture(IInlineInvokeProxy cph)
        {
            if (cph == null)
                return default;

            string user = GetString(cph, "user");
            string userName = GetString(cph, "userName");
            string userId = GetString(cph, "userId");
            string userType = GetString(cph, "userType");
            string rawInput = GetString(cph, "rawInput");
            string message = GetString(cph, "message");
            if (string.IsNullOrEmpty(message))
                message = GetString(cph, "msg");

            string actionName = GetString(cph, "actionName");
            if (string.IsNullOrEmpty(actionName))
                actionName = GetString(cph, "action");

            string triggerName = GetString(cph, "triggerName");
            if (string.IsNullOrEmpty(triggerName))
                triggerName = GetString(cph, "trigger");

            string command = GetString(cph, "command");

            string messageId = GetString(cph, "msgId");
            if (string.IsNullOrEmpty(messageId))
                messageId = GetString(cph, "messageId");

            bool isModerator = GetBool(cph, "isModerator");
            bool isVip = GetBool(cph, "isVip");
            bool isSubscribed = GetBool(cph, "isSubscribed") || GetBool(cph, "isSubscriber");
            bool isTest = GetBool(cph, "isTest");

            return new EventContext(
                user, userName, userId, userType, rawInput, message,
                actionName, triggerName, command, messageId,
                isModerator, isVip, isSubscribed, isTest);
        }

        internal static string GetString(IInlineInvokeProxy cph, string name)
        {
            try
            {
                if (cph.TryGetArg(name, out string s) && s != null)
                    return s;
            }
            catch
            {
                // ignore
            }

            try
            {
                if (cph.TryGetArg(name, out object o) && o != null)
                    return Convert.ToString(o) ?? "";
            }
            catch
            {
                // ignore
            }
            return "";
        }

        internal static bool GetBool(IInlineInvokeProxy cph, string name)
        {
            try
            {
                if (cph.TryGetArg(name, out bool b))
                    return b;
            }
            catch
            {
                // ignore
            }

            try
            {
                if (cph.TryGetArg(name, out object o) && o != null)
                {
                    if (o is bool bb)
                        return bb;
                    if (bool.TryParse(Convert.ToString(o), out bool parsed))
                        return parsed;
                }
            }
            catch
            {
                // ignore
            }
            return false;
        }
    }
}
