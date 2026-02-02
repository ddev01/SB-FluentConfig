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
            
            WriteLog("Creating Sbui instance...");
            
            // Create and show the settings UI
            var ui = new Sbui.Sbui();
            WriteLog("Sbui instance created successfully");
            
            WriteLog("Calling ShowUI()...");
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

