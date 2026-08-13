using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentConfig;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Updater;
using Moq;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Phase 2 host-side smoke: schema build, save RPC, dropdown.refresh, without Streamer.bot GUI.
    /// </summary>
    public class Phase2HostSmokeTests
    {
        [Fact]
        public void Schema_Build_IncludesToggleTextboxDropdownPill()
        {
            var cph = CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Phase2 Smoke", "0.2.0");
            ui.Section("General", "general", s => s
                .Toggle("Enabled", "smoke_enabled").Default(true)
                .Textbox("Display name", "smoke_name").Default("smoke-user")
                .Dropdown("Channel reward", "smoke_reward_display")
                    .WithPairValue("smoke_reward_id")
                    .Options(new[] { ("rew_1", "Reward Alpha"), ("rew_2", "Reward Beta") })
                    .Refresh(() => new[] { ("rew_1", "Reward Alpha"), ("rew_2", "Reward Beta"), ("rew_3", "Reward Gamma") })
                    .DefaultByValue("rew_2")
                .PillInput("Watch list", "smoke_pills")
                    .WithItemTemplate(item => item
                        .Title("Item: {name}")
                        .Toggle("Active", "{name}_active").Default(true)
                    )
            );

            var doc = ui.Session.BuildDocumentForTests();
            Assert.Equal("Phase2 Smoke", doc.Title);
            Assert.Single(doc.Sections);
            var types = FlattenTypes(doc.Sections[0].Children).ToList();
            Assert.Contains("toggle", types);
            Assert.Contains("textbox", types);
            Assert.Contains("dropdown", types);
            Assert.Contains("pill-input", types);

            var json = ProtocolJson.Serialize(doc);
            Assert.DoesNotContain("System.Windows.Controls.Panel", json);
            Assert.Contains("smoke_enabled", json);
            Assert.Contains("itemTemplate", json);
        }

        [Fact]
        public void SaveRpc_PersistsValues_ViaMockCph()
        {
            var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var cph = CreateMockCph(store);
            var ui = FluentConfigUi.Create(cph.Object, "Phase2 Persist", "0.2.0");
            ui.Section("G", "g", s => s.Toggle("Enabled", "smoke_enabled").Textbox("Name", "smoke_name"));

            ui.Session.BuildDocumentForTests();

            var values = new JObject
            {
                ["smoke_enabled"] = false,
                ["smoke_name"] = "after-edit",
            };
            var req = ProtocolJson.Serialize(WireMessage.Request(1, RpcMethods.Save, new SaveParams { Values = values }));
            ui.Session.HandleWebMessage(req);

            var settings = ui.Session.GetSettingsForTests();
            Assert.Equal(false, settings["smoke_enabled"]?.Value<bool>());
            Assert.Equal("after-edit", settings["smoke_name"]?.ToString());

            var key = store.Keys.FirstOrDefault(k => k == "phase2_persist_settings");
            Assert.False(string.IsNullOrEmpty(key));
            var persisted = JObject.Parse(store[key]);
            Assert.Equal("after-edit", persisted["smoke_name"]?.ToString());
        }

        [Fact]
        public void DropdownRefresh_ReturnsFakeRewards()
        {
            var cph = CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Phase2 Dropdown", "0.2.0");
            ui.Section("G", "g", s => s
                .Dropdown("Reward", "smoke_reward_display")
                    .Options(new[] { ("rew_1", "A") })
                    .Refresh(() => new[] { ("rew_1", "A"), ("rew_2", "B"), ("rew_3", "C") })
            );

            ui.Session.BuildDocumentForTests();

            // Refresh is registered during schema flush; invoke the stored callback directly.
            var refreshField = typeof(FluentConfigSession).GetField("_dropdownRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshField);
            var map = (Dictionary<string, Func<IList<DropdownOption>>>)refreshField.GetValue(ui.Session);
            Assert.True(map.ContainsKey("smoke_reward_display"));
            var options = map["smoke_reward_display"]();
            Assert.Equal(3, options.Count);
            Assert.Equal("rew_3", options[2].Value);
        }

        [Fact]
        public void WithVisibilityWhenOff_EmitsInvertedCondition()
        {
            var cph = CreateMockCph();
            var ui = FluentConfigUi.Create(cph.Object, "Phase2 Vis", "0.2.0");
            ui.Section("G", "g", s => s
                .Toggle("Premium", "premium_mode")
                .WithVisibilityWhenOff("premium_mode", inner => inner
                    .Textbox("Free", "free_tier")
                )
            );

            var doc = ui.Session.BuildDocumentForTests();
            var json = ProtocolJson.Serialize(doc);
            Assert.Contains("\"inverted\":true", json.Replace(" ", ""));
            Assert.Contains("free_tier", json);
        }

        private static IEnumerable<string> FlattenTypes(IEnumerable<SchemaNode> nodes)
        {
            if (nodes == null) yield break;
            foreach (var n in nodes)
            {
                yield return n.Type;
                if (n is GroupNode g)
                {
                    foreach (var c in FlattenTypes(g.Children))
                        yield return c;
                }
            }
        }

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

    /// <summary>
    /// Updater end-to-end against a local HttpListener mock (no production hosts).
    /// </summary>
    [Collection("GitHubUpdaterMock")]
    public class UpdaterFlowTests : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _baseUrl;
        private readonly string _tempDir;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private byte[] _assetBytes;
        private string _assetPath;

        public UpdaterFlowTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "FluentConfigUpdaterTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _assetPath = Path.Combine(_tempDir, "ExampleExtension.dll");
            // Minimal valid PE stub (MZ + e_lfanew → PE\0\0) for DownloadToFile sanity check.
            _assetBytes = CreateMinimalPeBytes();

            _listener = new HttpListener();
            // Port 0 is not supported by HttpListener prefixes; pick a free port.
            var port = GetFreePort();
            _baseUrl = "http://127.0.0.1:" + port;
            _listener.Prefixes.Add(_baseUrl + "/");
            _listener.Start();
            Task.Run(() => ListenLoop(_cts.Token));

            GitHubUpdater.SetApiBaseUrl(_baseUrl);
            GitHubUpdater.SetHttpClient(new HttpClient { BaseAddress = new Uri(_baseUrl) });
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { /* ignore */ }
            _listener.Close();
            GitHubUpdater.SetApiBaseUrl(null);
            GitHubUpdater.SetHttpClient(null);
            try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
        }

        [Fact]
        public void CheckForUpdate_EnsureInstalled_StageUpdate_SwapHelper()
        {
            var check = GitHubUpdater.CheckForUpdate("example-org/example-extension", "1.0.0");
            Assert.NotNull(check);
            Assert.True(check.UpdateAvailable);
            Assert.Equal("1.1.0", check.LatestVersion);
            Assert.Contains("/download/", check.DownloadUrl);

            var installPath = Path.Combine(_tempDir, "fresh", "ExampleExtension.dll");
            Assert.False(File.Exists(installPath));
            Assert.True(GitHubUpdater.EnsureInstalled(installPath, "example-org/example-extension"));
            Assert.True(File.Exists(installPath));
            Assert.Equal(_assetBytes, File.ReadAllBytes(installPath));

            // Live file present + locked scenario: stage beside target
            var livePath = Path.Combine(_tempDir, "live", "ExampleExtension.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(livePath));
            File.WriteAllBytes(livePath, new byte[] { 0x00, 0x00 });
            var staged = GitHubUpdater.StageUpdate(check.DownloadUrl, livePath);
            Assert.Equal(livePath + ".update", staged);
            Assert.True(File.Exists(staged));
            Assert.Equal(_assetBytes, File.ReadAllBytes(staged));

            // Run UpdaterHelper swap without relaunch (no Streamer.bot process name match needed —
            // helper waits for process exit; use a fake process name that is not running).
            var helperProj = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "UpdaterHelper", "UpdaterHelper.csproj"));
            Assert.True(File.Exists(helperProj), "UpdaterHelper.csproj missing at " + helperProj);

            var build = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{helperProj}\" -c Debug",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });
            build.WaitForExit(120000);
            Assert.Equal(0, build.ExitCode);

            var helperExe = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(helperProj), "bin", "Debug", "net481", "FluentConfig.UpdaterHelper.exe"));
            Assert.True(File.Exists(helperExe), "Helper exe missing: " + helperExe);

            var swap = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = helperExe,
                Arguments = $"\"{livePath}\" \"NoSuchProcess_FluentConfigTest.exe\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });
            var exited = swap.WaitForExit(30000);
            Assert.True(exited, "UpdaterHelper did not exit in time");
            Assert.Equal(0, swap.ExitCode);

            Assert.False(File.Exists(staged), "staged .update should be moved away");
            Assert.True(File.Exists(livePath));
            Assert.Equal(_assetBytes, File.ReadAllBytes(livePath));
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync().ConfigureAwait(false); }
                catch { break; }

                try
                {
                    var path = ctx.Request.Url.AbsolutePath.TrimEnd('/');
                    if (path.EndsWith("/repos/example-org/example-extension/releases/latest", StringComparison.OrdinalIgnoreCase))
                    {
                        var downloadUrl = _baseUrl + "/download/ExampleExtension.dll";
                        var body = new JObject
                        {
                            ["tag_name"] = "v1.1.0",
                            ["body"] = "Test release notes",
                            ["assets"] = new JArray
                            {
                                new JObject
                                {
                                    ["name"] = "ExampleExtension.dll",
                                    ["browser_download_url"] = downloadUrl,
                                }
                            }
                        }.ToString();
                        WriteResponse(ctx, 200, "application/json", body);
                    }
                    else if (path.EndsWith("/repos/example-org/example-extension/releases", StringComparison.OrdinalIgnoreCase))
                    {
                        var body = new JArray
                        {
                            new JObject
                            {
                                ["tag_name"] = "other-v9.9.9",
                                ["body"] = "wrong prefix",
                                ["html_url"] = _baseUrl + "/releases/other-v9.9.9",
                            },
                            new JObject
                            {
                                ["tag_name"] = "spotify-v1.0.0",
                                ["body"] = "initial",
                                ["html_url"] = _baseUrl + "/releases/spotify-v1.0.0",
                            },
                            new JObject
                            {
                                ["tag_name"] = "spotify-v1.1.0",
                                ["body"] = "middle",
                                ["html_url"] = _baseUrl + "/releases/spotify-v1.1.0",
                            },
                            new JObject
                            {
                                ["tag_name"] = "spotify-v1.2.3",
                                ["body"] = "newest spotify",
                                ["html_url"] = _baseUrl + "/releases/spotify-v1.2.3",
                            },
                        }.ToString();
                        WriteResponse(ctx, 200, "application/json", body);
                    }
                    else if (path.EndsWith("/download/ExampleExtension.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = "application/octet-stream";
                        ctx.Response.OutputStream.Write(_assetBytes, 0, _assetBytes.Length);
                        ctx.Response.Close();
                    }
                    else
                    {
                        WriteResponse(ctx, 404, "text/plain", "not found: " + path);
                    }
                }
                catch
                {
                    try { ctx.Response.Abort(); } catch { /* ignore */ }
                }
            }
        }

        private static void WriteResponse(HttpListenerContext ctx, int status, string contentType, string body)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(body ?? "");
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>Minimal PE image that passes <c>GitHubUpdater.LooksLikePeImage</c>.</summary>
        private static byte[] CreateMinimalPeBytes()
        {
            var bytes = new byte[128];
            bytes[0] = (byte)'M';
            bytes[1] = (byte)'Z';
            BitConverter.GetBytes(0x40).CopyTo(bytes, 0x3C);
            bytes[0x40] = (byte)'P';
            bytes[0x41] = (byte)'E';
            bytes[0x42] = 0;
            bytes[0x43] = 0;
            return bytes;
        }
    }
}
