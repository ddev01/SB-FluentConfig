using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FluentConfig.Updater
{
    /// <summary>
    /// Spawns the detached <c>FluentConfig.UpdaterHelper.exe</c> which waits for Streamer.bot to exit,
    /// swaps the staged <c>*.update</c> file into place, and relaunches.
    /// Helper project: <c>FluentConfig/UpdaterHelper/</c> (sibling of Host).
    /// </summary>
    public static class UpdateHelperLauncher
    {
        /// <summary>
        /// Launch the swap helper. Looks for UpdaterHelper next to FluentConfig.dll, then in a
        /// sibling UpdaterHelper output folder (dev builds).
        /// </summary>
        /// <param name="targetPath">Live DLL path that has a sibling <c>.update</c> staged file.</param>
        /// <param name="processName">Process to wait for (default Streamer.bot.exe).</param>
        public static bool LaunchSwapAndRelaunch(string targetPath, string processName = "Streamer.bot.exe")
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("targetPath is required.", nameof(targetPath));

            var helper = FindHelperExe();
            if (helper == null || !File.Exists(helper))
            {
                FluentConfigApp.LogInternal(
                    "[FluentConfig] UpdaterHelper.exe not found. Stage succeeded; swap manually after closing Streamer.bot. " +
                    "Expected next to FluentConfig.dll or at FluentConfig/UpdaterHelper/bin.");
                return false;
            }

            var exeToRelaunch = FindHostExe(processName);
            var args = $"\"{targetPath}\" \"{processName}\" \"{exeToRelaunch ?? ""}\"";

            var psi = new ProcessStartInfo
            {
                FileName = helper,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(helper) ?? Environment.CurrentDirectory,
            };
            Process.Start(psi);
            return true;
        }

        private static string FindHelperExe()
        {
            var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var nextToDll = Path.Combine(asmDir, "FluentConfig.UpdaterHelper.exe");
            if (File.Exists(nextToDll)) return nextToDll;

            // Dev: Host/bin/Debug/net481 → ../../UpdaterHelper/bin/Debug/net481
            var candidate = Path.GetFullPath(Path.Combine(asmDir, "..", "..", "..", "..", "UpdaterHelper", "bin", "Debug", "net481", "FluentConfig.UpdaterHelper.exe"));
            if (File.Exists(candidate)) return candidate;
            candidate = Path.GetFullPath(Path.Combine(asmDir, "..", "..", "..", "..", "UpdaterHelper", "bin", "Release", "net481", "FluentConfig.UpdaterHelper.exe"));
            if (File.Exists(candidate)) return candidate;
            return null;
        }

        private static string FindHostExe(string processName)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(processName);
                var procs = Process.GetProcessesByName(name);
                if (procs.Length > 0)
                {
                    try { return procs[0].MainModule?.FileName; }
                    catch { /* access denied on MainModule is fine */ }
                }
            }
            catch { }
            return null;
        }
    }
}
