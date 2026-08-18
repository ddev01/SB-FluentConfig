using System;
using System.Collections.Generic;
using FluentConfig;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class ButtonClickRoutingTests
    {
        [Fact]
        public void TryResolve_ExactId_Wins()
        {
            Action<UiContext> exact = _ => { };
            Action<UiContext> tmpl = _ => { };
            var map = new Dictionary<string, Action<UiContext>>
            {
                ["btn_copy"] = exact,
                ["btn_browse_giphy__{name}"] = tmpl,
            };

            Assert.True(ButtonClickRouting.TryResolve(map, "btn_copy", out var cb, out var name));
            Assert.Same(exact, cb);
            Assert.Null(name);
        }

        [Fact]
        public void TryResolve_ExpandedPillButton_ReturnsItemName()
        {
            Action<UiContext> tmpl = _ => { };
            var map = new Dictionary<string, Action<UiContext>>
            {
                ["btn_browse_giphy__{name}"] = tmpl,
            };

            Assert.True(ButtonClickRouting.TryResolve(map, "btn_browse_giphy__Rickroll", out var cb, out var name));
            Assert.Same(tmpl, cb);
            Assert.Equal("Rickroll", name);
        }

        [Fact]
        public void TryResolve_ExpandedDropdownSaveKey_MatchesTemplate()
        {
            Func<int> refresh = () => 1;
            var map = new Dictionary<string, Func<int>>
            {
                ["{name}_reward_display"] = refresh,
            };

            Assert.True(ButtonClickRouting.TryResolve(map, "test_reward_display", out var fn, out var name));
            Assert.Same(refresh, fn);
            Assert.Equal("test", name);
        }

        [Fact]
        public void TryResolve_UnknownId_Fails()
        {
            var map = new Dictionary<string, Action<UiContext>>
            {
                ["btn_browse_giphy__{name}"] = _ => { },
            };
            Assert.False(ButtonClickRouting.TryResolve(map, "btn_other", out _, out _));
        }

        [Fact]
        public void ItemTemplate_ButtonIdKeepsNamePlaceholder()
        {
            var store = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(
                System.StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "Pill Buttons", "1.0");
            ui.Section("G", "g", s => s
                .Button("Top")
                .PillInput("Alerts", "alerts")
                    .WithItemTemplate(item => item
                        .Button("Browse Giphy")
                    )
            );

            store["pill_buttons_settings"] = new JObject
            {
                ["alerts"] = new JArray("Rickroll"),
            }.ToString();

            var json = ProtocolJson.Serialize(ui.Session.BuildDocumentForTests());
            Assert.Contains("btn_browse_giphy__{name}", json);
            Assert.Contains("btn_browse_giphy__Rickroll", json);
            Assert.Contains("Browse Giphy", json);
            Assert.DoesNotContain("\"text\":\"OK\"", json.Replace(" ", ""));
        }

        [Fact]
        public void ButtonClick_ExpandedPillId_SetsItemName()
        {
            string itemName = null;
            var store = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(
                System.StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "Pill Click", "1.0");
            ui.Section("G", "g", s => s
                .PillInput("Alerts", "alerts")
                    .WithItemTemplate(item => item
                        .Button("Browse Giphy")
                            .OnClick(ctx => itemName = ctx.ItemName)
                    )
            );

            store["pill_click_settings"] = new JObject
            {
                ["alerts"] = new JArray("Rickroll"),
            }.ToString();
            ui.Session.BuildDocumentForTests();

            var req = ProtocolJson.Serialize(WireMessage.Request(
                1,
                RpcMethods.ButtonClick,
                new ButtonClickParams { ButtonId = "btn_browse_giphy__Rickroll", Values = new JObject() }));
            ui.Session.HandleWebMessage(req);

            Assert.Equal("Rickroll", itemName);
        }
    }
}
