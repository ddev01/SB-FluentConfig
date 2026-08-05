using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentConfig.Updater;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Tag-prefix extension update checks (notify-only) against a local mock API.
    /// </summary>
    [Collection("GitHubUpdaterMock")]
    public class GitHubUpdaterTaggedReleaseTests : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _baseUrl;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public GitHubUpdaterTaggedReleaseTests()
        {
            var port = GetFreePort();
            _baseUrl = "http://127.0.0.1:" + port;
            _listener = new HttpListener();
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
        }

        [Fact]
        public void CheckForTaggedRelease_PicksHighestMatchingPrefix()
        {
            var check = GitHubUpdater.CheckForTaggedRelease("example-org/example-extension", "spotify", "1.0.0");

            Assert.NotNull(check);
            Assert.True(check.UpdateAvailable);
            Assert.Equal("1.0.0", check.CurrentVersion);
            Assert.Equal("1.2.3", check.LatestVersion);
            Assert.Equal("spotify-v1.2.3", check.TagName);
            Assert.Equal(_baseUrl + "/releases/spotify-v1.2.3", check.ReleasePageUrl);
            Assert.Null(check.DownloadUrl);
        }

        [Fact]
        public void CheckForTaggedRelease_WhenCurrentIsLatest_ReturnsNotAvailable()
        {
            var check = GitHubUpdater.CheckForTaggedRelease("example-org/example-extension", "spotify", "1.2.3");

            Assert.NotNull(check);
            Assert.False(check.UpdateAvailable);
            Assert.Equal("1.2.3", check.CurrentVersion);
            Assert.Equal("1.2.3", check.LatestVersion);
        }

        [Fact]
        public void CheckForTaggedRelease_IgnoresNonMatchingTags()
        {
            var check = GitHubUpdater.CheckForTaggedRelease("example-org/example-extension", "missing", "0.1.0");

            Assert.NotNull(check);
            Assert.False(check.UpdateAvailable);
            Assert.Equal("0.1.0", check.CurrentVersion);
            Assert.Equal("0.1.0", check.LatestVersion);
        }

        [Fact]
        public void CheckForTaggedRelease_RequiresRepoAndPrefix()
        {
            Assert.Throws<ArgumentException>(() => GitHubUpdater.CheckForTaggedRelease("", "spotify", "1.0.0"));
            Assert.Throws<ArgumentException>(() => GitHubUpdater.CheckForTaggedRelease("org/repo", "", "1.0.0"));
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
                    if (path.EndsWith("/repos/example-org/example-extension/releases", StringComparison.OrdinalIgnoreCase)
                        || path.Contains("/repos/example-org/example-extension/releases?"))
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
    }
}
