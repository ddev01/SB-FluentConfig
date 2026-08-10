// Deployment 01 — FluentConfig.dll install + daily update check.
// NO FluentConfig.dll reference — Streamer.bot must compile this even when the DLL is missing.
// Chain order: this action → menu action (with FluentConfig), e.g. 02_ExtensionUpdateNotice.cs.
//
// Setup: Execute C# Code (default entry point) or Execute C# Method.
// Refs (GAC / framework only — portable on import, no Find Refs for Newtonsoft):
//   System.Net.Http, System.Windows.Forms
//   (+ mscorlib / System usually auto-added)
// First install: auto-download from GitHub; optional toast before download (no confirmation).
// See docs/guides/UPDATES.md.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

public class CPHInline
{
    // ── Edit these when publishing ──────────────────────────────────────
    const string UpdateRepo = "ddev01/SB-FluentConfig"; // owner/name
    const string MinStreamerBotVersion = "1.0.0";
    const string DllFileName = "FluentConfig.dll";
    const string HelperFileName = "FluentConfig.UpdaterHelper.exe";
    const string GeneralSettingsGlobal = "fluentconfig_settings";
    const string LegacyDllCheckLastUtcGlobal = "FluentConfig_DllCheck_LastUtc";
    const string DllCheckLastUtcKey = "dll_check_last_utc";
    // ────────────────────────────────────────────────────────────────────

    static readonly HttpClient Http = CreateHttp();

