using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FluentConfig
{
    /// <summary>
    /// Defensive fallback when the compile-time WebView2 version does not match the copy
    /// already loaded by Streamer.bot (or present in the host base directory).
    /// Inspired by TawmaeUI's AssemblyResolver — prefer an already-loaded same-simple-name
    /// assembly, else load from the host process directory, ignoring exact version.
    /// </summary>
    internal static class WebView2AssemblyResolve
    {
        private static int _initialized;

        internal static void EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
        }

        private static Assembly OnResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requested;
            try { requested = new AssemblyName(args.Name); }
            catch { return null; }

            var name = requested.Name;
            if (string.IsNullOrEmpty(name) ||
                name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!name.StartsWith("Microsoft.Web.WebView2", StringComparison.OrdinalIgnoreCase))
                return null;

            // Prefer whatever is already loaded into this AppDomain (Streamer.bot's copy).
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                {
                    try { return string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
            if (loaded != null)
            {
                LogFallback(name, requested.Version, loaded.GetName().Version, "already-loaded");
                return loaded;
            }

            var baseDir = ResolveHostBaseDirectory();
            if (string.IsNullOrEmpty(baseDir))
                return null;

            var path = Path.Combine(baseDir, name + ".dll");
            if (!File.Exists(path))
                return null;

            try
            {
                var asm = Assembly.LoadFrom(path);
                LogFallback(name, requested.Version, asm.GetName().Version, "base-directory:" + path);
                return asm;
            }
            catch (Exception ex)
            {
                FluentConfigApp.LogInternal(
                    $"[FluentConfig] WebView2 AssemblyResolve failed for '{name}': {ex.Message}");
                return null;
            }
        }

        private static void LogFallback(string name, Version requested, Version actual, string source)
        {
            FluentConfigApp.LogInternal(
                $"[FluentConfig] WebView2 AssemblyResolve fallback: '{name}' " +
                $"requested v{requested} → using v{actual} ({source}). " +
                "Compile against Streamer.bot's bundled WebView2 to avoid this.");
        }

        private static string ResolveHostBaseDirectory()
        {
            try
            {
                var entry = Assembly.GetEntryAssembly();
                if (entry != null && !string.IsNullOrEmpty(entry.Location))
                    return Path.GetDirectoryName(entry.Location);
            }
            catch { /* ignore */ }

            try
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                {
                    var fileName = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(fileName))
                        return Path.GetDirectoryName(fileName);
                }
            }
            catch { /* ignore */ }

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir))
                    return baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch { /* ignore */ }

            return null;
        }
    }
}
