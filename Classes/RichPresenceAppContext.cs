using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace RichPresenceApp.Classes
{
    public class RichPresenceAppContext : ApplicationContext, IDisposable
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

        // Icon cache
        private static Icon? _cachedIcon = null;

        // Constructor with parameters
        public RichPresenceAppContext(HttpServer httpServer, GameStateMonitor gameStateMonitor, DiscordManager discordManager)
        {
            try
            {
                // Store references
                _httpServer = httpServer ?? throw new ArgumentNullException(nameof(httpServer));
                _gameStateMonitor = gameStateMonitor ?? throw new ArgumentNullException(nameof(gameStateMonitor));
                _discordManager = discordManager ?? throw new ArgumentNullException(nameof(discordManager));

                // Create system tray icon with error handling
                try
                {
                    _trayIcon = new NotifyIcon
                    {
                        Icon = LoadApplicationIcon(),
                        ContextMenuStrip = CreateContextMenu(),
                        Visible = true,
                        Text = Program.AppName
                    };

                    // Add double-click handler to show main form
                    _trayIcon.DoubleClick += (sender, e) => ShowMainForm();

                    // Show balloon tip to indicate the app is running
                    ShowStartupNotification();
                }
                catch (Exception ex)
                {
                    // If icon loading fails, use a default system icon
                    ConsoleManager.LogError("Error creating tray icon", ex);

                    _trayIcon = new NotifyIcon
                    {
                        Icon = SystemIcons.Application,
                        ContextMenuStrip = CreateContextMenu(),
                        Visible = true,
                        Text = Program.AppName
                    };

                    // Add double-click handler to show main form
                    _trayIcon.DoubleClick += (sender, e) => ShowMainForm();

                    // Show balloon tip to indicate the app is running
                    ShowStartupNotification();
                }

                ConsoleManager.LogImportant("Application context initialized.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error initializing application context", ex);

                // Create a minimal tray icon with default system icon
                _trayIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
                    ContextMenuStrip = CreateContextMenu(),
                    Visible = true,
                    Text = Program.AppName
                };

                throw; // Rethrow to be caught by the main exception handler
            }
        }

        // Show startup notification - optimized
        private void ShowStartupNotification()
        {
            try
            {
                _trayIcon.BalloonTipTitle = Program.AppName;
                _trayIcon.BalloonTipText = "Application is running in the system tray. Double-click the icon to open settings.";
                _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                _trayIcon.ShowBalloonTip(2000); // Reduced to 2 seconds
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error showing startup notification", ex);
            }
        }

        // Load application icon with caching
        private Icon LoadApplicationIcon()
        {
            try
            {
                // Return cached icon if available
                if (_cachedIcon != null)
                {
                    return _cachedIcon;
                }

                // Try to load icon from file - first check for favicon.ico
                string faviconPath = Path.Combine(Program.AppPath, "favicon.ico");
                if (File.Exists(faviconPath))
                {
                    try
                    {
                        _cachedIcon = new Icon(faviconPath);
                        return _cachedIcon;
                    }
                    catch
                    {
                        // Continue to next method
                    }
                }

                // Try to load icon from embedded resource
                try
                {
                    Icon? embeddedIcon = ExtractIconFromExecutable();
                    if (embeddedIcon != null)
                    {
                        _cachedIcon = embeddedIcon;
                        return _cachedIcon;
                    }
                }
                catch
                {
                    // Continue to next method
                }

                // Fallback to default application icon
                return SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        // Extract icon from executable
        private Icon? ExtractIconFromExecutable()
        {
            try
            {
                // Try to get the current process's main module filename
                string? processPath = Process.GetCurrentProcess().MainModule?.FileName;

                // If we got a valid process path, try to extract the icon
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    return Icon.ExtractAssociatedIcon(processPath);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Create context menu
        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            try
            {
                // Add menu items
                menu.Items.Add("Settings", null, OnSettingsClick);
                menu.Items.Add("Toggle Debug Mode", null, OnToggleDebugModeClick);
                menu.Items.Add("-");
                menu.Items.Add("Exit", null, OnExitClick);
            }
            catch
            {
                // Add minimal exit option
                menu.Items.Add("Exit", null, OnExitClick);
            }

            return menu;
        }

        // Show main form
        public void ShowMainForm()
        {
            try
            {
                // Create main form if it doesn't exist
                if (_mainForm == null || _mainForm.IsDisposed)
                {
                    _mainForm = new ConfigForm();
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
                ConsoleManager.LogError("Error showing main form", ex);
                MessageBox.Show($"Error showing settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ConsoleManager.LogError("Error showing settings form", ex);
                MessageBox.Show($"Error showing settings form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle toggle debug mode click - optimized
        private void OnToggleDebugModeClick(object? sender, EventArgs e)
        {
            try
            {
                // Toggle debug mode
                ConsoleManager.ToggleDebugMode();

                // Update menu item text to reflect current state
                if (sender is ToolStripMenuItem menuItem)
                {
                    menuItem.Text = ConsoleManager.IsDebugMode() ? "Disable Debug Mode" : "Enable Debug Mode";
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error toggling debug mode", ex);
                MessageBox.Show($"Error toggling debug mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle exit click
        private void OnExitClick(object? sender, EventArgs e)
        {
            try
            {
                ShutdownApplication();
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error exiting application", ex);

                // Force exit
                Environment.Exit(1);
            }
        }

        // Shutdown application
        private void ShutdownApplication()
        {
            try
            {
                // Hide tray icon
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                }

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
                ConsoleManager.LogError("Error during shutdown", ex);
                Environment.Exit(1);
            }
        }

        // Dispose resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Dispose tray icon
                    _trayIcon?.Dispose();

                    // Dispose main form
                    _mainForm?.Dispose();

                    // Dispose cached icon
                    if (_cachedIcon != null)
                    {
                        _cachedIcon.Dispose();
                        _cachedIcon = null;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleManager.LogError("Error disposing resources", ex);
                }
            }

            base.Dispose(disposing);
        }

        // Explicit IDisposable implementation
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

