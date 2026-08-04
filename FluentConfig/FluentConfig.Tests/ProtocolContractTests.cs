using System.Collections.Generic;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Round-trip checks against the Phase 0 protocol contract (Host/Protocol).
    /// These compile against the Protocol surface.
    /// </summary>
    public class ProtocolContractTests
    {
        [Fact]
        public void Serialize_ToggleNode_UsesCamelCaseTypeDiscriminator()
        {
            var node = new ToggleNode
            {
                Id = "optional_limit_enabled",
                Label = "Enable optional limit",
                SaveKey = "optional_limit_enabled",
                Hint = "Enable and set a limit (0–100).",
                DefaultValue = false
            };

            var json = ProtocolJson.Serialize(node);
            var jo = JObject.Parse(json);

            Assert.Equal("toggle", jo["type"]?.ToString());
            Assert.Equal("optional_limit_enabled", jo["saveKey"]?.ToString());
            Assert.Equal(false, jo["defaultValue"]?.Value<bool>());
        }

        [Fact]
        public void Serialize_PillInput_CarriesItemTemplateNotPanel()
        {
            var node = new PillInputNode
            {
                Id = "test_items",
                Label = "Test items",
                SaveKey = "test_items",
                ItemTemplate = new List<SchemaNode>
                {
                    new TitleNode { Text = "Item: {name}" },
                    new ToggleNode { Label = "Enabled", SaveKey = "{name}_enabled", DefaultValue = true }
                },
                Items = new List<PillItemSchema>
                {
                    new PillItemSchema
                    {
                        Name = "Alpha",
                        Children = new List<SchemaNode>
                        {
                            new TitleNode { Text = "Item: Alpha" }
                        }
                    }
                }
            };

            var json = ProtocolJson.Serialize(node);
            var jo = JObject.Parse(json);

            Assert.Equal("pill-input", jo["type"]?.ToString());
            Assert.NotNull(jo["itemTemplate"]);
            Assert.Equal("Alpha", jo["items"]?[0]?["name"]?.ToString());
            Assert.DoesNotContain("Panel", json);
            Assert.DoesNotContain("StackPanel", json);
        }

        [Fact]
        public void Serialize_Visibility_InvertedGroup()
        {
            var node = new GroupNode
            {
                Id = "free_tier_group",
                Visibility = new VisibilityCondition
                {
                    SaveKey = "premium_mode",
                    EqualsValue = true,
                    Inverted = true
                },
                Children = new List<SchemaNode>
                {
                    new TextboxNode { Label = "Free tier setting", SaveKey = "free_tier_setting" }
                }
            };

            var json = ProtocolJson.Serialize(node);
            var jo = JObject.Parse(json);

            Assert.Equal("group", jo["type"]?.ToString());
            Assert.Equal(true, jo["visibility"]?["inverted"]?.Value<bool>());
            Assert.Equal("premium_mode", jo["visibility"]?["saveKey"]?.ToString());
        }

        [Fact]
        public void Deserialize_RoundTrips_UpdateNotice()
        {
            var original = new UpdateNoticeNode
            {
                Id = "self-update",
                CurrentVersion = "1.0.0",
                LatestVersion = "1.1.0",
                ReleaseNotes = "Bug fixes.",
                DownloadUrl = "https://example.test/releases/download/v1.1.0/Extension.dll",
                Repo = "example-org/example-extension",
                Dismissible = true
            };

            var json = ProtocolJson.Serialize(original);
            var back = ProtocolJson.Deserialize<SchemaNode>(json) as UpdateNoticeNode;

            Assert.NotNull(back);
            Assert.Equal("1.1.0", back.LatestVersion);
            Assert.Equal("example-org/example-extension", back.Repo);
            Assert.StartsWith("https://example.test/", back.DownloadUrl);
        }

        [Fact]
        public void WireMessage_RequestResponse_Envelope()
        {
            var req = WireMessage.Request(42, RpcMethods.DropdownRefresh, new DropdownRefreshParams { SaveKey = "dropdown_choice" });
            var reqJson = ProtocolJson.Serialize(req);
            var reqJo = JObject.Parse(reqJson);
            Assert.Equal("request", reqJo["kind"]?.ToString());
            Assert.Equal(42, reqJo["id"]?.Value<long>());
            Assert.Equal("dropdown.refresh", reqJo["method"]?.ToString());

            var resp = WireMessage.ResponseResult(42, new DropdownRefreshResult
            {
                Options = new List<DropdownOption>
                {
                    new DropdownOption { Value = "Option A", Display = "Option A" }
                }
            });
            var respJson = ProtocolJson.Serialize(resp);
            var respJo = JObject.Parse(respJson);
            Assert.Equal("response", respJo["kind"]?.ToString());
            Assert.Equal(42, respJo["id"]?.Value<long>());
            Assert.NotNull(respJo["result"]);
        }

        [Fact]
        public void WireMessage_BootstrapEvent_Shape()
        {
            var doc = new UiDocument
            {
                Title = "Example Extension",
                Version = "1.0",
                ColorScheme = "dark",
                Sections = new List<SectionSchema>
                {
                    new SectionSchema
                    {
                        Id = "General",
                        Title = "General settings",
                        Children = new List<SchemaNode>
                        {
                            new DescriptionNode { Text = "Configure the extension." }
                        }
                    }
                },
                Values = new JObject { ["rate_value"] = 1 }
            };

            var evt = WireMessage.Push(PushEventNames.Bootstrap, doc);
            var json = ProtocolJson.Serialize(evt);
            var jo = JObject.Parse(json);

            Assert.Equal("event", jo["kind"]?.ToString());
            Assert.Equal("bootstrap", jo["event"]?.ToString());
            Assert.Equal("Example Extension", jo["payload"]?["title"]?.ToString());
            Assert.Equal(1, jo["payload"]?["values"]?["rate_value"]?.Value<int>());
        }
    }
}
