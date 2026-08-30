using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Moq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Shared CPH mock for host tests: persisted string globals, optional action args, and log sinks.
    /// </summary>
    internal static class TestCph
    {
        internal static Mock<IInlineInvokeProxy> CreateMockCph(ConcurrentDictionary<string, string> store = null)
            => CreateMockCph(store, null);

        /// <summary>
        /// Mock CPH with persisted string globals, optional action args, and log sinks.
        /// </summary>
        internal static Mock<IInlineInvokeProxy> CreateMockCph(
            ConcurrentDictionary<string, string> store,
            ConcurrentDictionary<string, object> args,
            List<string> logInfo = null,
            List<string> logWarn = null,
            List<string> logError = null)
        {
            store = store ?? new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            args = args ?? new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
            var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Loose);
            mock.Setup(c => c.GetGlobalVar<string>(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string name, bool persisted) =>
                {
                    store.TryGetValue(name, out var v);
                    return v;
                });
            mock.Setup(c => c.SetGlobalVar(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Callback((string name, object value, bool persisted) =>
                {
                    store[name] = value?.ToString() ?? "";
                });

            mock.Setup(c => c.TryGetArg(It.IsAny<string>(), out It.Ref<object>.IsAny))
                .Returns(new TryGetArgObjectCallback((string name, out object value) =>
                {
                    if (args.TryGetValue(name, out var v))
                    {
                        value = v;
                        return true;
                    }
                    value = null;
                    return false;
                }));

            mock.Setup(c => c.TryGetArg(It.IsAny<string>(), out It.Ref<string>.IsAny))
                .Returns(new TryGetArgStringCallback((string name, out string value) =>
                {
                    if (args.TryGetValue(name, out var v) && v != null)
                    {
                        value = Convert.ToString(v);
                        return true;
                    }
                    value = null;
                    return false;
                }));

            mock.Setup(c => c.TryGetArg(It.IsAny<string>(), out It.Ref<bool>.IsAny))
                .Returns(new TryGetArgBoolCallback((string name, out bool value) =>
                {
                    if (args.TryGetValue(name, out var v) && v != null)
                    {
                        if (v is bool b)
                        {
                            value = b;
                            return true;
                        }
                        if (bool.TryParse(Convert.ToString(v), out var parsed))
                        {
                            value = parsed;
                            return true;
                        }
                    }
                    value = false;
                    return false;
                }));

            if (logInfo != null)
                mock.Setup(c => c.LogInfo(It.IsAny<string>())).Callback<string>(logInfo.Add);
            if (logWarn != null)
                mock.Setup(c => c.LogWarn(It.IsAny<string>())).Callback<string>(logWarn.Add);
            if (logError != null)
                mock.Setup(c => c.LogError(It.IsAny<string>())).Callback<string>(logError.Add);

            return mock;
        }

        private delegate bool TryGetArgObjectCallback(string name, out object value);
        private delegate bool TryGetArgStringCallback(string name, out string value);
        private delegate bool TryGetArgBoolCallback(string name, out bool value);
    }
}
