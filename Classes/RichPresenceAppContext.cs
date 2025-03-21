using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RichPresenceApp.Classes
{
    public class RichPresenceAppContext : ApplicationContext
    {
        // System tray icon
        private readonly NotifyIcon _trayIcon;

        // HTTP server
        private readonly HttpServer? _httpServer;

        // Game state monitor
        private readonly GameStateMonitor? _gameStateMonitor;

        // Discord manager
        private readonly DiscordManager? _discordManager;

        // Main form
        private Form? _mainForm;

        // Constructor with no parameters
        public RichPresenceAppContext()
        {
            // Create system tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = LoadApplicationIcon(),
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = Program.AppName
            };

            // Add double-click handler to show main form
            _trayIcon.DoubleClick += (sender, e) => ShowMainForm();

            ConsoleManager.WriteLine("Application context initialized.", ConsoleColor.Green, true);
        }

        // Constructor with parameters
        public RichPresenceAppContext(HttpServer httpServer, GameStateMonitor gameStateMonitor, DiscordManager discordManager)
        {
            // Store references
            _httpServer = httpServer ?? throw new ArgumentNullException(nameof(httpServer));
            _gameStateMonitor = gameStateMonitor ?? throw new ArgumentNullException(nameof(gameStateMonitor));
            _discordManager = discordManager ?? throw new ArgumentNullException(nameof(discordManager));

            // Create system tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = LoadApplicationIcon(),
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = Program.AppName
            };

            // Add double-click handler to show main form
            _trayIcon.DoubleClick += (sender, e) => ShowMainForm();

            ConsoleManager.WriteLine("Application context initialized.", ConsoleColor.Green, true);
        }

        // Load application icon
        private Icon LoadApplicationIcon()
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
        private Icon? ExtractIconFromExecutable()
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
        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            // Add menu items
            menu.Items.Add("Settings", null, OnSettingsClick);
            menu.Items.Add("Toggle Debug Mode", null, OnToggleDebugModeClick);
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, OnExitClick);

            return menu;
        }

        // Show main form
        private void ShowMainForm()
        {
            try
            {
                // Create main form if it doesn't exist
                if (_mainForm == null || _mainForm.IsDisposed)
                {
                    _mainForm = new ConfigForm();
                    _mainForm.FormClosing += MainForm_FormClosing;
                }

                // Show main form
                if (!_mainForm.Visible)
                {
                    _mainForm.Show();
                }

                // Bring to front
                if (_mainForm.WindowState == FormWindowState.Minimized)
                {
                    _mainForm.WindowState = FormWindowState.Normal;
                }
                _mainForm.Activate();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error showing main form: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error showing main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle main form closing
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // Check if MinimizeToTray is enabled
                if (Config.Current != null && Config.Current.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
                {
                    // Cancel the close
                    e.Cancel = true;

                    // Hide the form
                    if (sender is Form form)
                    {
                        form.Hide();
                    }

                    // Show notification
                    _trayIcon.ShowBalloonTip(
                        2000,
                        Program.AppName,
                        "Application minimized to tray. Double-click the tray icon to open.",
                        ToolTipIcon.Info
                    );
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error handling form closing: {ex.Message}", ConsoleColor.Red, true);
            }
        }

        // Handle settings click
        private void OnSettingsClick(object? sender, EventArgs e)
        {
            try
            {
                // Show settings form
                ShowMainForm();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error showing settings form: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error showing settings form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle toggle debug mode click
        private void OnToggleDebugModeClick(object? sender, EventArgs e)
        {
            try
            {
                // Toggle debug mode
                ConsoleManager.ToggleDebugMode();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error toggling debug mode: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error toggling debug mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle exit click
        private void OnExitClick(object? sender, EventArgs e)
        {
            try
            {
                // Hide tray icon
                _trayIcon.Visible = false;

                // Stop HTTP server
                _httpServer?.Stop();

                // Stop game process monitor
                _gameStateMonitor?.StopGameProcessMonitor();

                // Shutdown Discord RPC
                _discordManager?.Shutdown();

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

        // Dispose resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose tray icon
                _trayIcon.Dispose();

                // Dispose main form
                _mainForm?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}