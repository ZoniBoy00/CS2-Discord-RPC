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

        // Verbose logging flag
        private static bool _verboseLogging = false;

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

        // Buffer for log messages to reduce disk I/O
        private static readonly StringBuilder _logBuffer = new StringBuilder(8192);

        // Timer for flushing log buffer
        private static System.Threading.Timer? _flushTimer;

        // Constants
        private const int FLUSH_INTERVAL_MS = 10000; // 10 seconds - increased to reduce I/O

        // Initialize console manager
        public static void Initialize(bool verboseLogging = false)
        {
            try
            {
                _verboseLogging = verboseLogging;

                // Create log directory if it doesn't exist
                string? directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open log file for writing
                OpenLogFile();

                // Start flush timer with longer interval
                _flushTimer = new System.Threading.Timer(FlushLogBuffer, null, FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS);

                // Write initial log entry (minimal)
                LogImportant($"Log started at {DateTime.Now}");
                LogImportant($"Application version: 1.1.0");

                if (_verboseLogging)
                {
                    LogImportant($"OS version: {Environment.OSVersion}");
                    LogImportant($".NET version: {Environment.Version}");
                }
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

                    // Create new writer with auto-flush disabled for better performance
                    _logWriter = new StreamWriter(LogFilePath, false, Encoding.UTF8) { AutoFlush = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening log file: {ex.Message}");
            }
        }

        // Flush log buffer on timer
        private static void FlushLogBuffer(object? state)
        {
            try
            {
                lock (_logLock)
                {
                    if (_logBuffer.Length > 0 && _logWriter != null)
                    {
                        _logWriter.Write(_logBuffer.ToString());
                        _logWriter.Flush();
                        _logBuffer.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing log buffer: {ex.Message}");

                // Try to reopen log file
                OpenLogFile();
            }
        }

        // Write line to console and log file - optimized to reduce string allocations
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White, bool forceOutput = false)
        {
            try
            {
                // Skip debug messages if debug mode is disabled
                if (!_debugMode && !forceOutput && IsDebugMessage(message))
                {
                    // Still log to file but don't show in console
                    if (_verboseLogging)
                    {
                        AppendToLogBuffer($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                    }
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

                // Write to log buffer
                AppendToLogBuffer(formattedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to console: {ex.Message}");
            }
        }

        // Append message to log buffer
        private static void AppendToLogBuffer(string message)
        {
            try
            {
                lock (_logLock)
                {
                    // Append to buffer
                    _logBuffer.AppendLine(message);

                    // If buffer exceeds threshold, flush immediately
                    if (_logBuffer.Length > 8000)
                    {
                        FlushLogBuffer(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't use WriteLine here to avoid infinite recursion
                Console.WriteLine($"Error appending to log buffer: {ex.Message}");
            }
        }

        // Optimize IsDebugMessage method to be more selective
        private static bool IsDebugMessage(string message)
        {
            // Check if message contains debug keywords - more selective filtering
            return message.Contains("Received game state JSON") ||
                   message.Contains("Presence updated:") ||
                   message.Contains("Sent frame:") ||
                   message.Contains("Received frame:") ||
                   message.Contains("Received header:") ||
                   message.Contains("Connection to Discord established") ||
                   message.Contains("[DEBUG]");
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

        // Add method to log only important events
        public static void LogImportant(string message, ConsoleColor color = ConsoleColor.Green)
        {
            WriteLine(message, color, true);
        }

        // Add method to log debug information
        public static void LogDebug(string message, ConsoleColor color = ConsoleColor.Cyan)
        {
            if (_debugMode)
            {
                WriteLine($"[DEBUG] {message}", color, false);
            }
            else if (_verboseLogging)
            {
                // Still log to file but don't show in console
                AppendToLogBuffer($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}");
            }
        }

        // Add method to log errors
        public static void LogError(string message, Exception? ex = null)
        {
            string errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
            WriteLine(errorMessage, ConsoleColor.Red, true);

            if (ex != null && ex.StackTrace != null && _verboseLogging)
            {
                // Log stack trace to file only
                AppendToLogBuffer($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stack trace: {ex.StackTrace}");
            }
        }

        // Close log file
        public static void Shutdown()
        {
            try
            {
                // Stop flush timer
                if (_flushTimer != null)
                {
                    _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _flushTimer.Dispose();
                    _flushTimer = null;
                }

                // Flush buffer one last time
                FlushLogBuffer(null);

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

