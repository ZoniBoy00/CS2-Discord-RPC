using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using RichPresenceApp.Classes;

namespace RichPresenceApp
{
    internal static class Program
    {
        // Flag to track if CS2 is running - explicitly initialize to false
        public static bool IsGameRunning { get; set; } = false;

        // Application path - use AppContext.BaseDirectory for single-file apps
        public static string AppPath => AppContext.BaseDirectory;

        // Application name - use constant
        public static string AppName => "CS2 Discord RPC";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set up global exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                // Initialize console manager with minimal logging
                ConsoleManager.Initialize(false);

                // Check if Discord is running
                bool discordRunning = IsDiscordRunning();
                if (!discordRunning)
                {
                    ConsoleManager.LogImportant("Discord is not running. Please start Discord and try again.");
                    MessageBox.Show(
                        "Discord is not running. Please start Discord and try again.",
                        "Discord Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Load configuration FIRST
                Config.Load();

                // Setup application (GSI config)
                ApplicationSetup.Setup();
                ConsoleManager.LogImportant("Application setup completed successfully.");

                // Initialize services
                var discordManager = new DiscordManager();
                var gameStateMonitor = new GameStateMonitor(discordManager);
                var httpServer = new HttpServer(gameStateMonitor);

                // Start services
                discordManager.Initialize();
                httpServer.Start();

                // Make sure CS2 is not running initially
                IsGameRunning = false;

                // Start game process monitor
                gameStateMonitor.StartGameProcessMonitor();

                // Create system tray
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);

                // Create application context with the required parameters
                var appContext = new RichPresenceAppContext(httpServer, gameStateMonitor, discordManager);

                // Register application exit event
                Application.ApplicationExit += (sender, e) => ConsoleManager.Shutdown();

                // Run application
                Application.Run(appContext);
            }
            catch (Exception ex)
            {
                // Log the full exception details
                LogFatalException(ex, "Unhandled exception in Main");

                // Show error message in English to avoid localization issues
                MessageBox.Show(
                    $"An unhandled exception occurred: {ex.Message}\n\nPlease check the log file for details.\n\nThe application will now exit.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                // Shutdown console manager
                try { ConsoleManager.Shutdown(); } catch { }

                // Exit application
                Environment.Exit(1);
            }
        }

        // Handle UI thread exceptions
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogFatalException(e.Exception, "Thread Exception");

            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease check the log file for details.",
                "Application Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // Handle non-UI thread exceptions
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogFatalException(ex, "Unhandled Domain Exception");

                MessageBox.Show(
                    $"A fatal error occurred: {ex.Message}\n\nPlease check the log file for details.\n\nThe application will now exit.",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                Console.WriteLine("An unknown fatal error occurred.");
            }
        }

        // Log fatal exception with detailed information - optimized
        private static void LogFatalException(Exception ex, string context)
        {
            try
            {
                // Try to log to console first
                Console.WriteLine($"{context}: {ex.Message}");

                // Try to log to file if ConsoleManager is initialized
                try
                {
                    ConsoleManager.LogError($"{context}", ex);
                }
                catch
                {
                    // Ignore errors in logging
                }

                // Create emergency log file if ConsoleManager failed
                string emergencyLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "CS2RPC_CrashLog.txt");

                using (StreamWriter writer = new StreamWriter(emergencyLogPath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] {context}: {ex.Message}");
                    writer.WriteLine($"Stack trace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        writer.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            catch
            {
                // Last resort - can't do anything more if this fails
            }
        }

        // Check if Discord is running - optimized
        private static bool IsDiscordRunning()
        {
            try
            {
                // Check for Discord processes - use static array to avoid allocations
                string[] discordProcessNames = {
                    "Discord",
                    "DiscordPTB",
                    "DiscordCanary",
                    "DiscordDevelopment"
                };

                foreach (string processName in discordProcessNames)
                {
                    if (System.Diagnostics.Process.GetProcessesByName(processName).Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error checking if Discord is running", ex);
                return false;
            }
        }
    }
}

