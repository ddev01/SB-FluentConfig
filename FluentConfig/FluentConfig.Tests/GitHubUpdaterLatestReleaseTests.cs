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
    /// Notify-only <c>releases/latest</c> checks (no .dll asset required).
    /// </summary>
    [Collection("GitHubUpdaterMock")]
    public class GitHubUpdaterLatestReleaseTests : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _baseUrl;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private JObject _latestRelease;

        public GitHubUpdaterLatestReleaseTests()
        {
            var port = GetFreePort();
            _baseUrl = "http://127.0.0.1:" + port;
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUrl + "/");
            _listener.Start();
            Task.Run(() => ListenLoop(_cts.Token));

            GitHubUpdater.SetApiBaseUrl(_baseUrl);
            GitHubUpdater.SetHttpClient(new HttpClient { BaseAddress = new Uri(_baseUrl) });

            _latestRelease = new JObject
            {
                ["tag_name"] = "v1.2.0",
                ["body"] = "extension notes — no dll",
                ["html_url"] = _baseUrl + "/releases/v1.2.0",
                ["assets"] = new JArray(),
            };
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
        public void CheckForLatestRelease_WhenNewer_NotifiesWithoutDllAsset()
        {
            var check = GitHubUpdater.CheckForLatestRelease("example-org/example-extension", "1.0.0");

            Assert.NotNull(check);
            Assert.True(check.UpdateAvailable);
            Assert.Equal("1.0.0", check.CurrentVersion);
            Assert.Equal("1.2.0", check.LatestVersion);
            Assert.Null(check.DownloadUrl);
            Assert.Equal(_baseUrl + "/releases/v1.2.0", check.ReleasePageUrl);
        }

        [Fact]
        public void CheckForLatestRelease_WhenEqual_ReturnsNotAvailable()
        {
            var check = GitHubUpdater.CheckForLatestRelease("example-org/example-extension", "1.2.0");

            Assert.NotNull(check);
            Assert.False(check.UpdateAvailable);
            Assert.Equal("1.2.0", check.LatestVersion);
            Assert.Null(check.DownloadUrl);
        }

        [Fact]
        public void CheckForUpdate_WithoutDllAsset_IsNotAvailable()
        {
            // DLL path still requires a .dll asset even when the tag is newer.
            var check = GitHubUpdater.CheckForUpdate("example-org/example-extension", "1.0.0");

            Assert.NotNull(check);
            Assert.False(check.UpdateAvailable);
            Assert.Equal("1.2.0", check.LatestVersion);
            Assert.True(string.IsNullOrEmpty(check.DownloadUrl));
        }

        [Fact]
        public void CheckForLatestRelease_RequiresRepo()
        {
            Assert.Throws<ArgumentException>(() => GitHubUpdater.CheckForLatestRelease("", "1.0.0"));
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
                    if (path.EndsWith("/repos/example-org/example-extension/releases/latest",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        WriteResponse(ctx, 200, "application/json", _latestRelease.ToString());
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
