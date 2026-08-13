using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Runtime
{
    /// <summary>
    /// Structured Streamer.bot logger with a stable multi-segment prefix for extension actions.
    /// </summary>
    public sealed class ExtensionLogger
    {
        private readonly IInlineInvokeProxy _cph;
        private readonly string _title;
        private readonly string _extensionVersion;
        private readonly string _frameworkVersion;
        private readonly IEnumerable<string> _extraSensitiveKeys;

        public ExtensionLogger(
            IInlineInvokeProxy cph,
            string title,
            string extensionVersion,
            string frameworkVersion = null,
            IEnumerable<string> extraSensitiveKeys = null)
        {
            _cph = cph;
            _title = string.IsNullOrWhiteSpace(title) ? "extension" : title.Trim();
            _extensionVersion = string.IsNullOrWhiteSpace(extensionVersion) ? "0" : extensionVersion.Trim();
            _frameworkVersion = string.IsNullOrWhiteSpace(frameworkVersion)
                ? FluentConfigSession.FrameworkVersion
                : frameworkVersion.Trim();
            _extraSensitiveKeys = extraSensitiveKeys;
        }

        public void Info(string message, [CallerMemberName] string member = "-")
            => Write("info", message, member);

        public void Warn(string message, [CallerMemberName] string member = "-")
            => Write("warn", message, member);

        public void Error(string message, [CallerMemberName] string member = "-")
            => Write("error", message, member);

        /// <summary>
        /// One searchable banner line with action/user/trigger context from current args.
        /// </summary>
        public void Init([CallerMemberName] string member = "-")
        {
            var ev = EventContext.Capture(_cph);
            var parts = new List<string>(8);
            if (!string.IsNullOrEmpty(ev.ActionName))
                parts.Add("action=" + ev.ActionName);
            if (!string.IsNullOrEmpty(ev.TriggerName))
                parts.Add("trigger=" + ev.TriggerName);
            if (!string.IsNullOrEmpty(ev.User))
                parts.Add("user=" + ev.User);
            if (!string.IsNullOrEmpty(ev.UserName))
                parts.Add("userName=" + ev.UserName);
            if (!string.IsNullOrEmpty(ev.Command))
                parts.Add("command=" + ev.Command);
            if (!string.IsNullOrEmpty(ev.RawInput))
                parts.Add("rawInput=" + ev.RawInput);
            if (ev.IsTest)
                parts.Add("isTest=true");

            var detail = parts.Count == 0 ? "(no event args)" : string.Join(" ", parts);
            Info("INITIALIZING " + detail, member);
        }

        /// <summary>
        /// Log <c>{operation} failed: {ex.Message}</c>. Includes the exception string when a
        /// <c>debug</c> arg is true.
        /// </summary>
        public void Failed(string operation, Exception ex, [CallerMemberName] string member = "-")
        {
            var op = string.IsNullOrWhiteSpace(operation) ? "operation" : operation.Trim();
            var msg = ex == null ? $"{op} failed" : $"{op} failed: {ex.Message}";
            if (IsDebug() && ex != null)
                msg += " | " + ex;
            Error(msg, member);
        }

        /// <summary>Log a redacted clone of settings JSON (never dumps secrets in cleartext).</summary>
        public void LogSettings(JObject settings, [CallerMemberName] string member = "-")
        {
            var redacted = SettingsRedaction.RedactObject(settings, _extraSensitiveKeys);
            Info("settings: " + redacted, member);
        }

        private void Write(string level, string message, string member)
        {
            var action = EventContext.GetString(_cph, "actionName");
            if (string.IsNullOrEmpty(action))
                action = EventContext.GetString(_cph, "action");
            if (string.IsNullOrEmpty(action))
                action = "-";

            var method = string.IsNullOrWhiteSpace(member) ? "-" : member;
            var line =
                $"[FluentConfig {_frameworkVersion}][{_title} v.{_extensionVersion}] [{action}] [{method}] {message ?? ""}";

            if (_cph == null)
            {
                FluentConfigApp.LogInternal(line);
                return;
            }

            try
            {
                if (string.Equals(level, "warn", StringComparison.OrdinalIgnoreCase))
                    _cph.LogWarn(line);
                else if (string.Equals(level, "error", StringComparison.OrdinalIgnoreCase))
                    _cph.LogError(line);
                else
                    _cph.LogInfo(line);
            }
            catch
            {
                try { _cph.LogInfo(line); }
                catch { FluentConfigApp.LogInternal(line); }
            }
        }

        private bool IsDebug() => EventContext.GetBool(_cph, "debug");
    }
}
