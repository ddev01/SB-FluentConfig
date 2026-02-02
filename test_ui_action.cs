// Streamer.bot C# Action Code
// Copy and paste this into a new C# action in Streamer.bot

using Sbui;
using System;
using System.IO;
using System.Windows;

public class CPHInline
{
    private static string GetLogPath()
    {
        // Get Streamer.bot directory (where the action is running from)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string logDir = Path.Combine(baseDir, "logs");
        
        // Create logs directory if it doesn't exist
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        
        return Path.Combine(logDir, "ui.txt");
    }
    
    private static void WriteLog(string message)
    {
        try
        {
            string logPath = GetLogPath();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] {message}";
            
            File.AppendAllText(logPath, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // If file logging fails, write to a backup location
            try
            {
                string backupPath = Path.Combine(Path.GetTempPath(), "streamerbot_ui_log.txt");
                File.AppendAllText(backupPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message} - Error: {ex.Message}{Environment.NewLine}");
            }
            catch { }
        }
    }
    
    public bool Execute()
    {
        try
        {
            WriteLog("=== Sbui Test Action Started ===");
            WriteLog($"Application.Current is null: {Application.Current == null}");
            
            // Set up logging callback to write to file
            Sbui.Sbui.SetLogCallback((msg) => WriteLog($"[Sbui] {msg}"));
            
            // Skip if UI is already open (TawmaeUI pattern)
            if (Sbui.Sbui.AlreadyOpened("Settings UI Test", "1.0"))
            {
                WriteLog("=== Action skipped - UI already open ===");
                return true;
            }
            
            WriteLog("Creating Sbui instance...");
            
            var ui = new Sbui.Sbui(CPH, "Settings UI Test", true);
            ui.AddTitle("General Settings", "General");
            ui.AddDescription("You can enable additional requirements for this feature below. For example, you might want users to meet a specific condition or have a particular attribute.", "General");
            ui.AddToggleSwitch("Enable Requirement", "Turn this on to require a custom condition for use below.", "General", "custom_requirement_enabled", false);
            ui.AddTextbox("Requirement Name", "The name of your requirement (e.g. 'Level', 'AccessKey')", "General", "custom_requirement_name", "", false);
            ui.AddSlider("Required Value", "Specify the minimum value needed to meet the requirement.", "General", "custom_required_value", 1, 1000000, 250);
            WriteLog("Sbui instance created, calling ShowUI...");
            
            ui.ShowUI();
            WriteLog("ShowUI() completed");
            
            WriteLog("=== Action completed - UI should now be visible ===");
        }
        catch (Exception ex)
        {
            WriteLog($"=== ERROR OCCURRED ===");
            WriteLog($"Error Type: {ex.GetType().Name}");
            WriteLog($"Error Message: {ex.Message}");
            WriteLog($"Stack Trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                WriteLog($"Inner Exception: {ex.InnerException.Message}");
                WriteLog($"Inner Stack: {ex.InnerException.StackTrace}");
            }
        }
        
        return true;
    }
}

