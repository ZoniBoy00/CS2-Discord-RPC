using System;
using System.Diagnostics;
using System.Threading;

namespace RichPresenceApp.Classes
{
    public class GameStateMonitor : IDisposable
    {
        // Game state
        private GameState _gameState = new GameState();

        // Discord manager
        private readonly DiscordManager _discordManager;

        // Game process monitor timer
        private System.Threading.Timer? _gameProcessMonitorTimer;

        // Process name cache
        private readonly string[] _processNames = { "cs2", "csgo", "Counter-Strike 2", "Counter-Strike Global Offensive" };

        // Last check result to avoid unnecessary updates
        private bool _lastCheckResult = false;

        // Check interval in milliseconds - increased to reduce CPU usage
        private const int CHECK_INTERVAL_MS = 10000; // 10 seconds

        // Constructor
        public GameStateMonitor(DiscordManager discordManager)
        {
            _discordManager = discordManager ?? throw new ArgumentNullException(nameof(discordManager));
        }

        // Start game process monitor
        public void StartGameProcessMonitor()
        {
            try
            {
                // Stop existing timer if any
                StopGameProcessMonitor();

                // Check initial state
                bool isRunning = IsCS2Running();
                Program.IsGameRunning = isRunning;
                _lastCheckResult = isRunning;

                if (isRunning)
                {
                    ConsoleManager.LogImportant("CS2 is already running at startup.");

                    // Set default presence
                    _discordManager.SetDefaultPresence();
                }

                // Create timer with longer interval to reduce CPU usage
                _gameProcessMonitorTimer = new System.Threading.Timer(CheckGameProcess, null, 0, CHECK_INTERVAL_MS);

                ConsoleManager.LogImportant("Game process monitor started.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error starting game process monitor", ex);
            }
        }

        // Check if CS2 is running
        private void CheckGameProcess(object? state)
        {
            try
            {
                // Check if CS2 is running
                bool isRunning = IsCS2Running();

                // Only update if state changed
                if (isRunning != _lastCheckResult)
                {
                    // Update the static property
                    Program.IsGameRunning = isRunning;
                    _lastCheckResult = isRunning;

                    if (isRunning)
                    {
                        ConsoleManager.LogImportant("CS2 is now running.");

                        // Set default presence
                        _discordManager.SetDefaultPresence();
                    }
                    else
                    {
                        ConsoleManager.LogImportant("CS2 is no longer running.");

                        // Clear presence
                        _discordManager.ClearPresence();
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error checking game process", ex);
            }
        }

        // Check if CS2 is running - optimized implementation
        private bool IsCS2Running()
        {
            try
            {
                // Use cached process names for faster lookup
                foreach (var processName in _processNames)
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        // Dispose processes to prevent resource leaks
                        foreach (var process in processes)
                        {
                            process.Dispose();
                        }
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error checking if CS2 is running", ex);
                return false;
            }
        }

        // Update game state - optimized
        public void UpdateGameState(GameState gameState)
        {
            try
            {
                if (gameState == null)
                {
                    ConsoleManager.LogError("Received null game state");
                    return;
                }

                // Update game state
                _gameState = gameState;

                // Update Discord presence
                _discordManager.UpdatePresence(_gameState);
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error updating game state", ex);
            }
        }

        // Stop game process monitor
        public void StopGameProcessMonitor()
        {
            try
            {
                // Dispose timer
                if (_gameProcessMonitorTimer != null)
                {
                    _gameProcessMonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _gameProcessMonitorTimer.Dispose();
                    _gameProcessMonitorTimer = null;
                }

                ConsoleManager.LogImportant("Game process monitor stopped.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error stopping game process monitor", ex);
            }
        }

        // Implement IDisposable
        public void Dispose()
        {
            StopGameProcessMonitor();
            GC.SuppressFinalize(this);
        }
    }
}

