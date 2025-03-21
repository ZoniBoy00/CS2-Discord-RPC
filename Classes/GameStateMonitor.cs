using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RichPresenceApp.Classes
{
    public class GameStateMonitor
    {
        // Game state
        private GameState _gameState = new GameState();

        // Discord manager
        private readonly DiscordManager _discordManager;

        // Game process monitor timer - explicitly use System.Threading.Timer
        private System.Threading.Timer? _gameProcessMonitorTimer;

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
                // Create timer - explicitly use System.Threading.Timer
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

        // Check if CS2 is running
        private bool IsCS2Running()
        {
            try
            {
                // Get all processes
                Process[] processes = Process.GetProcesses();

                // Check if CS2 is running
                foreach (Process process in processes)
                {
                    try
                    {
                        // Check process name
                        if (process.ProcessName.Equals("cs2", StringComparison.OrdinalIgnoreCase) ||
                            process.ProcessName.Equals("csgo", StringComparison.OrdinalIgnoreCase) ||
                            process.ProcessName.Equals("Counter-Strike 2", StringComparison.OrdinalIgnoreCase) ||
                            process.ProcessName.Equals("Counter-Strike Global Offensive", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore errors for individual processes
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking if CS2 is running: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        // Update game state
        public void UpdateGameState(GameState gameState)
        {
            try
            {
                if (gameState == null)
                {
                    ConsoleManager.WriteLine("Received null game state", ConsoleColor.Yellow);
                    return;
                }

                // Update game state
                _gameState = gameState;

                // Update Discord presence
                _discordManager.UpdatePresence(_gameState);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error updating game state: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Stop game process monitor
        public void StopGameProcessMonitor()
        {
            try
            {
                // Dispose timer
                _gameProcessMonitorTimer?.Dispose();
                _gameProcessMonitorTimer = null;

                ConsoleManager.WriteLine("Game process monitor stopped.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error stopping game process monitor: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}