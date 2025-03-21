using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DiscordRPC;
using DiscordRPC.Logging;

namespace RichPresenceApp.Classes
{
    public class DiscordManager
    {
        // Discord RPC client
        private DiscordRpcClient? _client;

        // Last updated presence for comparison
        private string? _lastPresenceHash;

        // Flag to track if presence is cleared
        private volatile bool _isPresenceCleared = true;

        // Lock object for thread safety
        private readonly object _presenceLock = new object();

        // Timestamp when CS2 was first detected as running
        private readonly DateTime _cs2StartTime;

        // Flag to track if player is in a match
        private bool _isInMatch = false;

        // Last known map
        private string _lastKnownMap = "Unknown";

        // Game mode mapping
        private readonly Dictionary<string, string> _gameModeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "casual", "Casual" },
            { "competitive", "Competitive" },
            { "deathmatch", "Deathmatch" },
            { "gungameprogressive", "Arms Race" },
            { "gungametrbomb", "Demolition" },
            { "training", "Training" },
            { "custom", "Custom" },
            { "cooperative", "Cooperative" },
            { "coopmission", "Guardian" },
            { "skirmish", "Skirmish" },
            { "survival", "Danger Zone" },
            { "scrimcomp2v2", "Wingman" },
            { "scrimcomp5v5", "Premier" },
            { "teamdeathmatch", "Team Deathmatch" },
            { "retakes", "Retakes" },
            { "ffa", "Free For All" },
            { "1v1", "1v1" },
            { "2v2", "2v2" },
            { "3v3", "3v3" },
            { "4v4", "4v4" },
            { "5v5", "5v5" },
            { "hostage", "Hostage Rescue" },
            { "demolition", "Demolition" },
            { "armsrace", "Arms Race" },
            { "dangerzone", "Danger Zone" },
            { "premier", "Premier" },
            { "wingman", "Wingman" },
            { "matchmaking", "Competitive" },
            { "unranked", "Unranked" },
            { "war", "War Games" },
            { "flying_scoutsman", "Flying Scoutsman" },
            { "retake", "Retakes" },
            { "guardian", "Guardian" },
            { "practice", "Practice" },
            { "offline", "Offline" },
            { "workshop", "Workshop" }
        };

        // Constructor
        public DiscordManager()
        {
            // Store the current time
            _cs2StartTime = DateTime.UtcNow;
        }

        // Initialize Discord RPC
        public void Initialize()
        {
            try
            {
                ConsoleManager.WriteLine("Initializing Discord RPC...");

                if (Config.Current == null)
                {
                    ConsoleManager.WriteLine("Configuration not loaded, cannot initialize Discord RPC", ConsoleColor.Red);
                    return;
                }

                // Create client with minimal logging
                _client = new DiscordRpcClient(Config.Current.ApplicationId)
                {
                    Logger = new ConsoleLogger { Level = LogLevel.Error }
                };

                // Subscribe to essential events only
                _client.OnReady += (_, e) =>
                    ConsoleManager.WriteLine("Connected to Discord successfully.", ConsoleColor.Green);

                _client.OnConnectionFailed += (_, e) =>
                    ConsoleManager.WriteLine("Failed to connect to Discord. Make sure Discord is running.", ConsoleColor.Red);

                _client.OnError += (_, e) =>
                    ConsoleManager.WriteLine($"Discord error: {e.Message}", ConsoleColor.Red);

                // Initialize
                _client.Initialize();

                // Check if client is initialized
                if (_client.IsInitialized)
                {
                    // Set initial presence
                    SetDefaultPresence();
                }
                else
                {
                    ConsoleManager.WriteLine("Failed to initialize Discord RPC client.", ConsoleColor.Red);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error initializing Discord RPC: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Update presence based on game state with optimized comparison
        public void UpdatePresence(GameState gameState)
        {
            if (_client == null || !_client.IsInitialized)
                return;

            try
            {
                lock (_presenceLock)
                {
                    // Only update presence if CS2 is running
                    if (!Program.IsGameRunning)
                    {
                        if (!_isPresenceCleared)
                        {
                            ClearPresence();

                            // Reset match state
                            _isInMatch = false;
                            _lastKnownMap = "Unknown";
                        }
                        return;
                    }

                    // Check if we have map data
                    bool hasMapData = !string.IsNullOrEmpty(gameState.CurrentMap) && gameState.CurrentMap != "Unknown";

                    // If we have map data and we're not in a match, enter match state
                    if (hasMapData && !_isInMatch)
                    {
                        _isInMatch = true;
                        _lastKnownMap = gameState.CurrentMap;
                        ConsoleManager.WriteLine($"Player entered match on map: {_lastKnownMap}", ConsoleColor.Yellow, true);
                    }
                    // If we don't have map data and we're in a match, exit match state
                    else if (!hasMapData && _isInMatch)
                    {
                        _isInMatch = false;
                        ConsoleManager.WriteLine("Player returned to main menu", ConsoleColor.Yellow, true);
                    }
                    // If we're in a match and have map data, update the last known map
                    else if (hasMapData && _isInMatch)
                    {
                        _lastKnownMap = gameState.CurrentMap;
                    }

                    // Special handling for ESC menu
                    // If we're in a match and the activity is "menu", it's likely the ESC menu
                    // We should keep showing the in-match presence
                    bool isEscMenu = _isInMatch && gameState.CurrentActivity == "menu" && hasMapData;

                    // Build presence based on game state and match status
                    RichPresence presence = BuildPresence(gameState, isEscMenu);

                    // Calculate hash for comparison (excluding timestamp)
                    string presenceHash = CalculatePresenceHash(presence);

                    // Only update if presence has changed
                    if (presenceHash != _lastPresenceHash)
                    {
                        // Update presence
                        _client.SetPresence(presence);
                        _lastPresenceHash = presenceHash;
                        _isPresenceCleared = false;

                        // Log significant presence changes
                        if (_isInMatch)
                        {
                            ConsoleManager.WriteLine($"Updated presence: In match on {_lastKnownMap}", ConsoleColor.Green);
                        }
                        else
                        {
                            ConsoleManager.WriteLine("Updated presence: In main menu", ConsoleColor.Green);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error updating presence: {ex.Message}", ConsoleColor.Red);

                // Try to reinitialize Discord client
                TryReinitialize();
            }
        }

        // Try to reinitialize Discord client if it fails
        private void TryReinitialize()
        {
            try
            {
                // Dispose old client
                _client?.Dispose();
                _client = null;

                // Wait a moment
                Thread.Sleep(1000);

                // Reinitialize
                Initialize();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error reinitializing Discord client: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Set default presence when not in game
        public void SetDefaultPresence()
        {
            try
            {
                if (_client == null || !_client.IsInitialized)
                    return;

                // Only set default presence if CS2 is running
                if (!Program.IsGameRunning)
                {
                    ClearPresence();
                    return;
                }

                // Reset match state when setting default presence
                _isInMatch = false;
                _lastKnownMap = "Unknown";

                ConsoleManager.WriteLine("Setting default Discord presence", ConsoleColor.Cyan);

                var presence = new RichPresence
                {
                    Details = "In Main Menu",
                    State = "Waiting for a match",
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };

                _client.SetPresence(presence);
                _lastPresenceHash = CalculatePresenceHash(presence);
                _isPresenceCleared = false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error setting default presence: {ex.Message}", ConsoleColor.Red);

                // Try to reinitialize Discord client
                TryReinitialize();
            }
        }

        // Build presence based on game state
        private RichPresence BuildPresence(GameState gameState, bool isEscMenu)
        {
            // If player is in a match
            if (_isInMatch)
            {
                // Use the last known map
                string mapToShow = _lastKnownMap;

                // In-game presence
                var presence = new RichPresence
                {
                    Details = BuildDetailsString(gameState, mapToShow),
                    State = BuildStateString(gameState),
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };

                // Add map image if available
                string mapImageKey = GetMapImageKey(mapToShow);
                if (!string.IsNullOrEmpty(mapImageKey) && mapImageKey != "cs2_logo")
                {
                    presence.Assets.SmallImageKey = mapImageKey;
                    presence.Assets.SmallImageText = mapToShow;
                }

                return presence;
            }
            else
            {
                // Menu presence
                return new RichPresence
                {
                    Details = "In Main Menu",
                    State = "Waiting for a match",
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };
            }
        }

        // Clear presence with thread safety
        public void ClearPresence()
        {
            if (_client == null || !_client.IsInitialized)
                return;

            try
            {
                lock (_presenceLock)
                {
                    if (!_isPresenceCleared)
                    {
                        _client.ClearPresence();
                        _lastPresenceHash = null;
                        _isPresenceCleared = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error clearing presence: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Shutdown Discord RPC
        public void Shutdown()
        {
            try
            {
                lock (_presenceLock)
                {
                    if (_client != null)
                    {
                        ClearPresence();
                        _client.Dispose();
                        _client = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error shutting down Discord RPC: {ex.Message}", ConsoleColor.Red);
            }
        }

        // Build details string
        private string BuildDetailsString(GameState gameState, string? mapName = null)
        {
            if (Config.Current == null)
                return "Playing CS2";

            var details = new System.Text.StringBuilder();

            // Use provided map name
            string mapToShow = mapName ?? "Unknown";

            if (Config.Current.ShowMap && !string.IsNullOrEmpty(mapToShow) && mapToShow != "Unknown")
            {
                details.Append($"Map: {mapToShow}");
            }

            if (Config.Current.ShowGameMode && !string.IsNullOrEmpty(gameState.CurrentGameMode) && gameState.CurrentGameMode != "Unknown")
            {
                if (details.Length > 0)
                    details.Append(" | ");

                // Get formatted game mode name
                string formattedGameMode = GetFormattedGameMode(gameState.CurrentGameMode);
                details.Append($"Mode: {formattedGameMode}");
            }

            return details.Length > 0 ? details.ToString() : "Playing CS2";
        }

        // Get formatted game mode name
        private string GetFormattedGameMode(string gameMode)
        {
            if (string.IsNullOrEmpty(gameMode))
                return "Unknown";

            // Try to get mapped game mode
            if (_gameModeMap.TryGetValue(gameMode, out string? mappedMode))
                return mappedMode;

            // Fallback to original game mode with first letter capitalized
            return char.ToUpper(gameMode[0]) + gameMode.Substring(1);
        }

        // Build state string
        private string BuildStateString(GameState gameState)
        {
            if (Config.Current == null)
                return "In a match";

            var state = new System.Text.StringBuilder();

            if (Config.Current.ShowScore && _isInMatch)
            {
                state.Append($"Score: CT {gameState.CurrentCTScore} - T {gameState.CurrentTScore}");
            }

            if (Config.Current.ShowTeam && !string.IsNullOrEmpty(gameState.CurrentTeam) && gameState.CurrentTeam != "Spectator")
            {
                if (state.Length > 0)
                    state.Append(" | ");
                state.Append($"Team: {gameState.CurrentTeam}");
            }

            if (state.Length == 0)
            {
                return _isInMatch ? "In a match" : "Waiting for a match";
            }

            return state.ToString();
        }

        // Get map image key
        private string GetMapImageKey(string mapName)
        {
            if (string.IsNullOrEmpty(mapName) || mapName == "Unknown")
                return "cs2_logo";

            // Extract the base map name (remove prefixes like de_, cs_, etc.)
            string baseName = mapName;
            if (mapName.Contains("_"))
            {
                string[] parts = mapName.Split('_');
                if (parts.Length > 1)
                {
                    // Keep the prefix for standard maps
                    if (parts[0] == "de" || parts[0] == "cs" || parts[0] == "ar" || parts[0] == "gg")
                    {
                        baseName = mapName;
                    }
                    else
                    {
                        // For workshop maps, just use the base name
                        baseName = parts[1];
                    }
                }
            }

            return baseName.ToLower();
        }

        // Calculate a hash for presence comparison
        private string CalculatePresenceHash(RichPresence presence)
        {
            // Exclude timestamp from hash to ensure it's updated regularly
            return $"{presence.Details}|{presence.State}|{presence.Assets?.LargeImageKey}|{presence.Assets?.SmallImageText}";
        }
    }
}