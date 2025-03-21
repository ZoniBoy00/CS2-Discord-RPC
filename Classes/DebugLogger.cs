using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace RichPresenceApp.Classes
{
    public static class DebugLogger
    {
        private static readonly string DebugLogPath = Path.Combine(Program.AppPath, "debug.log");
        private static StreamWriter? logWriter;
        
        public static void Initialize()
        {
            try
            {
                // Create or overwrite debug log file
                logWriter = new StreamWriter(DebugLogPath, false, Encoding.UTF8);
                logWriter.AutoFlush = true;
                
                // Log system information
                LogSystemInfo();
                
                Log("Debug logger initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing debug logger: {ex.Message}");
            }
        }
        
        public static void Log(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}";
                
                logWriter?.WriteLine(logMessage);
                
                // Also write to console in debug mode
                Debug.WriteLine(logMessage);
            }
            catch
            {
                // Ignore errors in logging
            }
        }
        
        public static void LogException(Exception ex, string context = "")
        {
            try
            {
                Log($"EXCEPTION in {context}: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Log($"Inner exception: {ex.InnerException.Message}");
                    Log($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
            }
            catch
            {
                // Ignore errors in logging
            }
        }
        
        private static void LogSystemInfo()
        {
            try
            {
                Log("=== SYSTEM INFORMATION ===");
                Log($"OS: {Environment.OSVersion}");
                Log($".NET Version: {Environment.Version}");
                Log($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                Log($"64-bit Process: {Environment.Is64BitProcess}");
                Log($"Processor Count: {Environment.ProcessorCount}");
                Log($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
                Log($"Current Directory: {Environment.CurrentDirectory}");
                Log($"App Path: {Program.AppPath}");
                Log("=========================");
            }
            catch (Exception ex)
            {
                Log($"Error logging system info: {ex.Message}");
            }
        }
        
        public static void Close()
        {
            try
            {
                logWriter?.Close();
                logWriter?.Dispose();
                logWriter = null;
            }
            catch
            {
                // Ignore errors when closing
            }
        }
    }
}