using System;
using System.IO;
using FluentConfig.Updater;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Updater allowlist, version normalize, PE check, and PickAssetUrl (zipball must never win).
    /// Does not duplicate <see cref="GitHubUpdaterPickAssetTests"/> dll-preference / empty-assets cases.
    /// CheckForLatestRelease DownloadUrl=null is covered by <see cref="GitHubUpdaterLatestReleaseTests"/>.
    /// </summary>
    [Collection("GitHubUpdaterMock")]
    public class UpdaterContractTests
    {
        [Theory]
        [InlineData("https://github.com/example-org/example-ext/releases/download/v1.0.0/FluentConfig.dll")]
        [InlineData("https://objects.githubusercontent.com/github-production-release-asset/1/FluentConfig.dll")]
        [InlineData("https://release-assets.githubusercontent.com/github-production-release-asset/1/FluentConfig.dll")]
        [InlineData("https://github-releases.githubusercontent.com/123/FluentConfig.dll")]
        [InlineData("https://release-assets.githubusercontent.com/path/FluentConfig.dll")]
        public void IsAllowedDownloadUrl_AcceptsGitHubDownloadHosts(string url)
        {
            Assert.True(GitHubUpdater.IsAllowedDownloadUrl(url));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-url")]
        [InlineData("http://github.com/example-org/example-ext/releases/download/v1/FluentConfig.dll")]
        [InlineData("https://evil.example.test/FluentConfig.dll")]
        [InlineData("https://api.github.com/repos/example-org/example-ext/zipball")]
        [InlineData("https://githubusercontent.com.evil.example.test/FluentConfig.dll")]
        public void IsAllowedDownloadUrl_RejectsEverythingElse(string url)
        {
            Assert.False(GitHubUpdater.IsAllowedDownloadUrl(url));
        }

        [Fact]
        public void IsAllowedDownloadUrl_AllowsOverriddenApiHost()
        {
            try
            {
                GitHubUpdater.SetApiBaseUrl("http://127.0.0.1:9");
                Assert.True(GitHubUpdater.IsAllowedDownloadUrl("http://127.0.0.1:9/FluentConfig.dll"));
            }
            finally
            {
                GitHubUpdater.SetApiBaseUrl(null);
            }
        }

        [Fact]
        public void PickAssetUrl_ZipAssetOnly_ReturnsNull_NotZipball()
        {
            var release = new JObject
            {
                ["zipball_url"] = "https://example.test/zipball",
                ["assets"] = new JArray
                {
                    new JObject
                    {
                        ["name"] = "source.zip",
                        ["browser_download_url"] = "https://example.test/source.zip",
                    },
                },
            };
            Assert.Null(GitHubUpdater.PickAssetUrl(release));
        }

        [Fact]
        public void LooksLikePeImage_RejectsNonPe()
        {
            Assert.False(GitHubUpdater.LooksLikePeImage(null));
            Assert.False(GitHubUpdater.LooksLikePeImage(new byte[128]));
            var jpeg = new byte[128];
            jpeg[0] = 0xFF;
            jpeg[1] = 0xD8;
            Assert.False(GitHubUpdater.LooksLikePeImage(jpeg));
        }

        [Fact]
        public void StageUpdate_RejectedHost_ThrowsWithoutDownload()
        {
            var target = Path.Combine(Path.GetTempPath(), "fc-stage-reject", "FluentConfig.dll");
            var ex = Assert.Throws<InvalidOperationException>(() =>
                GitHubUpdater.StageUpdate("https://evil.example.test/FluentConfig.dll", target));
            Assert.Contains("allowed host", ex.Message);
        }

        [Fact]
        public void Normalize_StripsBuildMetadataSoHashDoesNotCollapseVersion()
        {
            const string informational = "0.1.0-beta.2";
            const string withRevision = "0.1.0-beta.2+9e55963aaaaaaaaaaaaaaaaaaaaaaaaaaa";
            Assert.Equal("0.1.0.2", GitHubUpdater.Normalize(informational));
            Assert.Equal("0.1.0.2", GitHubUpdater.Normalize(withRevision));
            Assert.False(GitHubUpdater.IsNewer(informational, withRevision));
            Assert.False(GitHubUpdater.IsNewer(withRevision, informational));
        }

        [Fact]
        public void GuessDevConfiguration_PrefersPathSegment()
        {
            Assert.Equal("Release", UpdateHelperLauncher.GuessDevConfiguration(@"C:\build\Host\bin\Release\net481"));
            Assert.Equal("Debug", UpdateHelperLauncher.GuessDevConfiguration(@"C:\build\Host\bin\Debug\net481"));
        }
    }
}
