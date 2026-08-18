using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using FluentConfig;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class FilepathValidationTests
    {
        [Fact]
        public void Check_Empty_IsOk()
        {
            Assert.Null(FilepathValidation.Check("", true, new[] { ".gif" }));
            Assert.Null(FilepathValidation.Check("  ", true, null));
        }

        [Fact]
        public void Check_Url_Rejected()
        {
            Assert.Equal("Use a local file path, not a URL.", FilepathValidation.Check("https://example.test/a.gif", true, null));
        }

        [Fact]
        public void Check_MissingFile_Rejected()
        {
            Assert.Equal("File not found.", FilepathValidation.Check(@"C:\definitely-missing-fc-test-file.bin", true, null));
        }

        [Fact]
        public void Check_WrongExtension_Rejected()
        {
            var path = Path.GetTempFileName();
            try
            {
                var err = FilepathValidation.Check(path, true, new[] { ".gif", ".mp4" });
                Assert.StartsWith("Expected ", err);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Check_ExistingMatchingExtension_Ok()
        {
            var path = Path.Combine(Path.GetTempPath(), "fc-valid-" + Path.GetRandomFileName() + ".gif");
            File.WriteAllBytes(path, new byte[] { 1 });
            try
            {
                Assert.Null(FilepathValidation.Check(path, true, new[] { ".gif", ".webm" }));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ValidateAll_ExpandsPillName()
        {
            var rules = new List<FilepathRule>
            {
                new FilepathRule
                {
                    SaveKey = "{name}_gif",
                    Label = "GIF / video",
                    MustExist = true,
                    PillSaveKey = "alerts",
                },
            };
            var values = new JObject
            {
                ["alerts"] = new JArray("Rickroll"),
                ["Rickroll_gif"] = "not-a-real-file",
            };
            var errors = FilepathValidation.ValidateAll(rules, values);
            Assert.Contains(errors, e => e.StartsWith("GIF / video:"));
        }

        [Fact]
        public void FindRule_MatchesExpandedPillKey()
        {
            var rules = new List<FilepathRule>
            {
                new FilepathRule { SaveKey = "{name}_sound", Label = "Sound" },
            };
            var found = FilepathValidation.FindRule(rules, "Rickroll_sound");
            Assert.NotNull(found);
            Assert.Equal("Sound", found.Label);
        }

        [Fact]
        public void SaveRpc_RejectsMissingFilepath()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var cph = Phase2HostSmokeTests.CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "Path Check", "1.0");
            ui.Section("G", "g", s => s.Filepath("GIF / video", "gif_path"));
            ui.Session.BuildDocumentForTests();

            var req = ProtocolJson.Serialize(WireMessage.Request(
                1,
                RpcMethods.Save,
                new SaveParams { Values = new JObject { ["gif_path"] = "not-a-real-file.gif" } }));
            ui.Session.HandleWebMessage(req);

            var settings = ui.Session.GetSettingsForTests();
            Assert.NotEqual("not-a-real-file.gif", settings["gif_path"]?.ToString());
        }
    }
}
