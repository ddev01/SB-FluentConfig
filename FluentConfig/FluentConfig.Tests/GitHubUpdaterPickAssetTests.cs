using FluentConfig.Updater;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class GitHubUpdaterPickAssetTests
    {
        [Fact]
        public void PickAssetUrl_EmptyAssets_ReturnsNull_NotZipball()
        {
            var release = new JObject
            {
                ["zipball_url"] = "https://example.test/zip",
                ["assets"] = new JArray(),
            };
            Assert.Null(GitHubUpdater.PickAssetUrl(release));
        }

        [Fact]
        public void PickAssetUrl_PrefersDllAsset()
        {
            var release = new JObject
            {
                ["assets"] = new JArray
                {
                    new JObject
                    {
                        ["name"] = "notes.txt",
                        ["browser_download_url"] = "https://example.test/notes.txt",
                    },
                    new JObject
                    {
                        ["name"] = "FluentConfig.dll",
                        ["browser_download_url"] = "https://example.test/FluentConfig.dll",
                    },
                },
            };
            Assert.Equal("https://example.test/FluentConfig.dll", GitHubUpdater.PickAssetUrl(release));
        }

        [Fact]
        public void LooksLikePeImage_RejectsTooShort()
        {
            Assert.False(GitHubUpdater.LooksLikePeImage(new byte[] { 0x4D, 0x5A }));
        }
    }
}
