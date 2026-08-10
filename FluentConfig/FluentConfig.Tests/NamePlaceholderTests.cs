using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentConfig;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// {name} pill template expansion must substitute only inside leaf strings
    /// without corrupting JSON when names contain quotes, backslashes, or literal "{name}".
    /// </summary>
    public class NamePlaceholderTests
    {
        private static readonly MethodInfo ExpandTemplate = typeof(FluentConfigSession).GetMethod(
            "ExpandTemplate",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static IList<SchemaNode> Expand(IList<SchemaNode> template, string name) =>
            (IList<SchemaNode>)ExpandTemplate.Invoke(null, new object[] { template, name });

        private static IList<SchemaNode> SampleTemplate() => new List<SchemaNode>
        {
            new TitleNode { Text = "Item: {name}" },
            new ToggleNode { Label = "Enabled", SaveKey = "{name}_enabled" },
            new TextboxNode { Label = "Notes", SaveKey = "{name}_notes", DefaultValue = "prefix-{name}-suffix" },
        };

        [Fact]
        public void ExpandTemplate_NameWithQuotes_RoundTripsJson()
        {
            const string name = "say \"hello\"";
            var expanded = Expand(SampleTemplate(), name);
            var json = ProtocolJson.Serialize(expanded);
            var reparsed = JToken.Parse(json);

            Assert.Equal("Item: say \"hello\"", reparsed.SelectToken("$[0].text")?.Value<string>());
            Assert.Equal("say \"hello\"_enabled", reparsed.SelectToken("$[1].saveKey")?.Value<string>());
            Assert.Equal("prefix-say \"hello\"-suffix", FindDefaultValue(reparsed, "say \"hello\"_notes"));
        }

        [Fact]
        public void ExpandTemplate_NameWithBackslashes_RoundTripsJson()
        {
            const string name = @"path\to\item";
            var expanded = Expand(SampleTemplate(), name);
            var json = ProtocolJson.Serialize(expanded);
            var reparsed = JToken.Parse(json);

            Assert.Equal(@"Item: path\to\item", reparsed.SelectToken("$[0].text")?.Value<string>());
            Assert.Equal(@"path\to\item_enabled", reparsed.SelectToken("$[1].saveKey")?.Value<string>());
            Assert.Equal(@"prefix-path\to\item-suffix", FindDefaultValue(reparsed, @"path\to\item_notes"));
        }

        [Fact]
        public void ExpandTemplate_NameContainsLiteralNameSubstring_PreservesLiteral()
        {
            const string name = "foo{name}bar";
            var expanded = Expand(SampleTemplate(), name);
            var reparsed = JToken.Parse(ProtocolJson.Serialize(expanded));

            Assert.Equal("Item: foo{name}bar", reparsed.SelectToken("$[0].text")?.Value<string>());
            Assert.Equal("foo{name}bar_enabled", reparsed.SelectToken("$[1].saveKey")?.Value<string>());
            Assert.DoesNotContain("foofoo", reparsed.ToString());
        }

        [Fact]
        public void ExpandTemplate_ViaBuildDocument_PillNamesWithSpecialChars()
        {
            var store = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(
                System.StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "Name Placeholder", "1.0");
            ui.Section("G", "g", s => s
                .PillInput("Items", "items")
                    .WithItemTemplate(item => item
                        .Title("Item: {name}")
                        .Toggle("On", "{name}_on")
                    )
            );

            var key = "name_placeholder_settings";
            store[key] = new JObject
            {
                ["items"] = new JArray("say \"hello\"", @"path\to\item", "literal{name}here"),
            }.ToString();

            var doc = ui.Session.BuildDocumentForTests();
            var reparsed = JToken.Parse(ProtocolJson.Serialize(doc));
            var saveKeys = reparsed.SelectTokens("$..saveKey").Select(t => t.Value<string>()).ToList();

            Assert.Contains("say \"hello\"_on", saveKeys);
            Assert.Contains(@"path\to\item_on", saveKeys);
            Assert.Contains("literal{name}here_on", saveKeys);
        }

        private static string FindDefaultValue(JToken root, string saveKey)
        {
            foreach (var token in root.SelectTokens("$..saveKey"))
            {
                if (token.Type == JTokenType.String && token.Value<string>() == saveKey)
                {
                    var parent = token.Parent?.Parent as JObject;
                    return parent?["defaultValue"]?.Value<string>();
                }
            }
            return null;
        }
    }
}
