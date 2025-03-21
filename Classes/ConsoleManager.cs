using System;
using System.IO;
using System.Text;
using System.Threading;

namespace RichPresenceApp.Classes
{
    public static class ConsoleManager
    {
        // Debug mode flag
        private static bool _debugMode = false;

        // Log file path
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CS2RPC",
            "log.txt"
        );

        // File stream for log file
        private static StreamWriter? _logWriter;

        // Lock object for thread safety
        private static readonly object _logLock = new object();

        // Initialize console manager
        public static void Initialize()
        {
            try
            {
                // Create log directory if it doesn't exist
                string? directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open log file for writing
                OpenLogFile();

                // Write initial log entry
                WriteLine($"Log started at {DateTime.Now}", ConsoleColor.Green, true);
                WriteLine($"Application version: 1.0.0", ConsoleColor.Green, true);
                WriteLine($"OS version: {Environment.OSVersion}", ConsoleColor.Green, true);
                WriteLine($".NET version: {Environment.Version}", ConsoleColor.Green, true);
                WriteLine(new string('-', 50), ConsoleColor.Gray, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing console manager: {ex.Message}");
            }
        }

        // Open log file
        private static void OpenLogFile()
        {
            try
            {
                lock (_logLock)
                {
                    // Close existing writer if open
                    _logWriter?.Close();
                    _logWriter?.Dispose();

                    // Create new writer with auto-flush
                    _logWriter = new StreamWriter(LogFilePath, false) { AutoFlush = true };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening log file: {ex.Message}");
            }
        }

        // Write line to console and log file
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White, bool forceOutput = false)
        {
            try
            {
                // Skip debug messages if debug mode is disabled
                if (!_debugMode && !forceOutput && IsDebugMessage(message))
                {
                    // Still log to file but don't show in console
                    AppendToLogFile($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}");
                    return;
                }

                // Get current time
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Format message
                string formattedMessage = $"[{timestamp}] {message}";

                // Write to console
                Console.ForegroundColor = color;
                Console.WriteLine(formattedMessage);
                Console.ResetColor();

                // Write to log file
                AppendToLogFile(formattedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to console: {ex.Message}");
            }
        }

        // Append message to log file
        private static void AppendToLogFile(string message)
        {
            try
            {
                lock (_logLock)
                {
                    // Check if log writer is open
                    if (_logWriter == null)
                    {
                        // Try to reopen log file
                        OpenLogFile();

                        // Check if reopening succeeded
                        if (_logWriter == null)
                        {
                            Console.WriteLine("Failed to open log file for writing");
                            return;
                        }
                    }

                    // Write message to log file
                    _logWriter.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                // Don't use WriteLine here to avoid infinite recursion
                Console.WriteLine($"Error writing to log file: {ex.Message}");

                // Try to reopen log file on next write
                lock (_logLock)
                {
                    _logWriter?.Close();
                    _logWriter?.Dispose();
                    _logWriter = null;
                }
            }
        }

        // Check if message is a debug message
        private static bool IsDebugMessage(string message)
        {
            // Check if message contains debug keywords
            return message.Contains("Received game state JSON") ||
                   message.Contains("Updated presence") ||
                   message.Contains("Manually extracted");
        }

        // Toggle debug mode
        public static void ToggleDebugMode()
        {
            _debugMode = !_debugMode;
            WriteLine($"Debug mode {(_debugMode ? "enabled" : "disabled")}", ConsoleColor.Yellow, true);
        }

        // Get debug mode status
        public static bool IsDebugMode()
        {
            return _debugMode;
        }

        // Close log file
        public static void Shutdown()
        {
            try
            {
                lock (_logLock)
                {
                    // Close log writer
                    _logWriter?.Close();
                    _logWriter?.Dispose();
                    _logWriter = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing log file: {ex.Message}");
            }
        }
    }
}