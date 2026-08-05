using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using FluentConfig;
using FluentConfig.Protocol;
using Moq;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.SmokeHost
{
    /// <summary>
    /// STA harness: opens FluentConfigHostWindow with Release embedded HTML (or Debug Vite),
    /// waits briefly, injects a save RPC, writes PerfTrace + settings evidence to a log file.
    /// Usage: FluentConfig.SmokeHost.exe [logPath]
    /// Env: STREAMER_BOT_PATH (optional) for WebView2 assembly resolve.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var logPath = args.Length > 0
                ? args[0]
                : Path.Combine(Path.GetTempPath(), "FluentConfig_Phase2_Smoke.log");

            var lines = new ConcurrentBag<string>();
            void Log(string msg)
            {
                var line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg;
                lines.Add(line);
                Console.WriteLine(line);
            }

            try
            {
                // FluentConfigUi.Create initializes WebView2AssemblyResolve.
                EnsureStreamerBotAssemblyResolve(Log);
                Log("IsBundleEmbedded=" + EmbeddedHtml.IsBundleEmbedded);
                Log("EmbeddedHtml.Content length=" + (EmbeddedHtml.Content?.Length ?? 0));

                var store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
                var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Loose);
                mock.Setup(c => c.GetGlobalVar<string>(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns((string name, bool persisted) =>
                    {
                        store.TryGetValue(name, out var v);
                        return v;
                    });
                mock.Setup(c => c.SetGlobalVar(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                    .Callback((string name, object value, bool persisted) =>
                    {
                        store[name] = value?.ToString() ?? "";
                        Log("SetGlobalVar " + name + " len=" + (store[name]?.Length ?? 0));
                    });

                FluentConfigApp.SetLogCallback(Log);

                if (Application.Current == null)
                    new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                var ui = FluentConfigUi.Create(mock.Object, "Phase2 SmokeHost", "0.2.0");
                ui.Section("General", "general", s => s
                    .Intro("SmokeHost automated window.")
                    .Toggle("Enabled", "smoke_enabled").Default(true)
                    .Textbox("Display name", "smoke_name").Default("before")
                    .Dropdown("Reward", "smoke_reward_display")
                        .WithPairValue("smoke_reward_id")
                        .Options(new[] { ("rew_1", "Alpha"), ("rew_2", "Beta") })
                        .Refresh(() => new[] { ("rew_1", "Alpha"), ("rew_2", "Beta"), ("rew_3", "Gamma") })
                        .DefaultByValue("rew_2")
                    .PillInput("Watch", "smoke_pills")
                        .WithItemTemplate(item => item.Toggle("Active", "{name}_active").Default(true))
                );

                var coldSw = System.Diagnostics.Stopwatch.StartNew();
                ui.Show();
                coldSw.Stop();
                Log("[PerfTrace] SmokeHost.Show.ColdReturn: " + coldSw.ElapsedMilliseconds + "ms (window shown; WebView nav async)");

                var session = ui.Session;

                // After a short settle, inject save as if the Svelte UI posted it.
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    try
                    {
                        var values = new JObject
                        {
                            ["smoke_enabled"] = false,
                            ["smoke_name"] = "after-smokehost-edit",
                            ["smoke_reward_display"] = "Beta",
                            ["smoke_reward_id"] = "rew_2",
                            ["smoke_pills"] = new JArray("Alpha"),
                        };
                        var req = ProtocolJson.Serialize(WireMessage.Request(99, RpcMethods.Save, new SaveParams { Values = values }));
                        session.HandleWebMessage(req);
                        var settings = session.GetSettingsForTests();
                        Log("SettingsAfterSave=" + settings);
                        File.WriteAllText(Path.ChangeExtension(logPath, ".settings.json"), settings.ToString(Newtonsoft.Json.Formatting.Indented));
                    }
                    catch (Exception ex)
                    {
                        Log("Save inject failed: " + ex);
                    }

                    var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    closeTimer.Tick += (s2, e2) =>
                    {
                        closeTimer.Stop();
                        Log("Closing smoke window.");
                        Application.Current.Shutdown(0);
                    };
                    closeTimer.Start();
                };
                timer.Start();

                Application.Current.Run();
                File.WriteAllLines(logPath, lines);
                Console.WriteLine("Wrote " + logPath);
                return 0;
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
                try { File.WriteAllLines(logPath, lines); } catch { /* ignore */ }
                return 1;
            }
        }

        private static void EnsureStreamerBotAssemblyResolve(Action<string> log)
        {
            var sb = Environment.GetEnvironmentVariable("STREAMER_BOT_PATH");
            if (string.IsNullOrWhiteSpace(sb))
            {
                var desktop = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Desktop",
                    "Streamer.bot-x64-1.0.4");
                var winget = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WinGet", "Packages",
                    "streamerbot.streamerbot_Microsoft.Winget.Source_8wekyb3d8bbwe");
                if (Directory.Exists(desktop)) sb = desktop;
                else if (Directory.Exists(winget)) sb = winget;
            }
            if (string.IsNullOrWhiteSpace(sb) || !Directory.Exists(sb)) return;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                if (name == null) return null;
                var path = Path.Combine(sb, name + ".dll");
                if (File.Exists(path))
                {
                    log("AssemblyResolve " + name + " -> " + path);
                    return Assembly.LoadFrom(path);
                }
                return null;
            };
        }
    }
}
