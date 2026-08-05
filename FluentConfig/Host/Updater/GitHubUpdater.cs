using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Updater
{
    /// <summary>
    /// Result of <see cref="GitHubUpdater.CheckForUpdate"/>.
    /// </summary>
    public sealed class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public string TagName { get; set; }
        /// <summary>GitHub release html_url (used by notify-only extension update notices).</summary>
        public string ReleasePageUrl { get; set; }
    }

    /// <summary>
    /// Generic GitHub-releases updater usable by FluentConfig itself and by any third-party extension.
    /// Unauthenticated public API only (pure read for CheckForUpdate).
    /// </summary>
    public static class GitHubUpdater
    {
        private static readonly object Sync = new object();
        private static HttpClient _http;
        private static string _apiBaseUrl;

        /// <summary>
        /// Base URL for the releases API (no trailing slash). Defaults to the public GitHub API.
        /// Override in tests via <see cref="SetApiBaseUrl"/> or env <c>FLUENTCONFIG_GITHUB_API_BASE</c>
        /// (e.g. <c>http://127.0.0.1:9xxx</c>) so fixtures never hardcode production hosts in assertions.
        /// </summary>
        public static string ApiBaseUrl
        {
            get
            {
                lock (Sync)
                {
                    if (!string.IsNullOrEmpty(_apiBaseUrl))
                        return _apiBaseUrl;
                    var fromEnv = Environment.GetEnvironmentVariable("FLUENTCONFIG_GITHUB_API_BASE");
                    if (!string.IsNullOrWhiteSpace(fromEnv))
                        return fromEnv.Trim().TrimEnd('/');
                    return "https://api.github.com";
                }
            }
        }

        /// <summary>Override API base (tests / local mock). Pass null to clear.</summary>
        public static void SetApiBaseUrl(string baseUrl)
        {
            lock (Sync)
            {
                _apiBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim().TrimEnd('/');
            }
        }

        /// <summary>Replace the shared HttpClient (tests). Pass null to restore default.</summary>
        public static void SetHttpClient(HttpClient client)
        {
            lock (Sync)
            {
                _http = client;
            }
        }

        private static HttpClient Http
        {
            get
            {
                lock (Sync)
                {
                    if (_http != null) return _http;
                    return _http = CreateClient();
                }
            }
        }

        private static HttpClient CreateClient()
        {
            // TLS 1.2 required for the public GitHub API on older .NET Framework defaults.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FluentConfig-Updater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        /// <summary>
        /// GET {ApiBaseUrl}/repos/{repo}/releases/latest and compare to <paramref name="currentVersion"/>.
        /// Pure read — safe to call on every UI open. Returns null on network/parse failure.
        /// </summary>
        /// <param name="repo">owner/name</param>
        /// <param name="currentVersion">SemVer-ish string (leading v stripped)</param>
        public static UpdateCheckResult CheckForUpdate(string repo, string currentVersion)
        {
            if (string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("repo is required (owner/name).", nameof(repo));

            var url = ApiBaseUrl + "/repos/" + repo.Trim() + "/releases/latest";
            string body;
            try
            {
                body = Http.GetStringAsync(url).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return null;
            }

            JObject json;
            try { json = JObject.Parse(body); }
            catch { return null; }

            var tag = json.Value<string>("tag_name") ?? "";
            var latest = StripV(tag);
            var current = StripV(currentVersion ?? "");
            var notes = json.Value<string>("body");
            var downloadUrl = PickAssetUrl(json);

            var available = !string.IsNullOrEmpty(latest)
                            && !string.IsNullOrEmpty(current)
                            && !VersionsEqual(current, latest)
                            && IsNewer(latest, current);

            return new UpdateCheckResult
            {
                UpdateAvailable = available && !string.IsNullOrEmpty(downloadUrl),
                CurrentVersion = current,
                LatestVersion = latest,
                ReleaseNotes = notes,
                DownloadUrl = downloadUrl,
                TagName = tag,
                ReleasePageUrl = json.Value<string>("html_url"),
            };
        }

        /// <summary>
        /// Lists <c>/repos/{repo}/releases</c> (paginated), filters tags starting with
        /// <c>{tagPrefix}-v</c> (e.g. <c>spotify-v1.2.3</c>), and picks the highest semver.
        /// Notify-only — no asset download URL required. Returns null on network/parse failure.
        /// </summary>
        /// <param name="repo">owner/name</param>
        /// <param name="tagPrefix">Extension tag prefix without trailing <c>-v</c> (e.g. <c>spotify</c>)</param>
        /// <param name="currentVersion">SemVer-ish string (leading v stripped)</param>
        public static UpdateCheckResult CheckForTaggedRelease(string repo, string tagPrefix, string currentVersion)
        {
            if (string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("repo is required (owner/name).", nameof(repo));
            if (string.IsNullOrWhiteSpace(tagPrefix))
                throw new ArgumentException("tagPrefix is required.", nameof(tagPrefix));

            var prefix = tagPrefix.Trim().TrimEnd('-') + "-v";
            var current = StripV(currentVersion ?? "");

            string bestTag = null;
            string bestVersion = null;
            string bestNotes = null;
            string bestHtmlUrl = null;

            try
            {
                for (int page = 1; page <= 10; page++)
                {
                    var url = ApiBaseUrl + "/repos/" + repo.Trim()
                              + "/releases?per_page=100&page=" + page;
                    string body;
                    try
                    {
                        body = Http.GetStringAsync(url).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        return null;
                    }

                    JArray releases;
                    try { releases = JArray.Parse(body); }
                    catch { return null; }

                    if (releases.Count == 0)
                        break;

                    foreach (var item in releases)
                    {
                        if (!(item is JObject release)) continue;
                        var tag = release.Value<string>("tag_name") ?? "";
                        if (!tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var version = StripV(tag.Substring(prefix.Length));
                        if (string.IsNullOrEmpty(version))
                            continue;

                        if (bestVersion == null || IsNewer(version, bestVersion))
                        {
                            bestTag = tag;
                            bestVersion = version;
                            bestNotes = release.Value<string>("body");
                            bestHtmlUrl = release.Value<string>("html_url");
                        }
                    }

                    if (releases.Count < 100)
                        break;
                }
            }
            catch
            {
                return null;
            }

            if (string.IsNullOrEmpty(bestVersion))
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = current,
                    LatestVersion = current,
                };
            }

            var available = !string.IsNullOrEmpty(current)
                            && !VersionsEqual(current, bestVersion)
                            && IsNewer(bestVersion, current);

            return new UpdateCheckResult
            {
                UpdateAvailable = available,
                CurrentVersion = current,
                LatestVersion = bestVersion,
                ReleaseNotes = bestNotes,
                TagName = bestTag,
                ReleasePageUrl = bestHtmlUrl,
                DownloadUrl = null,
            };
        }

        /// <summary>
        /// First-time install: download the latest release asset to <paramref name="path"/> when the file does not exist.
        /// No file-lock concern. Returns true if installed (or already present).
        /// </summary>
        public static bool EnsureInstalled(string path, string repo)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is required.", nameof(path));
            if (string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("repo is required.", nameof(repo));

            if (File.Exists(path))
                return true;

            var check = CheckForUpdate(repo, "0.0.0");
            if (check == null || string.IsNullOrEmpty(check.DownloadUrl))
                return false;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            DownloadToFile(check.DownloadUrl, path);
            return File.Exists(path);
        }

        /// <summary>
        /// Downloads the update to <c>targetPath + ".update"</c> because the live file is locked
        /// while Streamer.bot holds it. Pair with <see cref="UpdateHelperLauncher"/> to swap after exit.
        /// </summary>
        public static string StageUpdate(string downloadUrl, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new ArgumentException("downloadUrl is required.", nameof(downloadUrl));
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("targetPath is required.", nameof(targetPath));

            var staged = targetPath + ".update";
            var dir = Path.GetDirectoryName(staged);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            DownloadToFile(downloadUrl, staged);
            return staged;
        }

        private static void DownloadToFile(string url, string path)
        {
            var bytes = Http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Picks a single downloadable asset URL from a GitHub release JSON object.
        /// Self-update only (expects one primary .dll asset). Prefer a .dll
        /// <c>browser_download_url</c>; otherwise the first asset URL, else <c>zipball_url</c>.
        /// Not used by <see cref="CheckForTaggedRelease"/> (notify-only / no download).
        /// </summary>
        private static string PickAssetUrl(JObject release)
        {
            var assets = release["assets"] as JArray;
            if (assets == null || assets.Count == 0)
                return release.Value<string>("zipball_url");

            // Prefer a .dll asset; otherwise first browser_download_url.
            string fallback = null;
            foreach (var asset in assets)
            {
                var name = asset.Value<string>("name") ?? "";
                var url = asset.Value<string>("browser_download_url");
                if (string.IsNullOrEmpty(url)) continue;
                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    return url;
                if (fallback == null) fallback = url;
            }
            return fallback;
        }

        private static string StripV(string version)
        {
            if (string.IsNullOrEmpty(version)) return "";
            version = version.Trim();
            if (version.Length > 0 && (version[0] == 'v' || version[0] == 'V'))
                version = version.Substring(1);
            return version;
        }

        private static bool VersionsEqual(string a, string b) =>
            string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

        private static string Normalize(string v) => Regex.Replace(v ?? "", @"[^0-9.]+", "");

        /// <summary>Simple dotted-numeric compare; non-numeric suffixes ignored.</summary>
        public static bool IsNewer(string candidate, string current)
        {
            var cParts = Normalize(candidate).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var curParts = Normalize(current).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var len = Math.Max(cParts.Length, curParts.Length);
            for (int i = 0; i < len; i++)
            {
                int c = i < cParts.Length && int.TryParse(cParts[i], out var cv) ? cv : 0;
                int u = i < curParts.Length && int.TryParse(curParts[i], out var uv) ? uv : 0;
                if (c != u) return c > u;
            }
            return false;
        }
    }
}
