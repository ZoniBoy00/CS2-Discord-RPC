using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RichPresenceApp.Classes
{
    public static class SystemTray
    {
        // Create system tray icon
        public static NotifyIcon CreateTrayIcon(HttpServer httpServer, GameStateMonitor gameStateMonitor, DiscordManager discordManager)
        {
            try
            {
                // Create notify icon
                var trayIcon = new NotifyIcon
                {
                    Icon = LoadApplicationIcon(),
                    Text = Program.AppName,
                    Visible = true,
                    ContextMenuStrip = CreateContextMenu(httpServer, gameStateMonitor, discordManager)
                };

                ConsoleManager.WriteLine("System tray icon created successfully.", ConsoleColor.Green, true);

                return trayIcon;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error creating system tray icon: {ex.Message}", ConsoleColor.Red, true);
                throw;
            }
        }

        // Load application icon
        private static Icon LoadApplicationIcon()
        {
            try
            {
                // Try to load icon from file - first check for favicon.ico
                string faviconPath = Path.Combine(Program.AppPath, "favicon.ico");
                if (File.Exists(faviconPath))
                {
                    ConsoleManager.WriteLine("Loading icon from favicon.ico", ConsoleColor.Green, true);
                    return new Icon(faviconPath);
                }

                // Try to load icon from embedded resource
                Icon? embeddedIcon = ExtractIconFromExecutable();
                if (embeddedIcon != null)
                {
                    ConsoleManager.WriteLine("Loading icon from embedded resource", ConsoleColor.Green, true);
                    return embeddedIcon;
                }

                // Try to load icon from file - fallback to app.ico
                string appIconPath = Path.Combine(Program.AppPath, "app.ico");
                if (File.Exists(appIconPath))
                {
                    ConsoleManager.WriteLine("Loading icon from app.ico", ConsoleColor.Green, true);
                    return new Icon(appIconPath);
                }

                // Fallback to default application icon
                ConsoleManager.WriteLine("Using default application icon", ConsoleColor.Yellow, true);
                return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location) ?? SystemIcons.Application;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error loading application icon: {ex.Message}", ConsoleColor.Red, true);
                return SystemIcons.Application;
            }
        }

        // Extract icon from executable
        private static Icon? ExtractIconFromExecutable()
        {
            try
            {
                // Get executing assembly
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Get assembly name
                string assemblyName = assembly.GetName().Name ?? "RichPresenceApp";

                // Try to find icon resource
                string[] resourceNames = assembly.GetManifestResourceNames();
                string? iconResourceName = Array.Find(resourceNames, name => name.Contains(".ico"));

                // If icon resource found, load it
                if (!string.IsNullOrEmpty(iconResourceName))
                {
                    using Stream? stream = assembly.GetManifestResourceStream(iconResourceName);
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }

                // Try to extract icon from executable
                string executablePath = Assembly.GetExecutingAssembly().Location;
                return Icon.ExtractAssociatedIcon(executablePath);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error extracting icon from executable: {ex.Message}", ConsoleColor.Red, true);
                return null;
            }
        }

        // Create context menu
        private static ContextMenuStrip CreateContextMenu(HttpServer httpServer, GameStateMonitor gameStateMonitor, DiscordManager discordManager)
        {
            try
            {
                // Create context menu
                var contextMenu = new ContextMenuStrip();

                // Add menu items
                contextMenu.Items.Add("Settings", null, (sender, e) => ShowSettingsForm());
                contextMenu.Items.Add("Toggle Debug Mode", null, (sender, e) => ConsoleManager.ToggleDebugMode());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Exit", null, (sender, e) => ExitApplication(httpServer, gameStateMonitor, discordManager));

                return contextMenu;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error creating context menu: {ex.Message}", ConsoleColor.Red, true);
                throw;
            }
        }

        // Show settings form
        private static void ShowSettingsForm()
        {
            try
            {
                // Create settings form
                using var settingsForm = new ConfigForm();

                // Show settings form
                settingsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error showing settings form: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error showing settings form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Exit application
        private static void ExitApplication(HttpServer httpServer, GameStateMonitor gameStateMonitor, DiscordManager discordManager)
        {
            try
            {
                // Stop HTTP server
                httpServer.Stop();

                // Stop game process monitor
                gameStateMonitor.StopGameProcessMonitor();

                // Shutdown Discord RPC
                discordManager.Shutdown();

                // Exit application
                Application.Exit();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error exiting application: {ex.Message}", ConsoleColor.Red, true);

                // Force exit
                Environment.Exit(1);
            }
        }
    }
}