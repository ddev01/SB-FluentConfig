using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentConfig;
using FluentConfig.Core;
using FluentConfig.Runtime;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class RuntimeHelpersTests
    {
        private class SampleData
        {
            public int Count { get; set; } = 0;
            public string LastUser { get; set; } = "";
        }

        [Fact]
        public void SetSetting_PreservesOtherKeys()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var key = SettingsKeyHelper.SettingsKeyFor("Runtime Demo");
            store[key] = new JObject
            {
                ["mode"] = "Normal",
                ["access_token"] = "secret-value",
            }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;
            Fc.SetSetting(cph, "Runtime Demo", "access_token", "");

            var persisted = JObject.Parse(store[key]);
            Assert.Equal("Normal", persisted["mode"]?.ToString());
            Assert.Equal("", persisted["access_token"]?.ToString());
        }

        [Fact]
        public void SaveSettings_MutatesMultipleKeys()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var key = SettingsKeyHelper.SettingsKeyFor("Runtime Demo");
            store[key] = new JObject
            {
                ["client_id"] = "abc",
                ["access_token"] = "tok",
                ["refresh_token"] = "ref",
            }.ToString();

            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;
            Fc.SaveSettings(cph, "Runtime Demo", o =>
            {
                o["access_token"] = "";
                o["refresh_token"] = "";
            });

            var persisted = JObject.Parse(store[key]);
            Assert.Equal("abc", persisted["client_id"]?.ToString());
            Assert.Equal("", persisted["access_token"]?.ToString());
            Assert.Equal("", persisted["refresh_token"]?.ToString());
        }

        [Fact]
        public void HasSavedSettings_EmptyVsPresent()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;

            Assert.False(Fc.HasSavedSettings(cph, "Runtime Demo"));

            store[Fc.SettingsKeyFor("Runtime Demo")] = "{}";
            Assert.True(Fc.HasSavedSettings(cph, "Runtime Demo"));

            store[Fc.SettingsKeyFor("Runtime Demo")] = "   ";
            Assert.False(Fc.HasSavedSettings(cph, "Runtime Demo"));
        }

        [Fact]
        public void Redact_MasksSensitiveFields_ButKeepsIds()
        {
            var source = new JObject
            {
                ["user_id"] = "123",
                ["access_token"] = "tok-plain",
                ["nested"] = new JObject
                {
                    ["client_secret"] = "shh",
                    ["display_name"] = "Alice",
                },
            };

            var redacted = SettingsRedaction.RedactObject(source);
            Assert.Equal("123", redacted["user_id"]?.ToString());
            Assert.Equal("***", redacted["access_token"]?.ToString());
            Assert.Equal("***", redacted["nested"]?["client_secret"]?.ToString());
            Assert.Equal("Alice", redacted["nested"]?["display_name"]?.ToString());
            // Original unchanged
            Assert.Equal("tok-plain", source["access_token"]?.ToString());
        }

        [Fact]
        public void ApplyTemplate_SupportsPercentAndBrace_AndStripsAt()
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = "@Alice",
                ["rawInput"] = "hello",
            };

            Assert.Equal("Hi Alice: hello", Fc.ApplyTemplate("Hi %user%: %rawInput%", vars));
            Assert.Equal("Hi Alice!", Fc.ApplyTemplate("Hi {user}!", vars));
            Assert.Equal("keep %unknown%", Fc.ApplyTemplate("keep %unknown%", vars));
        }

        [Fact]
        public void CaptureEvent_ReadsCommonArgs()
        {
            var args = new ConcurrentDictionary<string, object>(StringComparer.Ordinal)
            {
                ["user"] = "Alice",
                ["userName"] = "alice",
                ["userId"] = "42",
                ["rawInput"] = "!points",
                ["actionName"] = "Get Points",
                ["isTest"] = true,
                ["isModerator"] = true,
            };

            var cph = Phase2HostSmokeTests.CreateMockCph(null, args).Object;
            var ev = Fc.CaptureEvent(cph);

            Assert.Equal("Alice", ev.User);
            Assert.Equal("alice", ev.UserName);
            Assert.Equal("42", ev.UserId);
            Assert.Equal("!points", ev.RawInput);
            Assert.Equal("Get Points", ev.ActionName);
            Assert.True(ev.IsTest);
            Assert.True(ev.IsModerator);
        }

        [Fact]
        public void Logger_WritesPrefixedInfoAndFailed()
        {
            var info = new List<string>();
            var error = new List<string>();
            var args = new ConcurrentDictionary<string, object>(StringComparer.Ordinal)
            {
                ["actionName"] = "DemoAction",
            };
            var cph = Phase2HostSmokeTests.CreateMockCph(null, args, info, null, error).Object;

            var log = Fc.Logger(cph, "Runtime Demo", "1.2.3");
            log.Info("hello");
            log.Failed("refresh token", new InvalidOperationException("expired"));

            Assert.Single(info);
            Assert.Contains("[Runtime Demo v.1.2.3]", info[0]);
            Assert.Contains("[DemoAction]", info[0]);
            Assert.Contains("hello", info[0]);

            Assert.Single(error);
            Assert.Contains("refresh token failed: expired", error[0]);
        }

        [Fact]
        public void LoadData_SaveData_RoundTrip()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store).Object;

            Assert.Equal(Fc.KeyFor("Runtime Demo", "data"), Fc.DataKeyFor("Runtime Demo"));

            var state = Fc.LoadData<SampleData>(cph, "Runtime Demo");
            Assert.Equal(0, state.Count);

            state.Count = 5;
            state.LastUser = "bob";
            Fc.SaveData(cph, "Runtime Demo", state);

            var again = Fc.LoadData<SampleData>(cph, "Runtime Demo");
            Assert.Equal(5, again.Count);
            Assert.Equal("bob", again.LastUser);

            Fc.SetData(cph, "Runtime Demo", "count", 9);
            Assert.Equal(9, Fc.GetData(cph, "Runtime Demo", "count", 0));
        }

        [Fact]
        public void ApplyTemplate_FromEventContext()
        {
            var ev = new EventContext(
                user: "@Carol",
                userName: "carol",
                userId: "7",
                userType: "twitch",
                rawInput: "hi",
                message: "hi",
                actionName: "A",
                triggerName: "T",
                command: "!hi",
                messageId: "m1",
                isModerator: false,
                isVip: false,
                isSubscribed: false,
                isTest: false);

            Assert.Equal("Hey Carol", Fc.ApplyTemplate("Hey %user%", ev));
        }
    }
}
