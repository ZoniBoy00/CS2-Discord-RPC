using System;
using System.Diagnostics;
using System.Threading; // Make sure this is included
using System.Threading.Tasks;

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

                // Create timer with 5-second interval
                _gameProcessMonitorTimer = new System.Threading.Timer(CheckGameProcess, null, 0, 5000);

                ConsoleManager.WriteLine("Game process monitor started.", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error starting game process monitor: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Check if CS2 is running
        private void CheckGameProcess(object? state)
        {
            try
            {
                // Check if CS2 is running
                bool isRunning = IsCS2Running();

                // Update game running state
                if (isRunning != Program.IsGameRunning)
                {
                    // Update the static property
                    Program.IsGameRunning = isRunning;

                    if (isRunning)
                    {
                        ConsoleManager.WriteLine("CS2 is now running.", ConsoleColor.Green);

                        // Set default presence
                        _discordManager.SetDefaultPresence();
                    }
                    else
                    {
                        ConsoleManager.WriteLine("CS2 is no longer running.", ConsoleColor.Yellow);

                        // Clear presence
                        _discordManager.ClearPresence();
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking game process: {ex.Message}", ConsoleColor.Red);
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
                    if (Process.GetProcessesByName(processName).Length > 0)
                    {
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

                ConsoleManager.WriteLine("Game process monitor stopped.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error stopping game process monitor: {ex.Message}", ConsoleColor.Red);
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

