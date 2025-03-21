using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using RichPresenceApp.Classes;

namespace RichPresenceApp
{
    internal static class Program
    {
        // Flag to track if CS2 is running
        private static bool _isGameRunning = false;

        // Public property with getter and setter
        public static bool IsGameRunning
        {
            get => _isGameRunning;
            set => _isGameRunning = value;
        }

        // Application path
        public static string AppPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        // Application name
        public static string AppName => "CS2 Discord RPC";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Set console title
                Console.Title = AppName;

                // Initialize console manager
                ConsoleManager.Initialize();

                // Load configuration FIRST
                Config.Load();

                // Setup application (GSI config)
                ApplicationSetup.Setup();
                ConsoleManager.WriteLine("Application setup completed successfully.", ConsoleColor.Green, true);

                // Initialize Discord RPC
                var discordManager = new DiscordManager();
                discordManager.Initialize();

                // Create game state monitor
                var gameStateMonitor = new GameStateMonitor(discordManager);

                // Create HTTP server
                var httpServer = new HttpServer(gameStateMonitor);
                httpServer.Start();

                // Start game process monitor
                gameStateMonitor.StartGameProcessMonitor();

                // Create system tray
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Create application context with the required parameters
                var appContext = new RichPresenceAppContext(httpServer, gameStateMonitor, discordManager);

                // Register application exit event
                Application.ApplicationExit += (sender, e) =>
                {
                    // Shutdown console manager
                    ConsoleManager.Shutdown();
                };

                // Run application
                Application.Run(appContext);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Unhandled exception: {ex.Message}", ConsoleColor.Red, true);
                ConsoleManager.WriteLine(ex.StackTrace ?? "No stack trace available", ConsoleColor.Red, true);

                // Show error message
                MessageBox.Show($"An unhandled exception occurred: {ex.Message}\n\nThe application will now exit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Shutdown console manager
                ConsoleManager.Shutdown();

                // Exit application
                Environment.Exit(1);
            }
        }
    }
}