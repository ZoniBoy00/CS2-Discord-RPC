using System;
using System.Drawing;
using System.IO;
using System.Reflection;
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

        // Constructor with no parameters
        public RichPresenceAppContext()
        {
            try
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

                // Show balloon tip to indicate the app is running
                ShowStartupNotification();

                ConsoleManager.WriteLine("Application context initialized.", ConsoleColor.Green, true);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error in RichPresenceAppContext constructor: {ex.Message}", ConsoleColor.Red, true);

                // Create a minimal tray icon with default system icon
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
        }

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
                    ConsoleManager.WriteLine($"Error creating tray icon: {ex.Message}", ConsoleColor.Yellow, true);

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

                ConsoleManager.WriteLine("Application context initialized.", ConsoleColor.Green, true);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error initializing application context: {ex.Message}", ConsoleColor.Red, true);

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
                _trayIcon.ShowBalloonTip(3000); // Reduced from 5000 to 3000ms
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
                        ConsoleManager.WriteLine("Loading icon from favicon.ico", ConsoleColor.Green, true);
                        _cachedIcon = new Icon(faviconPath);
                        return _cachedIcon;
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.WriteLine($"Error loading favicon.ico: {ex.Message}", ConsoleColor.Yellow, true);
                        // Continue to next method
                    }
                }

                // Try to load icon from embedded resource
                try
                {
                    Icon? embeddedIcon = ExtractIconFromExecutable();
                    if (embeddedIcon != null)
                    {
                        ConsoleManager.WriteLine("Loading icon from embedded resource", ConsoleColor.Green, true);
                        _cachedIcon = embeddedIcon;
                        return _cachedIcon;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleManager.WriteLine($"Error extracting icon from executable: {ex.Message}", ConsoleColor.Yellow, true);
                    // Continue to next method
                }

                // Try to load icon from file - fallback to app.ico
                string appIconPath = Path.Combine(Program.AppPath, "app.ico");
                if (File.Exists(appIconPath))
                {
                    try
                    {
                        ConsoleManager.WriteLine("Loading icon from app.ico", ConsoleColor.Green, true);
                        _cachedIcon = new Icon(appIconPath);
                        return _cachedIcon;
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.WriteLine($"Error loading app.ico: {ex.Message}", ConsoleColor.Yellow, true);
                        // Continue to fallback
                    }
                }

                // Fallback to default application icon
                ConsoleManager.WriteLine("Using default application icon", ConsoleColor.Yellow, true);
                return SystemIcons.Application;
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
                // Use AppContext.BaseDirectory instead of Assembly.Location for single-file apps
                string executablePath = AppContext.BaseDirectory;

                // Try to get the current process's main module filename
                string? processPath = null;
                try
                {
                    processPath = Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    ConsoleManager.WriteLine($"Could not get process path: {ex.Message}", ConsoleColor.Yellow, true);
                }

                // If we got a valid process path, try to extract the icon
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    try
                    {
                        return Icon.ExtractAssociatedIcon(processPath);
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.WriteLine($"Failed to extract icon from process path: {ex.Message}", ConsoleColor.Yellow, true);
                    }
                }

                // Try with the entry assembly's name
                string entryAssemblyPath = Path.Combine(executablePath, AppDomain.CurrentDomain.FriendlyName);
                if (File.Exists(entryAssemblyPath))
                {
                    try
                    {
                        return Icon.ExtractAssociatedIcon(entryAssemblyPath);
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.WriteLine($"Failed to extract icon from entry assembly: {ex.Message}", ConsoleColor.Yellow, true);
                    }
                }

                // Try with a hardcoded executable name
                string hardcodedPath = Path.Combine(executablePath, "CS2DiscordRPC.exe");
                if (File.Exists(hardcodedPath))
                {
                    try
                    {
                        return Icon.ExtractAssociatedIcon(hardcodedPath);
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.WriteLine($"Failed to extract icon from hardcoded path: {ex.Message}", ConsoleColor.Yellow, true);
                    }
                }

                return null;
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

            try
            {
                // Add menu items
                menu.Items.Add("Settings", null, OnSettingsClick);
                menu.Items.Add("Toggle Debug Mode", null, OnToggleDebugModeClick);
                menu.Items.Add("-");
                menu.Items.Add("Exit", null, OnExitClick);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error creating context menu: {ex.Message}", ConsoleColor.Red, true);

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

                ConsoleManager.WriteLine("Settings form shown", ConsoleColor.Green, true);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error showing main form: {ex.Message}", ConsoleColor.Red, true);
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
                ConsoleManager.WriteLine($"Error showing settings form: {ex.Message}", ConsoleColor.Red, true);
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
                ConsoleManager.WriteLine($"Error exiting application: {ex.Message}", ConsoleColor.Red, true);

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
                ConsoleManager.WriteLine($"Error during shutdown: {ex.Message}", ConsoleColor.Red, true);
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
                    ConsoleManager.WriteLine($"Error disposing resources: {ex.Message}", ConsoleColor.Red, true);
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

