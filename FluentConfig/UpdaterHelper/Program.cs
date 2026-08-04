using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FluentConfig.UpdaterHelper
{
    /// <summary>
    /// Detached helper: wait for Streamer.bot (or named process) to exit, swap staged .update into place, relaunch.
    /// Args: &lt;targetPath&gt; [processName] [relaunchExePath]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.Error.WriteLine("Usage: FluentConfig.UpdaterHelper <targetPath> [processName] [relaunchExePath]");
                return 1;
            }

            var targetPath = args[0];
            var processName = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1] : "Streamer.bot.exe";
            var relaunchPath = args.Length > 2 ? args[2] : null;
            var stagedPath = targetPath + ".update";

            if (!File.Exists(stagedPath))
            {
                Console.Error.WriteLine("Staged file not found: " + stagedPath);
                return 2;
            }

            var waitName = Path.GetFileNameWithoutExtension(processName);
            var deadline = DateTime.UtcNow.AddMinutes(10);
            while (DateTime.UtcNow < deadline)
            {
                Process[] procs;
                try { procs = Process.GetProcessesByName(waitName); }
                catch { break; }

                if (procs.Length == 0)
                    break;

                foreach (var p in procs)
                    p.Dispose();

                Thread.Sleep(500);
            }

            // Brief settle for file locks to release.
            Thread.Sleep(1000);

            try
            {
                if (File.Exists(targetPath))
                {
                    var backup = targetPath + ".bak";
                    try
                    {
                        if (File.Exists(backup)) File.Delete(backup);
                        File.Move(targetPath, backup);
                    }
                    catch
                    {
                        // Fall through and try overwrite.
                    }
                }

                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                File.Move(stagedPath, targetPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Swap failed: " + ex.Message);
                return 3;
            }

            if (!string.IsNullOrWhiteSpace(relaunchPath) && File.Exists(relaunchPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = relaunchPath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(relaunchPath) ?? Environment.CurrentDirectory,
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Relaunch failed: " + ex.Message);
                    return 4;
                }
            }

            return 0;
        }
    }
}
