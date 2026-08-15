using System;
using System.Linq;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class DropdownFlagsTests
    {
        [Fact]
        public void Searchable_AllowCustom_Multiple_EmitFlags()
        {
            var cph = Phase2HostSmokeTests.CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Dropdown Flags", "1.0");
            ui.Section("G", "g", s => s
                .Combobox("Groups", "include_groups")
                    .Searchable()
                    .AllowCustom()
                    .Multiple()
                    .Options(new[] { "vip", "sub" })
            );

            var doc = ui.Session.BuildDocumentForTests();
            var node = Assert.IsType<DropdownNode>(doc.Sections[0].Children.Single(n => n is DropdownNode));
            Assert.True(node.Searchable);
            Assert.True(node.AllowCustom);
            Assert.True(node.Multiple);
            var jo = JObject.Parse(ProtocolJson.Serialize(node));
            Assert.Equal(true, jo["searchable"]?.Value<bool>());
            Assert.Equal(true, jo["allowCustom"]?.Value<bool>());
            Assert.Equal(true, jo["multiple"]?.Value<bool>());
            Assert.True(doc.Values["include_groups"] is JArray);
            Assert.Empty((JArray)doc.Values["include_groups"]);
        }

        [Fact]
        public void PlainDropdown_OmitsNewFlags()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginDropdown("Choice", "dropdown_choice");
            pending.Options(new[] { "A", "B" });
            pending.Flush();

            var json = ProtocolJson.Serialize(Assert.Single(list.ToList()));
            Assert.DoesNotContain("searchable", json);
            Assert.DoesNotContain("allowCustom", json);
            Assert.DoesNotContain("\"multiple\"", json);
        }

        [Fact]
        public void Multiple_WithPairValue_Throws()
        {
            var list = new SchemaNodeList();
            var pending = new PendingControl(session: null, list);
            pending.BeginDropdown("T", "timer_display");
            pending.WithPairValue("timer_id");
            pending.Multiple();
            var ex = Assert.Throws<InvalidOperationException>(() => pending.Flush());
            Assert.Contains("Multiple", ex.Message);
        }
    }
}
