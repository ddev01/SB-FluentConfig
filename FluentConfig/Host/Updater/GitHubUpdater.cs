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
    /// Synchronous check methods may block on HTTP — call them off the UI thread (e.g. ThreadPool)
    /// when used from FluentConfig Show/bootstrap paths.
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
        /// Requires a <c>.dll</c> asset for <see cref="UpdateCheckResult.UpdateAvailable"/> (DLL install/update path).
        /// For notify-only latest checks (extensions), use <see cref="CheckForLatestRelease"/>.
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
        /// Notify-only probe of <c>releases/latest</c> — sets <see cref="UpdateCheckResult.UpdateAvailable"/>
        /// when newer even if no <c>.dll</c> asset exists. <see cref="UpdateCheckResult.DownloadUrl"/> is always null.
        /// Returns null on network/parse failure.
        /// </summary>
        public static UpdateCheckResult CheckForLatestRelease(string repo, string currentVersion)
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

            var available = !string.IsNullOrEmpty(latest)
                            && !string.IsNullOrEmpty(current)
                            && !VersionsEqual(current, latest)
                            && IsNewer(latest, current);

            return new UpdateCheckResult
            {
                UpdateAvailable = available,
                CurrentVersion = current,
                LatestVersion = latest,
                ReleaseNotes = notes,
                DownloadUrl = null,
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
            if (!IsAllowedDownloadUrl(url))
                throw new InvalidOperationException("Download URL is not on the allowed host list (https GitHub release assets).");

            const long maxBytes = 64L * 1024 * 1024; // 64 MiB — generous for a single managed DLL
            using (var response = Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > maxBytes)
                    throw new InvalidOperationException($"Update asset exceeds size cap ({maxBytes} bytes).");

                using (var remote = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using (var ms = new MemoryStream())
                {
                    var buffer = new byte[81920];
                    long total = 0;
                    int read;
                    while ((read = remote.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        if (total > maxBytes)
                            throw new InvalidOperationException($"Update asset exceeds size cap ({maxBytes} bytes).");
                        ms.Write(buffer, 0, read);
                    }

                    var bytes = ms.ToArray();
                    if (!LooksLikePeImage(bytes))
                        throw new InvalidOperationException("Downloaded update is not a valid PE image (MZ/PE header check failed).");

                    File.WriteAllBytes(path, bytes);
                }
            }
        }

        /// <summary>
        /// True when <paramref name="url"/> is https and targets a known GitHub download host,
        /// or (for tests) the host of the current <see cref="ApiBaseUrl"/> override / loopback.
        /// </summary>
        public static bool IsAllowedDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            // Test / mock escape hatch: ApiBaseUrl override or explicit env allows that host (incl. http loopback).
            var apiBase = ApiBaseUrl;
            if (Uri.TryCreate(apiBase, UriKind.Absolute, out var apiUri)
                && !string.Equals(apiUri.Host, "api.github.com", StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.Host, apiUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            return IsGitHubDownloadHost(uri.Host);
        }

        private static bool IsGitHubDownloadHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            // github.com release links + objects/release-assets CDNs
            if (host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return true;
            if (host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)) return true;
            if (host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase)) return true;
            if (host.Equals("github-releases.githubusercontent.com", StringComparison.OrdinalIgnoreCase)) return true;
            if (host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Minimal DOS/PE sanity check before staging a self-update DLL.</summary>
        internal static bool LooksLikePeImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 64) return false;
            if (bytes[0] != (byte)'M' || bytes[1] != (byte)'Z') return false;

            int peOffset = BitConverter.ToInt32(bytes, 0x3C);
            if (peOffset < 0 || peOffset + 4 > bytes.Length) return false;
            return bytes[peOffset] == (byte)'P'
                   && bytes[peOffset + 1] == (byte)'E'
                   && bytes[peOffset + 2] == 0
                   && bytes[peOffset + 3] == 0;
        }

        /// <summary>
        /// Picks a single .dll downloadable asset URL from a GitHub release JSON object.
        /// Self-update only — never falls back to <c>zipball_url</c> (source archive).
        /// Returns null when no .dll asset exists (treat as no update available).
        /// Not used by <see cref="CheckForTaggedRelease"/> (notify-only / no download).
        /// </summary>
        internal static string PickAssetUrl(JObject release)
        {
            if (release == null) return null;
            var assets = release["assets"] as JArray;
            if (assets == null || assets.Count == 0)
                return null;

            foreach (var asset in assets)
            {
                var name = asset.Value<string>("name") ?? "";
                var url = asset.Value<string>("browser_download_url");
                if (string.IsNullOrEmpty(url)) continue;
                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    return url;
            }
            return null;
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