    public bool Execute()
    {
        try
        {
            CPH.LogInfo("[FluentConfig DllCheck] check starting");
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var sbDir = ResolveStreamerBotDir();
            if (string.IsNullOrEmpty(sbDir))
            {
                CPH.LogError("[FluentConfig DllCheck] could not locate Streamer.bot install directory");
                Notify(
                    "FluentConfig",
                    "Could not locate the Streamer.bot install directory.",
                    blockingPopup: true,
                    icon: MessageBoxIcon.Error);
                return false;
            }

            var dllsDir = Path.Combine(sbDir, "dlls");
            var dllPath = Path.Combine(dllsDir, DllFileName);
            Directory.CreateDirectory(dllsDir);

            if (!File.Exists(dllPath))
            {
                CPH.LogInfo("[FluentConfig DllCheck] FluentConfig.dll missing; installing from GitHub (repo=" + UpdateRepo + ")");
                // Action queue is often non-UI — toast alone can silently no-op. Show a blocking
                // OK dialog on an STA thread (no Yes/No), then download.
                Notify(
                    "Installing FluentConfig",
                    "FluentConfig.dll is missing and will be downloaded from GitHub now.\n\n"
                    + "First open may take a moment.",
                    blockingPopup: true);

                string installError;
                if (!TryInstallLatest(dllPath, out installError))
                {
                    CPH.LogError("[FluentConfig DllCheck] install failed: " + (installError ?? "unknown"));
                    Notify(
                        "FluentConfig install failed",
                        "FluentConfig.dll could not be downloaded from GitHub.\n\n"
                        + "Repo: " + UpdateRepo + "\n"
                        + (string.IsNullOrEmpty(installError) ? "" : "Detail: " + installError),
                        blockingPopup: true,
                        icon: MessageBoxIcon.Error);
                    return false;
                }

                var installedVersion = ReadFileVersion(dllPath);
                CPH.LogInfo("[FluentConfig DllCheck] installed FluentConfig.dll v" + installedVersion);
            }

            var sbVersion = SafeGetVersion();
            if (!string.IsNullOrEmpty(MinStreamerBotVersion)
                && (string.IsNullOrEmpty(sbVersion) || IsNewer(MinStreamerBotVersion, sbVersion)))
            {
                CPH.LogWarn(
                    "[FluentConfig DllCheck] Streamer.bot version gate failed: need "
                    + MinStreamerBotVersion
                    + ", current="
                    + (string.IsNullOrEmpty(sbVersion) ? "(unknown)" : sbVersion));
                Notify(
                    "FluentConfig",
                    "FluentConfig requires Streamer.bot version " + MinStreamerBotVersion
                    + " or higher. You currently are on version "
                    + (string.IsNullOrEmpty(sbVersion) ? "(unknown)" : sbVersion)
                    + ". Please update Streamer.bot.",
                    blockingPopup: true,
                    icon: MessageBoxIcon.Warning);
                return false;
            }

            if (IsWithinDailyThrottle())
            {
                CPH.LogInfo("[FluentConfig DllCheck] skipped (daily throttle)");
                return true;
            }

            MarkChecked();

            var onDisk = ReadFileVersion(dllPath);
            if (string.IsNullOrEmpty(onDisk))
                onDisk = "0.0.0";

            CPH.LogInfo("[FluentConfig DllCheck] on-disk FluentConfig.dll v" + onDisk);

            var release = FetchLatestRelease();
            if (release == null || string.IsNullOrEmpty(release.DllUrl))
            {
                CPH.LogWarn("[FluentConfig DllCheck] GitHub release lookup failed or no .dll asset");
                return true;
            }

            if (!IsNewer(release.Version, onDisk))
            {
                CPH.LogInfo("[FluentConfig DllCheck] up to date (latest v" + release.Version + ")");
                return true;
            }

            CPH.LogInfo(
                "[FluentConfig DllCheck] update available: v"
                + onDisk
                + " -> v"
                + release.Version
                + "; staging");

            try
            {
                StageFile(release.DllUrl, dllPath + ".update");
                if (!string.IsNullOrEmpty(release.HelperUrl))
                {
                    var helperPath = Path.Combine(dllsDir, HelperFileName);
                    try { StageFile(release.HelperUrl, helperPath + ".update"); }
                    catch (Exception ex)
                    {
                        CPH.LogWarn("[FluentConfig DllCheck] helper stage failed (best-effort): " + ex.Message);
                    }
                }

                LaunchHelper(dllPath);
                CPH.LogInfo("[FluentConfig DllCheck] update staged; restart Streamer.bot to apply");
                Notify(
                    "FluentConfig update staged",
                    "Restart Streamer.bot to finish updating FluentConfig.dll.",
                    blockingPopup: false);
            }
            catch (Exception ex)
            {
                CPH.LogWarn("[FluentConfig DllCheck] stage failed: " + ex.Message);
            }

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogError("[FluentConfig DllCheck] check failed: " + ex.Message);
            Notify(
                "FluentConfig",
                "FluentConfig DLL check failed:\n" + ex.Message,
                blockingPopup: true,
                icon: MessageBoxIcon.Error);
            return false;
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────

    static HttpClient CreateHttp()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FluentConfig-DllCheck/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    string SafeGetVersion()
    {
        try { return CPH.GetVersion(); }
        catch { return null; }
    }

    /// <summary>
    /// Native SB toast (same overloads as Tawmae / docs) plus a WinForms MessageBox.
    /// DllCheck often runs on Streamer.bot's non-blocking Default queue (not the UI thread),
    /// so MessageBox must run on an STA thread or it never appears.
    /// </summary>
    void Notify(string title, string message, bool blockingPopup, MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        TryShowToast(title, message);

        try
        {
            Exception threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            if (blockingPopup)
                t.Join();
            if (threadEx != null)
                CPH.LogWarn("[FluentConfig DllCheck] MessageBox failed: " + threadEx.Message);
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[FluentConfig DllCheck] notify popup failed: " + ex.Message);
        }
    }

    void TryShowToast(string title, string message)
    {
        // Docs: ShowToastNotification(title, message) and (title, message, attribution, iconPath).
        // Tawmae dll_check uses the 4-arg form. Prefer 2-arg; fall back to 4-arg.
        try
        {
            CPH.ShowToastNotification(title, message);
            return;
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[FluentConfig DllCheck] toast (2-arg) failed: " + ex.Message);
        }

        try
        {
            CPH.ShowToastNotification(title, message, "FluentConfig", "");
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[FluentConfig DllCheck] toast (4-arg) failed: " + ex.Message);
        }
    }

    string ResolveStreamerBotDir()
    {
        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                var file = proc.MainModule?.FileName;
                if (!string.IsNullOrEmpty(file))
                    return Path.GetDirectoryName(file);
            }
        }
        catch { /* access denied on MainModule is fine */ }

        try
        {
            var entry = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entry))
                return Path.GetDirectoryName(entry);
        }
        catch { }

        return AppDomain.CurrentDomain.BaseDirectory?.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    bool IsWithinDailyThrottle()
    {
        try
        {
            var raw = ReadDllCheckLastUtc();
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!DateTime.TryParse(raw, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var last))
                return false;
            if (last.Kind == DateTimeKind.Unspecified)
                last = DateTime.SpecifyKind(last, DateTimeKind.Utc);
            return DateTime.UtcNow - last.ToUniversalTime() < TimeSpan.FromHours(24);
        }
        catch { return false; }
    }

    void MarkChecked()
    {
        try
        {
            WriteDllCheckLastUtc(DateTime.UtcNow.ToString("o"));
        }
        catch { }
    }

    string ReadDllCheckLastUtc()
    {
        try
        {
            var json = CPH.GetGlobalVar<string>(GeneralSettingsGlobal, true);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var fromGeneral = ExtractJsonStringValue(json, DllCheckLastUtcKey);
                if (!string.IsNullOrWhiteSpace(fromGeneral))
                    return fromGeneral;
            }

            return CPH.GetGlobalVar<string>(LegacyDllCheckLastUtcGlobal, true);
        }
        catch { return null; }
    }

    void WriteDllCheckLastUtc(string utc)
    {
        try
        {
            var json = CPH.GetGlobalVar<string>(GeneralSettingsGlobal, true)?.Trim();
            if (string.IsNullOrEmpty(json))
            {
                CPH.SetGlobalVar(
                    GeneralSettingsGlobal,
                    "{\"dll_check_last_utc\":\"" + EscapeJsonString(utc) + "\"}",
                    true);
                return;
            }

            var re = new Regex("\"dll_check_last_utc\"\\s*:\\s*\"[^\"]*\"");
            if (re.IsMatch(json))
            {
                CPH.SetGlobalVar(
                    GeneralSettingsGlobal,
                    re.Replace(json, "\"dll_check_last_utc\":\"" + EscapeJsonString(utc) + "\""),
                    true);
                return;
            }

            if (json.StartsWith("{") && json.EndsWith("}"))
                CPH.SetGlobalVar(
                    GeneralSettingsGlobal,
                    "{\"dll_check_last_utc\":\"" + EscapeJsonString(utc) + "\"," + json.Substring(1),
                    true);
        }
        catch { }
    }

    static string ExtractJsonStringValue(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return null;

        var match = Regex.Match(
            json,
            "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    static string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    bool TryInstallLatest(string dllPath, out string error)
    {
        error = null;
        LatestRelease release;
        try
        {
            release = FetchLatestRelease();
        }
        catch (Exception ex)
        {
            error = "GitHub lookup failed: " + ex.Message;
            return false;
        }

        if (release == null)
        {
            error = "No response from GitHub releases/latest.";
            return false;
        }
        if (string.IsNullOrEmpty(release.DllUrl))
        {
            error = "Release " + (release.Version ?? "?") + " has no .dll asset (parse/name mismatch).";
            return false;
        }

        try
        {
            DownloadToFile(release.DllUrl, dllPath);

            if (!string.IsNullOrEmpty(release.HelperUrl))
            {
                var helperPath = Path.Combine(Path.GetDirectoryName(dllPath) ?? "", HelperFileName);
                try { DownloadToFile(release.HelperUrl, helperPath); }
                catch { /* best-effort */ }
            }
        }
        catch (Exception ex)
        {
            error = "Download failed: " + ex.Message;
            return false;
        }

        if (!File.Exists(dllPath))
        {
            error = "Download finished but file was not written.";
            return false;
        }
        return true;
    }

    sealed class LatestRelease
    {
        public string Version;
        public string DllUrl;
        public string HelperUrl;
    }

    LatestRelease FetchLatestRelease()
    {
        var url = "https://api.github.com/repos/" + UpdateRepo.Trim() + "/releases/latest";
        var body = Http.GetStringAsync(url).GetAwaiter().GetResult();
        var tag = JsonString(body, "tag_name") ?? "";
        var version = StripV(tag);
        string dllUrl = null;
        string helperUrl = null;

        // Only scan the assets array — a top-level release "name" (title) would otherwise
        // swallow the first asset's name→url pair and leave FluentConfig.dll unmatched.
        var assetsJson = SliceJsonArray(body, "assets") ?? body;
        var assetRx = new Regex(
            "\"name\"\\s*:\\s*\"([^\"]+)\"[\\s\\S]*?\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (Match m in assetRx.Matches(assetsJson))
        {
            var name = UnescapeJson(m.Groups[1].Value);
            var download = UnescapeJson(m.Groups[2].Value);
            if (string.IsNullOrEmpty(download)) continue;
            if (name.Equals(DllFileName, StringComparison.OrdinalIgnoreCase)
                || (dllUrl == null && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                dllUrl = download;
            if (name.Equals(HelperFileName, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("UpdaterHelper.exe", StringComparison.OrdinalIgnoreCase))
                helperUrl = download;
        }

        return new LatestRelease
        {
            Version = version,
            DllUrl = dllUrl,
            HelperUrl = helperUrl,
        };
    }

    /// <summary>Returns the JSON array text for <paramref name="property"/>, or null.</summary>
    static string SliceJsonArray(string json, string property)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(property)) return null;
        var key = "\"" + property + "\"";
        var keyIdx = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIdx < 0) return null;
        var bracket = json.IndexOf('[', keyIdx + key.Length);
        if (bracket < 0) return null;
        var depth = 0;
        for (int i = bracket; i < json.Length; i++)
        {
            var c = json[i];
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                    return json.Substring(bracket, i - bracket + 1);
            }
        }
        return null;
    }

    static string JsonString(string json, string property)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(property)) return null;
        var m = Regex.Match(
            json,
            "\"" + Regex.Escape(property) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.CultureInvariant);
        return m.Success ? UnescapeJson(m.Groups[1].Value) : null;
    }

    static string UnescapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    static string ReadFileVersion(string path)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            // Prefer FileVersion (numeric PE) so 0.1.0-beta.1 tags normalize to the same dotted form.
            var v = info.FileVersion ?? info.ProductVersion;
            return StripV(v ?? "");
        }
        catch { return ""; }
    }

    void StageFile(string downloadUrl, string stagedPath)
    {
        DownloadToFile(downloadUrl, stagedPath);
    }

    void DownloadToFile(string url, string path)
    {
        if (!IsAllowedDownloadUrl(url))
            throw new InvalidOperationException("Download URL host not allowed.");

        const long maxBytes = 64L * 1024 * 1024;
        using (var response = Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
        {
            response.EnsureSuccessStatusCode();
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
                        throw new InvalidOperationException("Update asset exceeds size cap.");
                    ms.Write(buffer, 0, read);
                }

                var bytes = ms.ToArray();
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".dll.update", StringComparison.OrdinalIgnoreCase))
                {
                    if (!LooksLikePeImage(bytes))
                        throw new InvalidOperationException("Downloaded file is not a valid PE image.");
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, bytes);
            }
        }
    }

    static bool IsAllowedDownloadUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;
        var host = uri.Host ?? "";
        if (host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static bool LooksLikePeImage(byte[] bytes)
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

    void LaunchHelper(string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath) ?? "";
        var helper = Path.Combine(dir, HelperFileName);
        var stagedHelper = helper + ".update";
        if (File.Exists(stagedHelper))
        {
            try
            {
                if (File.Exists(helper)) File.Delete(helper);
                File.Move(stagedHelper, helper);
            }
            catch { /* swap helper after SB exit if locked */ }
        }

        if (!File.Exists(helper))
        {
            CPH.LogWarn("[FluentConfig DllCheck] UpdaterHelper.exe not found; swap manually after exit.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = helper,
                Arguments = "\"" + targetPath + "\" \"Streamer.bot.exe\" \"\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = dir,
            });
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[FluentConfig DllCheck] helper launch failed: " + ex.Message);
        }
    }

    static string StripV(string version)
    {
        if (string.IsNullOrEmpty(version)) return "";
        version = version.Trim();
        // Match GitHubUpdater: strip leading v only; IsNewer/Normalize handles -beta metadata.
        var plus = version.IndexOf('+');
        if (plus >= 0) version = version.Substring(0, plus);
        if (version.Length > 0 && (version[0] == 'v' || version[0] == 'V'))
            version = version.Substring(1);
        return version;
    }

    static string Normalize(string v) => Regex.Replace(v ?? "", @"[^0-9.]+", "");

    static bool IsNewer(string candidate, string current)
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
